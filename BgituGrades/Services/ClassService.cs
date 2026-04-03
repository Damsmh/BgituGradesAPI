using AutoMapper;
using BgituGrades.DTO;
using BgituGrades.Entities;
using BgituGrades.Models.Class;
using BgituGrades.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BgituGrades.Services
{
    public interface IClassService
    {
        Task<IEnumerable<ClassDateResponse>> GetClassDatesAsync(GetClassDateRequest request, CancellationToken cancellationToken);
        Task<IEnumerable<FullGradeMarkResponse>> GetMarksByWorksAsync(GetClassDateRequest request, CancellationToken cancellationToken);
        Task<IEnumerable<FullGradePresenceResponse>> GetPresenceByScheduleAsync(GetClassDateRequest request, CancellationToken cancellationToken);
        Task<ClassResponse> CreateClassAsync(CreateClassRequest request, CancellationToken cancellationToken);
        Task<ClassResponse?> GetClassByIdAsync(int id, CancellationToken cancellationToken);
        Task<bool> DeleteClassAsync(int id, CancellationToken cancellationToken);
        Task<IEnumerable<ClassDateResponse>> GenerateScheduleDatesAsync(int groupId, int disciplineId, CancellationToken cancellationToken,
            DateOnly? startDateOverride = null, DateOnly? endDateOverride = null);
        Task<IEnumerable<ClassDateResponse>> GenerateScheduleDatesAsync(Group group, IEnumerable<Class> classes,
            IEnumerable<Transfer> transfers, DateOnly? startDateOverride = null, DateOnly? endDateOverride = null);
        Task<IEnumerable<ClassDTO>> GetAllClassesDtoAsync(CancellationToken cancellationToken);
        Task<ClassDTO?> GetClassDtoByIdAsync(int id, CancellationToken cancellationToken);
    }
    public class ClassService(IClassRepository classRepository, IGroupRepository groupRepository, ITransferService transferService,
        IStudentRepository studentRepository, IWorkRepository workRepository, IMapper mapper, IDistributedCache cache) : IClassService
    {
        private readonly IClassRepository _classRepository = classRepository;
        private readonly IGroupRepository _groupRepository = groupRepository;
        private readonly IStudentRepository _studentRepository = studentRepository;
        private readonly IWorkRepository _workRepository = workRepository;
        private readonly ITransferService _transferService = transferService;
        private readonly IMapper _mapper = mapper;
        private readonly IDistributedCache _cache = cache;
        private const string CacheKeyPrefix = "class:schedule:";

        public async Task<ClassResponse> CreateClassAsync(CreateClassRequest request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<Class>(request);
            var createdEntity = await _classRepository.CreateClassAsync(entity, cancellationToken: cancellationToken);
            return _mapper.Map<ClassResponse>(createdEntity);
        }

        public async Task<IEnumerable<ClassDateResponse>> GetClassDatesAsync(GetClassDateRequest request, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeyPrefix}group:{request.GroupId}:discipline:{request.DisciplineId}";

            var cached = await GetFromCacheAsync<IEnumerable<ClassDateResponse>>(cacheKey);
            if (cached != null)
                return cached;

            var group = await _groupRepository.GetByIdAsync(request.GroupId, cancellationToken: cancellationToken);
            if (group == null) return [];

            var classDates = await GenerateScheduleDatesAsync(request.GroupId, request.DisciplineId, cancellationToken);

            await SetCacheAsync(cacheKey, classDates.ToList(), TimeSpan.FromDays(7));

            return classDates;
        }


        public async Task<IEnumerable<ClassDateResponse>> GenerateScheduleDatesAsync(int groupId, int disciplineId, CancellationToken cancellationToken,
            DateOnly? startDateOverride = null, DateOnly? endDateOverride = null)
        {
            var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken: cancellationToken);
            if (group == null) return [];

            var classes = await _classRepository.GetClassesByDisciplineAndGroupAsync(disciplineId, groupId, cancellationToken: cancellationToken);
            var transfers = await _transferService.GetTransfersByGroupAndDisciplineAsync(groupId, disciplineId, cancellationToken: cancellationToken);



            var startDate = startDateOverride ?? group.StudyStartDate;
            var endDate = endDateOverride ?? group.StudyEndDate;
            var firstWeekStart = group.StartWeekNumber;

            var dates = new List<ClassDateResponse>();


            var studyStartDayOfWeek = startDate.DayOfWeek;
            var daysToMonday = ((int)DayOfWeek.Monday - (int)studyStartDayOfWeek + 7) % 7;
            var firstMonday = startDate.AddDays(daysToMonday);


            var week1Start = firstMonday.AddDays(-7 * (firstWeekStart - 1));

            var transferMap = transfers
                .ToDictionary(t => t.OriginalDate, t => t.NewDate);


            if (week1Start > endDate.AddDays(7))
                return dates;

            var currentWeekStart = week1Start;

            while (currentWeekStart <= endDate.AddDays(7))
            {
                foreach (var _class in classes)
                {
                    var lessonDate = currentWeekStart
                        .AddDays(_class.WeekDay - 1)
                        .AddDays(7 * (_class.Weeknumber - 1));

                    var actualDate = transferMap.TryGetValue(lessonDate, out var newDate)
                        ? newDate
                        : lessonDate;

                    if (lessonDate >= startDate && lessonDate <= endDate)
                    {
                        dates.Add(new ClassDateResponse
                        {
                            Date = actualDate,
                            ClassType = _class.Type,
                            StartTime = _class.StartTime,
                            Id = _class.Id
                        });
                    }
                }
                currentWeekStart = currentWeekStart.AddDays(14);
            }

            return dates.OrderBy(d => d.Date).DistinctBy(d => (d.Date, d.ClassType)).ToList();
        }

        public async Task<IEnumerable<ClassDateResponse>> GenerateScheduleDatesAsync(Group group, IEnumerable<Class> classes,
            IEnumerable<Transfer> transfers, DateOnly? startDateOverride = null, DateOnly? endDateOverride = null)
        {
            var startDate = startDateOverride ?? group.StudyStartDate;
            var endDate = endDateOverride ?? group.StudyEndDate;
            var firstWeekStart = group.StartWeekNumber;

            var dates = new List<ClassDateResponse>();


            var studyStartDayOfWeek = startDate.DayOfWeek;
            var daysToMonday = ((int)DayOfWeek.Monday - (int)studyStartDayOfWeek + 7) % 7;
            var firstMonday = startDate.AddDays(daysToMonday);


            var week1Start = firstMonday.AddDays(-7 * (firstWeekStart - 1));

            var transferMap = transfers
                .ToDictionary(t => t.OriginalDate, t => t.NewDate);


            if (week1Start > endDate.AddDays(7))
                return dates;

            var currentWeekStart = week1Start;

            while (currentWeekStart <= endDate.AddDays(7))
            {
                foreach (var _class in classes)
                {
                    var lessonDate = currentWeekStart
                        .AddDays(_class.WeekDay - 1)
                        .AddDays(7 * (_class.Weeknumber - 1));

                    var actualDate = transferMap.TryGetValue(lessonDate, out var newDate)
                        ? newDate
                        : lessonDate;

                    if (lessonDate >= startDate && lessonDate <= endDate)
                    {
                        dates.Add(new ClassDateResponse
                        {
                            Date = actualDate,
                            ClassType = _class.Type,
                            StartTime = _class.StartTime,
                            Id = _class.Id
                        });
                    }
                }
                currentWeekStart = currentWeekStart.AddDays(14);
            }

            return dates.OrderBy(d => d.Date).DistinctBy(d => (d.Date, d.ClassType)).ToList();
        }

        public async Task<bool> DeleteClassAsync(int id, CancellationToken cancellationToken)
        {
            return await _classRepository.DeleteClassAsync(id, cancellationToken: cancellationToken);
        }

        public async Task<ClassResponse?> GetClassByIdAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await _classRepository.GetByIdAsync(id, cancellationToken: cancellationToken);
            return entity == null ? null : _mapper.Map<ClassResponse>(entity);
        }

        public async Task<IEnumerable<FullGradePresenceResponse>> GetPresenceByScheduleAsync(GetClassDateRequest request, CancellationToken cancellationToken)
        {
            var scheduleDates = await GenerateScheduleDatesAsync(request.GroupId, request.DisciplineId, cancellationToken);

            var students = await _studentRepository.GetPresenseGrade(scheduleDates, request.GroupId, request.DisciplineId, cancellationToken: cancellationToken);
            return students;
        }

        public async Task<IEnumerable<FullGradeMarkResponse>> GetMarksByWorksAsync(GetClassDateRequest request, CancellationToken cancellationToken)
        {
            var works = await _workRepository.GetByDisciplineAndGroupAsync(request.DisciplineId, request.GroupId, cancellationToken: cancellationToken);

            var students = await _studentRepository.GetMarksGrade(works, request.GroupId, request.DisciplineId, cancellationToken: cancellationToken);
            return students;
        }

        public async Task<IEnumerable<ClassDTO>> GetAllClassesDtoAsync(CancellationToken cancellationToken)
        {
            var entities = await _classRepository.GetAllClassesAsync(cancellationToken: cancellationToken);
            return _mapper.Map<IEnumerable<ClassDTO>>(entities);
        }

        public async Task<ClassDTO?> GetClassDtoByIdAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await _classRepository.GetByIdAsync(id, cancellationToken: cancellationToken);
            return entity == null ? null : _mapper.Map<ClassDTO>(entity);
        }

        private async Task<T?> GetFromCacheAsync<T>(string key)
        {
            try
            {
                var value = await _cache.GetStringAsync(key);
                if (value == null)
                    return default;
                return JsonSerializer.Deserialize<T>(value);
            }
            catch
            {
                return default;
            }
        }

        private async Task SetCacheAsync<T>(string key, T value, TimeSpan expiration)
        {
            try
            {
                var serialized = JsonSerializer.Serialize(value);
                var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration };
                await _cache.SetStringAsync(key, serialized, options);
            }
            catch
            {

            }
        }
    }
}
