using AutoMapper;
using BgituGrades.DTO;
using BgituGrades.Entities;
using BgituGrades.Models.Student;
using BgituGrades.Repositories;
using OfficeOpenXml;

namespace BgituGrades.Services
{
    public interface IStudentService
    {
        Task<IEnumerable<StudentResponse>> GetAllStudentsAsync(CancellationToken cancellationToken);
        Task<IEnumerable<StudentResponse>> GetStudentsByGroupAsync(GetStudentsByGroupRequest request, CancellationToken cancellationToken);
        Task<StudentResponse> CreateStudentAsync(CreateStudentRequest request, CancellationToken cancellationToken);
        Task<StudentResponse?> GetStudentByIdAsync(int id, CancellationToken cancellationToken);
        Task<bool> UpdateStudentAsync(UpdateStudentRequest request, CancellationToken cancellationToken);
        Task<bool> DeleteStudentAsync(int id, CancellationToken cancellationToken);
        Task<IEnumerable<StudentDTO>> GetAllStudentsDtoAsync(CancellationToken cancellationToken);
        Task<IEnumerable<StudentDTO>> GetStudentsDtoByGroupAsync(int groupId, CancellationToken cancellationToken);
        Task<StudentDTO?> GetStudentDtoByIdAsync(int id, CancellationToken cancellationToken);
        Task<ImportResult> ImportStudentsFromXlsxAsync(Stream streamFile, CancellationToken cancellationToken);
        Task DeleteAllAsync(CancellationToken cancellationToken);
    }
    public class StudentService(IStudentRepository studentRepository, IPresenceRepository presenceRepository,
        IClassService classService, IDisciplineRepository disciplineRepository, IGroupService groupService, IMapper mapper) : IStudentService
    {
        private readonly IStudentRepository _studentRepository = studentRepository;
        private readonly IPresenceRepository _presenceRepository = presenceRepository;
        private readonly IDisciplineRepository _disciplineRepository = disciplineRepository;
        private readonly IClassService _classService = classService;
        private readonly IGroupService _groupService = groupService;
        private readonly IMapper _mapper = mapper;

        private const byte STATUS_STUDYING = 1;
        private const short BATCH_SIZE = 500;
        private const byte COL_CODE = 0;
        private const byte COL_LASTNAME = 1;
        private const byte COL_FIRSTNAME = 2;
        private const byte COL_MIDDLENAME = 3;
        private const byte COL_STATUS = 4;
        private const byte COL_GROUP_NAME = 7;


        public async Task<StudentResponse> CreateStudentAsync(CreateStudentRequest request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<Student>(request);
            var createdEntity = await _studentRepository.CreateStudentAsync(entity, cancellationToken: cancellationToken);

            var disciplines = await _disciplineRepository.GetByGroupIdAsync(request.GroupId, cancellationToken: cancellationToken);

            var disciplinesDict = new Dictionary<int, IEnumerable<DateOnly>>();
            foreach (var d in disciplines)
            {
                var classes = await _classService.GenerateScheduleDatesAsync(request.GroupId, d.Id, cancellationToken);
                disciplinesDict[d.Id] = classes.Select(c => c.Date);
            }

            await _presenceRepository.AddNewStudentPresences(createdEntity.Id, disciplinesDict, cancellationToken: cancellationToken);
            return _mapper.Map<StudentResponse>(createdEntity);
        }

        public async Task<bool> DeleteStudentAsync(int id, CancellationToken cancellationToken)
        {
            return await _studentRepository.DeleteStudentAsync(id, cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<StudentResponse>> GetAllStudentsAsync(CancellationToken cancellationToken)
        {
            var entities = await _studentRepository.GetAllStudentsAsync(cancellationToken: cancellationToken);
            return _mapper.Map<IEnumerable<StudentResponse>>(entities);
        }

        public async Task<StudentResponse?> GetStudentByIdAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await _studentRepository.GetByIdAsync(id, cancellationToken: cancellationToken);
            return entity == null ? null : _mapper.Map<StudentResponse>(entity);
        }

        public async Task<IEnumerable<StudentResponse>> GetStudentsByGroupAsync(GetStudentsByGroupRequest request, CancellationToken cancellationToken)
        {
            var entities = await _studentRepository.GetStudentsByGroupAsync(request.GroupId, cancellationToken: cancellationToken);
            return _mapper.Map<IEnumerable<StudentResponse>>(entities);
        }

        public async Task<bool> UpdateStudentAsync(UpdateStudentRequest request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<Student>(request);
            return await _studentRepository.UpdateStudentAsync(entity, cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<StudentDTO>> GetAllStudentsDtoAsync(CancellationToken cancellationToken)
        {
            var entities = await _studentRepository.GetAllStudentsAsync(cancellationToken: cancellationToken);
            return _mapper.Map<IEnumerable<StudentDTO>>(entities);
        }

        public async Task<IEnumerable<StudentDTO>> GetStudentsDtoByGroupAsync(int groupId, CancellationToken cancellationToken)
        {
            var entities = await _studentRepository.GetStudentsByGroupAsync(groupId, cancellationToken: cancellationToken);
            return _mapper.Map<IEnumerable<StudentDTO>>(entities);
        }

        public async Task<StudentDTO?> GetStudentDtoByIdAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await _studentRepository.GetByIdAsync(id, cancellationToken: cancellationToken);
            return entity == null ? null : _mapper.Map<StudentDTO>(entity);
        }

        public async Task<ImportResult> ImportStudentsFromXlsxAsync(Stream fileStream, CancellationToken cancellationToken)
        {
            var groupsByName = await _groupService
            .GetAllAsync(cancellationToken)
            .ContinueWith(t => t.Result.ToDictionary(
                g => g.Name,
                g => g.Id,
                StringComparer.OrdinalIgnoreCase));

            var result = new ImportResult();
            var batch = new List<Student>(BATCH_SIZE);
            var unknownGroups = new HashSet<string>();

            using var package = new ExcelPackage(fileStream);
            var sheet = package.Workbook.Worksheets[0];

            int totalRows = sheet.Dimension.End.Row;

            for (int row = 2; row <= totalRows; row++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var statusCell = sheet.Cells[row, COL_STATUS + 1].Value;
                if (statusCell == null) continue;

                sbyte status = Convert.ToSByte(statusCell);
                if (status != STATUS_STUDYING) continue;

                var groupName = sheet.Cells[row, COL_GROUP_NAME + 1].GetValue<string>()?.Trim();
                if (string.IsNullOrEmpty(groupName))
                {
                    result.SkippedRows++;
                    continue;
                }

                if (!groupsByName.TryGetValue(groupName, out int groupId))
                {
                    unknownGroups.Add(groupName);
                    result.SkippedRows++;
                    continue;
                }

                var officialId = sheet.Cells[row, COL_CODE + 1].GetValue<int>();
                var lastName = sheet.Cells[row, COL_LASTNAME + 1].GetValue<string>()?.Trim() ?? "";
                var firstName = sheet.Cells[row, COL_FIRSTNAME + 1].GetValue<string>()?.Trim() ?? "";
                var middleName = sheet.Cells[row, COL_MIDDLENAME + 1].GetValue<string>()?.Trim() ?? "";

                var fullName = string.Join(" ",
                    new[] { lastName, firstName, middleName }
                        .Where(s => !string.IsNullOrEmpty(s) && s != "NULL"));

                batch.Add(new Student
                {
                    OfficialId = officialId,
                    Name = fullName,
                    GroupId = groupId
                });

                result.ProcessedRows++;

                if (batch.Count >= BATCH_SIZE)
                {
                    await FlushBatchAsync(batch, cancellationToken);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
                await FlushBatchAsync(batch, cancellationToken);

            result.UnknownGroups = unknownGroups;
            return result;
        }

        private async Task FlushBatchAsync(List<Student> batch, CancellationToken cancellationToken)
        {
            await _studentRepository.BulkInsertAsync(batch, cancellationToken: cancellationToken);
            // Великая ъуйня batch возвращается уже с Id из бд

            var presencesDict = await BuildPresencesDictAsync(batch, cancellationToken: cancellationToken);
            await _presenceRepository.BulkInsertPresencesAsync(presencesDict, cancellationToken: cancellationToken);
        }

        private async Task<Dictionary<int, Dictionary<int, IEnumerable<DateOnly>>>> BuildPresencesDictAsync(
            List<Student> students, CancellationToken cancellationToken)
        {
            var byGroup = students.GroupBy(s => s.GroupId);
            var result = new Dictionary<int, Dictionary<int, IEnumerable<DateOnly>>>();

            foreach (var group in byGroup)
            {
                var disciplines = await _disciplineRepository
                    .GetByGroupIdAsync(group.Key, cancellationToken: cancellationToken);

                var disciplineSchedules = new Dictionary<int, IEnumerable<DateOnly>>();
                var scheduleTasks = disciplines.Select(d =>
                    _classService.GenerateScheduleDatesAsync(group.Key, d.Id, cancellationToken: cancellationToken)
                        .ContinueWith(t => (d.Id, Dates: t.Result.Select(c => c.Date))));

                foreach (var (disciplineId, dates) in await Task.WhenAll(scheduleTasks))
                    disciplineSchedules[disciplineId] = dates;

                foreach (var student in group)
                    result[student.Id] = disciplineSchedules;
            }
            return result;
        }

        public async Task DeleteAllAsync(CancellationToken cancellationToken)
        {
            await studentRepository.DeleteAllAsync(cancellationToken);
        }
    }
}
