using AutoMapper;
using BgutuGrades.DTO;
using BgutuGrades.Entities;
using BgutuGrades.Models.Class;
using BgutuGrades.Repositories;
using Grades.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace BgutuGrades.Services
{
    public interface IClassService
    {
        Task<IEnumerable<ClassDateResponse>> GetClassDatesAsync(GetClassDateRequest request);
        Task<IEnumerable<FullGradeMarkResponse>> GetMarksByScheduleAsync(GetClassDateRequest request);
        Task<IEnumerable<FullGradePresenceResponse>> GetPresenceByScheduleAsync(GetClassDateRequest request);
        Task<ClassResponse> CreateClassAsync(CreateClassRequest request);
        Task<ClassResponse?> GetClassByIdAsync(int id);
        Task<bool> DeleteClassAsync(int id);
    }
    public class ClassService(IClassRepository classRepository, IGroupRepository groupRepository, IStudentRepository studentRepository, IMapper mapper) : IClassService
    {
        private readonly IClassRepository _classRepository = classRepository;
        private readonly IGroupRepository _groupRepository = groupRepository;
        private readonly IStudentRepository _studentRepository = studentRepository;
        private readonly IMapper _mapper = mapper;

        public async Task<ClassResponse> CreateClassAsync(CreateClassRequest request)
        {
            var entity = _mapper.Map<Class>(request);
            var createdEntity = await _classRepository.CreateClassAsync(entity);
            return _mapper.Map<ClassResponse>(createdEntity);
        }

        public async Task<IEnumerable<ClassDateResponse>> GetClassDatesAsync(GetClassDateRequest request)
        {
            var group = await _groupRepository.GetByIdAsync(request.GroupId);
            if (group == null) return [];

            var classes = await _classRepository.GetClassesByDisciplineAndGroupAsync(request.DisciplineId, request.GroupId);

            var classDates = GenerateClassDates(group, classes, request.EndDate);
            return _mapper.Map<IEnumerable<ClassDateResponse>>(classDates);
        }


        private async Task<IEnumerable<ClassDateResponse>> GenerateScheduleDatesAsync(GetClassDateRequest request,
            DateOnly? startDateOverride = null, DateOnly? endDateOverride = null)
        {
            var group = await _groupRepository.GetByIdAsync(request.GroupId);
            if (group == null) return [];

            var classes = await _classRepository.GetClassesByDisciplineAndGroupAsync(request.DisciplineId, request.GroupId);

            var startDate = startDateOverride ?? group.StudyStartDate;
            var endDate = endDateOverride ?? group.StudyEndDate;
            var firstWeekStart = group.StartWeekNumber;

            var dates = new List<ClassDateResponse>();


            var studyStartDayOfWeek = startDate.DayOfWeek;
            var firstMonday = startDate.AddDays(-(int)studyStartDayOfWeek); // понедельник
            var week1Start = firstMonday.AddDays(-7 * (firstWeekStart - 1));

            var currentWeekStart = week1Start;

            while (true)
            {
                if (currentWeekStart > endDate.AddDays(14)) break;

                foreach (var _class in classes)
                {
                    var lessonDate = currentWeekStart
                        .AddDays(_class.WeekDay - 1)
                        .AddDays(7 * (_class.Weeknumber - 1));

                    if (lessonDate >= startDate && lessonDate <= endDate)
                    {
                        dates.Add(new ClassDateResponse
                        {
                            Date = lessonDate,
                            ClassType = _class.Type,
                            Id = _class.Id
                        });
                    }
                }

                currentWeekStart = currentWeekStart.AddDays(14); // следующая неделя
            }

            return dates.OrderBy(d => d.Date).DistinctBy(d => d.Date).ToList();
        }

        private static List<ClassDateDTO> GenerateClassDates(Grades.Entities.Group group, IEnumerable<Class> classes, DateOnly endDate)
        {
            var classDates = new List<ClassDateDTO>();

            var startDate = group.StudyStartDate;
            var daysSinceMonday = (int)startDate.DayOfWeek - (int)DayOfWeek.Monday;
            if (daysSinceMonday < 0) daysSinceMonday += 7;

            var firstWeekStart = startDate.AddDays(-daysSinceMonday);
            firstWeekStart = firstWeekStart.AddDays(-(group.StartWeekNumber - 1) * 7);

            var currentWeekStart = firstWeekStart;
            while (currentWeekStart <= endDate)
            {
                foreach (var classItem in classes)
                {
                    var classDate = currentWeekStart
                        .AddDays(classItem.WeekDay)
                        .AddDays((classItem.Weeknumber - 1) * 7);

                    if (classDate >= group.StudyStartDate && classDate <= endDate)
                    {
                        classDates.Add(new ClassDateDTO
                        {
                            Date = classDate,
                            ClassType = classItem.Type
                        });
                    }
                }
                currentWeekStart = currentWeekStart.AddDays(14);
            }

            return classDates.OrderBy(x => x.Date).ToList();
        }

        public async Task<bool> DeleteClassAsync(int id)
        {
            return await _classRepository.DeleteClassAsync(id);
        }

        public async Task<ClassResponse?> GetClassByIdAsync(int id)
        {
            var entity = await _classRepository.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ClassResponse>(entity);
        }

        public async Task<IEnumerable<FullGradePresenceResponse>> GetPresenceByScheduleAsync(GetClassDateRequest request)
        {
            var scheduleDates = await GenerateScheduleDatesAsync(request);

            var students = await _studentRepository.GetPresenseGrade(scheduleDates, request.GroupId, request.DisciplineId);
            return students;
        }

        public async Task<IEnumerable<FullGradeMarkResponse>> GetMarksByScheduleAsync(GetClassDateRequest request)
        {
            var scheduleDates = await GenerateScheduleDatesAsync(request);

            var students = await _studentRepository.GetMarksGrade(scheduleDates, request.GroupId, request.DisciplineId);
            return students;
        }
    }
}
