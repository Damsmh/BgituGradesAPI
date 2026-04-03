using AutoMapper;
using BgituGrades.Entities;
using BgituGrades.Models.Student;
using BgituGrades.Repositories;
using OfficeOpenXml;
using System.Text.RegularExpressions;

namespace BgituGrades.Services
{
    public interface IStudentService
    {
        Task<IEnumerable<StudentResponse>> GetStudentsByGroupAsync(GetStudentsByGroupRequest request, CancellationToken cancellationToken);
        Task<IEnumerable<StudentResponse>> GetArchivedStudentsByGroupAsync(GetStudentsByGroupRequest request, CancellationToken cancellationToken);
        Task<StudentResponse> CreateStudentAsync(CreateStudentRequest request, CancellationToken cancellationToken);
        Task<StudentResponse?> GetStudentByIdAsync(int id, CancellationToken cancellationToken);
        Task<bool> UpdateStudentAsync(UpdateStudentRequest request, CancellationToken cancellationToken);
        Task<bool> DeleteStudentAsync(int id, CancellationToken cancellationToken);
        Task<ImportResult> ImportStudentsFromXlsxAsync(Stream streamFile, CancellationToken cancellationToken);
        Task DeleteAllAsync(CancellationToken cancellationToken);
    }
    public partial class StudentService(IStudentRepository studentRepository, IPresenceRepository presenceRepository,
        IClassService classService, IDisciplineRepository disciplineRepository, IGroupService groupService, IMapper mapper) : IStudentService

    {
        private readonly IStudentRepository _studentRepository = studentRepository;
        private readonly IPresenceRepository _presenceRepository = presenceRepository;
        private readonly IDisciplineRepository _disciplineRepository = disciplineRepository;
        private readonly IClassService _classService = classService;
        private readonly IGroupService _groupService = groupService;
        private readonly IMapper _mapper = mapper;

        private const sbyte STATUS_STUDYING = 1;
        private const sbyte STATUS_ACADEMIC_LEAVE = -1;
        private const sbyte STATUS_EXPELLED = 3;
        private const short BATCH_SIZE = 2000;
        private const byte COL_CODE = 0;
        private const byte COL_LASTNAME = 1;
        private const byte COL_FIRSTNAME = 2;
        private const byte COL_MIDDLENAME = 3;
        private const byte COL_STATUS = 4;
        private const byte COL_GROUP_CODE = 6;
        private const byte COL_GROUP_NAME = 7;


        public async Task<StudentResponse> CreateStudentAsync(CreateStudentRequest request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<Student>(request);
            var createdEntity = await _studentRepository.CreateStudentAsync(entity, cancellationToken: cancellationToken);

            var disciplines = await _disciplineRepository.GetByGroupIdAsync(request.GroupId, cancellationToken: cancellationToken);

            var disciplinesDict = new Dictionary<int, IEnumerable<DateOnly>>();
            foreach (var d in disciplines)
            {
                var classes = await _classService.GenerateScheduleDatesAsync(request.GroupId, d!.Id, cancellationToken);
                disciplinesDict[d.Id] = classes.Select(c => c.Date);
            }

            await _presenceRepository.AddNewStudentPresences(createdEntity.Id, disciplinesDict, cancellationToken: cancellationToken);
            return _mapper.Map<StudentResponse>(createdEntity);
        }

        public async Task<bool> DeleteStudentAsync(int id, CancellationToken cancellationToken)
        {
            return await _studentRepository.DeleteStudentAsync(id, cancellationToken: cancellationToken);
        }

        public async Task<StudentResponse?> GetStudentByIdAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await _studentRepository.GetByIdAsync(id, cancellationToken: cancellationToken);
            return entity == null ? null : _mapper.Map<StudentResponse>(entity);
        }

        public async Task<IEnumerable<StudentResponse>> GetStudentsByGroupAsync(GetStudentsByGroupRequest request, CancellationToken cancellationToken)
        {
            var entities = await _studentRepository.GetStudentsByGroupIdsAsync(request.GroupIds, cancellationToken: cancellationToken);
            return _mapper.Map<IEnumerable<StudentResponse>>(entities);
        }

        public async Task<bool> UpdateStudentAsync(UpdateStudentRequest request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<Student>(request);
            return await _studentRepository.UpdateStudentAsync(entity, cancellationToken: cancellationToken);
        }

        public async Task<ImportResult> ImportStudentsFromXlsxAsync(Stream fileStream, CancellationToken cancellationToken)
        {
            var groupsByName = await _groupService
                .GetAllAsync(cancellationToken)
                .ContinueWith(t => t.Result.ToDictionary(
                    g => g.Name,
                    g => g.Id,
                    StringComparer.OrdinalIgnoreCase));

            var subGroupMap = groupsByName
                .Keys
                .Where(name => name.Contains('(') && name.Contains(')'))
                .GroupBy(name =>
                {
                    var match = Regex.Match(name, @"\([а-яёa-z]\)$",
                        RegexOptions.IgnoreCase);
                    return match.Success
                        ? name[..match.Index].Trim()
                        : name[..name.IndexOf('(')].Trim();
                }, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Select(name => groupsByName[name]).ToList(), StringComparer.OrdinalIgnoreCase);


            var result = new ImportResult();
            var batch = new List<Student>(BATCH_SIZE);
            var unknownGroups = new HashSet<string>();
            var leavedStudents = new List<int>();

            using var package = new ExcelPackage(fileStream);
            var sheet = package.Workbook.Worksheets[0];

            int totalRows = sheet.Dimension.End.Row;

            for (int row = 2; row <= totalRows; row++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var statusCell = sheet.Cells[row, COL_STATUS + 1].Value;
                if (statusCell == null) continue;

                var officialId = sheet.Cells[row, COL_CODE + 1].GetValue<int>();

                sbyte status = Convert.ToSByte(statusCell);
                if (status == STATUS_EXPELLED || status == STATUS_ACADEMIC_LEAVE)
                {
                    leavedStudents.Add(officialId);
                    continue;
                }


                var groupName = sheet.Cells[row, COL_GROUP_NAME + 1].GetValue<string>()?.Trim();

                if (string.IsNullOrEmpty(groupName))
                {
                    result.SkippedRows++;
                    continue;
                }

                List<int> targetGroupIds;
                if (groupsByName.TryGetValue(groupName, out int exactGroupId))
                {
                    targetGroupIds = [exactGroupId];
                }
                else if (subGroupMap.TryGetValue(groupName, out var subIds))
                {
                    targetGroupIds = subIds;
                }
                else
                {
                    unknownGroups.Add(groupName);
                    result.SkippedRows++;
                    continue;
                }

                var lastName = sheet.Cells[row, COL_LASTNAME + 1].GetValue<string>()?.Trim() ?? "";
                var firstName = sheet.Cells[row, COL_FIRSTNAME + 1].GetValue<string>()?.Trim() ?? "";
                var middleName = sheet.Cells[row, COL_MIDDLENAME + 1].GetValue<string>()?.Trim() ?? "";

                var officialGroupId = sheet.Cells[row, COL_GROUP_CODE + 1].GetValue<int>();

                var fullName = string.Join(" ",
                    new[] { lastName, firstName, middleName }
                        .Where(s => !string.IsNullOrEmpty(s) && s != "NULL"));

                foreach (var gId in targetGroupIds)
                {
                    batch.Add(new Student
                    {
                        OfficialId = officialId,
                        Name = fullName,
                        GroupId = gId,
                        OfficialGroupId = officialGroupId,
                    });
                }
                result.ProcessedRows++;

                if (batch.Count >= BATCH_SIZE)
                {
                    await FlushBatchAsync(batch, leavedStudents, cancellationToken);
                    batch.Clear();
                    leavedStudents.Clear();
                }
            }

            if (batch.Count > 0)
                await FlushBatchAsync(batch, leavedStudents, cancellationToken);

            result.UnknownGroups = unknownGroups;
            return result;
        }

        private async Task FlushBatchAsync(List<Student> batch, List<int> leavedIds, CancellationToken cancellationToken)
        {
            await _studentRepository.DeleteByIdsAsync(leavedIds, cancellationToken: cancellationToken);
            await _studentRepository.BulkInsertAsync(batch, cancellationToken: cancellationToken);
        }

        public async Task DeleteAllAsync(CancellationToken cancellationToken)
        {
            await _studentRepository.DeleteAllAsync(cancellationToken);
        }

        public async Task<IEnumerable<StudentResponse>> GetArchivedStudentsByGroupAsync(GetStudentsByGroupRequest request, CancellationToken cancellationToken)
        {
            var students = await _studentRepository.GetArchivedByGroupIdsAsync(request.GroupIds, cancellationToken);
            var results = _mapper.Map<IEnumerable<StudentResponse>>(students);
            return results;
        }
    }
}
