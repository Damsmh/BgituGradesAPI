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
        Task<IEnumerable<FullGradeMarkResponse>> GetMarksByWorksAsync(GetClassDateRequest request);
        Task<IEnumerable<FullGradePresenceResponse>> GetPresenceByScheduleAsync(GetClassDateRequest request);
        Task<ClassResponse> CreateClassAsync(CreateClassRequest request);
        Task<ClassResponse?> GetClassByIdAsync(int id);
        Task<bool> DeleteClassAsync(int id);
    }
    public class ClassService(IClassRepository classRepository, IGroupRepository groupRepository,
        IStudentRepository studentRepository, IWorkRepository workRepository, IMapper mapper) : IClassService
    {
        private readonly IClassRepository _classRepository = classRepository;
        private readonly IGroupRepository _groupRepository = groupRepository;
        private readonly IStudentRepository _studentRepository = studentRepository;
        private readonly IWorkRepository _workRepository = workRepository;
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

            var classDates = await GenerateScheduleDatesAsync(request);
            return classDates;
        }


        private async Task<IEnumerable<ClassDateResponse>> GenerateScheduleDatesAsync(GetClassDateRequest request,
            DateOnly? startDateOverride = null, DateOnly? endDateOverride = null)
        {
            var group = await _groupRepository.GetByIdAsync(request.GroupId);
            if (group == null) return [];

            var classes = await _classRepository.GetClassesByDisciplineAndGroupAsync(
                request.DisciplineId, request.GroupId);

            var startDate = startDateOverride ?? group.StudyStartDate;
            var endDate = endDateOverride ?? group.StudyEndDate;
            var firstWeekStart = group.StartWeekNumber;

            var dates = new List<ClassDateResponse>();

    
            var studyStartDayOfWeek = startDate.DayOfWeek;
            var daysToMonday = ((int)DayOfWeek.Monday - (int)studyStartDayOfWeek + 7) % 7;
            var firstMonday = startDate.AddDays(daysToMonday);

     
            var week1Start = firstMonday.AddDays(-7 * (firstWeekStart - 1));

     
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

                    if (lessonDate >= startDate && lessonDate <= endDate)
                    {
                        dates.Add(new ClassDateResponse
                        {
                            Date = lessonDate,
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

        public async Task<IEnumerable<FullGradeMarkResponse>> GetMarksByWorksAsync(GetClassDateRequest request)
        {
            var works = await _workRepository.GetByDisciplineAndGroupAsync(request.DisciplineId, request.GroupId);

            var students = await _studentRepository.GetMarksGrade(works, request.GroupId, request.DisciplineId);
            return students;
        }
    }
}
