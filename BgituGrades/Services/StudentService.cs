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
        Task<IEnumerable<StudentResponse>> GetArchivedStudentsByGroupAsync(GetStudentsByGroupRequest request, CancellationToken cancellationToken);
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
        IClassService classService, IDisciplineRepository disciplineRepository, IGroupService groupService,
        IGroupRepository groupRepository, IClassRepository classRepository, ITransferRepository transferRepository, IMapper mapper) : IStudentService
        
    {
        private readonly IStudentRepository _studentRepository = studentRepository;
        private readonly IGroupRepository _groupRepository = groupRepository;
        private readonly IClassRepository _classRepository = classRepository;
        private readonly ITransferRepository _transferRepository = transferRepository;
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
            var entities = await _studentRepository.GetStudentsByGroupIdsAsync(request.GroupIds, cancellationToken: cancellationToken);
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
            var groupIds = students.Select(s => s.GroupId).Distinct().ToList();

            var groups = await _groupRepository.GetByIdsAsync(groupIds, cancellationToken);
            var groupById = groups.ToDictionary(g => g.Id);

            var disciplinesByGroup = await _disciplineRepository.GetDictByGroupIdsAsync(groupIds, cancellationToken);

            var allPairs = disciplinesByGroup
                .SelectMany(kvp => kvp.Value.Select(d => new { GroupId = kvp.Key, DisciplineId = d.Id }))
                .ToList();

            var allDisciplineIds = allPairs.Select(p => p.DisciplineId).Distinct().ToList();

            var classesByGroupAndDiscipline = await _classRepository
                .GetClassesByGroupIdsAndDisciplineIdsAsync(groupIds, allDisciplineIds, cancellationToken);

            var transfersByGroupAndDiscipline = await _transferRepository
                .GetTransfersByGroupIdsAsync(groupIds, cancellationToken);

            var scheduleByGroupAndDiscipline = new Dictionary<(int GroupId, int DisciplineId), IEnumerable<DateOnly>>();

            foreach (var pair in allPairs)
            {
                var group = groupById[pair.GroupId];
                var key = (pair.GroupId, pair.DisciplineId);

                classesByGroupAndDiscipline.TryGetValue(key, out var classes);
                transfersByGroupAndDiscipline.TryGetValue(key, out var transfers);

                var dates = GenerateScheduleDatesInMemory(group, classes ?? [], transfers ?? []);
                scheduleByGroupAndDiscipline[key] = dates;
            }

            var result = new Dictionary<int, Dictionary<int, IEnumerable<DateOnly>>>();

            foreach (var student in students)
            {
                if (!disciplinesByGroup.TryGetValue(student.GroupId, out var disciplines))
                {
                    result[student.Id] = [];
                    continue;
                }

                var disciplineSchedules = new Dictionary<int, IEnumerable<DateOnly>>();
                foreach (var discipline in disciplines)
                {
                    var key = (student.GroupId, discipline.Id);
                    scheduleByGroupAndDiscipline.TryGetValue(key, out var dates);
                    disciplineSchedules[discipline.Id] = dates ?? [];
                }

                result[student.Id] = disciplineSchedules;
            }

            return result;
        }

        private static List<DateOnly> GenerateScheduleDatesInMemory(
            Group group, IEnumerable<Class> classes, IEnumerable<Transfer> transfers)
        {
            var startDate = group.StudyStartDate;
            var endDate = group.StudyEndDate;
            var firstWeekStart = group.StartWeekNumber;

            var studyStartDayOfWeek = startDate.DayOfWeek;
            var daysToMonday = ((int)DayOfWeek.Monday - (int)studyStartDayOfWeek + 7) % 7;
            var firstMonday = startDate.AddDays(daysToMonday);
            var week1Start = firstMonday.AddDays(-7 * (firstWeekStart - 1));

            var transferMap = transfers.ToDictionary(t => t.OriginalDate, t => t.NewDate);

            if (week1Start > endDate.AddDays(7)) return [];

            var dates = new List<DateOnly>();
            var currentWeekStart = week1Start;
            while (currentWeekStart <= endDate.AddDays(7))
            {
                foreach (var c in classes)
                {
                    var lessonDate = currentWeekStart
                        .AddDays(c.WeekDay - 1)
                        .AddDays(7 * (c.Weeknumber - 1));

                    if (lessonDate < startDate || lessonDate > endDate) continue;

                    var actualDate = transferMap.TryGetValue(lessonDate, out var newDate)
                        ? newDate : lessonDate;

                    dates.Add(actualDate);
                }
                currentWeekStart = currentWeekStart.AddDays(14);
            }

            return dates.Distinct().Order().ToList();
        }

        public async Task DeleteAllAsync(CancellationToken cancellationToken)
        {
            await studentRepository.DeleteAllAsync(cancellationToken);
        }

        public async Task<IEnumerable<StudentResponse>> GetArchivedStudentsByGroupAsync(GetStudentsByGroupRequest request, CancellationToken cancellationToken)
        {
            var students = _studentRepository.GetArchivedByGroupIdsAsync(request.GroupIds, cancellationToken);
            var results = _mapper.Map<List<StudentResponse>>(students);
            return results;
        }
    }
}
