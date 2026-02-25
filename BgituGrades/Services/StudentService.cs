using AutoMapper;
using BgituGrades.Entities;
using BgituGrades.Models.Student;
using BgituGrades.Repositories;

namespace BgituGrades.Services
{
    public interface IStudentService
    {
        Task<IEnumerable<StudentResponse>> GetAllStudentsAsync();
        Task<IEnumerable<StudentResponse>> GetStudentsByGroupAsync(GetStudentsByGroupRequest request);
        Task<StudentResponse> CreateStudentAsync(CreateStudentRequest request);
        Task<StudentResponse?> GetStudentByIdAsync(int id);
        Task<bool> UpdateStudentAsync(UpdateStudentRequest request);
        Task<bool> DeleteStudentAsync(int id);
    }
    public class StudentService(IStudentRepository studentRepository, IPresenceRepository presenceRepository,
        IClassService classService, IDisciplineRepository disciplineRepository, IMapper mapper) : IStudentService
    {
        private readonly IStudentRepository _studentRepository = studentRepository;
        private readonly IPresenceRepository _presenceRepository = presenceRepository;
        private readonly IDisciplineRepository _disciplineRepository = disciplineRepository;
        private readonly IClassService _classService = classService;
        private readonly IMapper _mapper = mapper;

        public async Task<StudentResponse> CreateStudentAsync(CreateStudentRequest request)
        {
            var entity = _mapper.Map<Student>(request);
            var createdEntity = await _studentRepository.CreateStudentAsync(entity);
            Dictionary<int, IEnumerable<DateOnly>> disciplinesDict = new();
            var disciplines = await _disciplineRepository.GetByGroupIdAsync(request.GroupId);
            foreach (var discipline in disciplines)
            {
                var classes = await _classService.GenerateScheduleDatesAsync(request.GroupId, discipline.Id);
                disciplinesDict.Add(discipline.Id, classes.Select(c => c.Date));
            }
            await _presenceRepository.AddNewStudentPresences(createdEntity.Id, disciplinesDict);
            return _mapper.Map<StudentResponse>(createdEntity);
        }

        public async Task<bool> DeleteStudentAsync(int id)
        {
            return await _studentRepository.DeleteStudentAsync(id);
        }

        public async Task<IEnumerable<StudentResponse>> GetAllStudentsAsync()
        {
            var entities = await _studentRepository.GetAllStudentsAsync();
            return _mapper.Map<IEnumerable<StudentResponse>>(entities);
        }

        public async Task<StudentResponse?> GetStudentByIdAsync(int id)
        {
            var entity = await _studentRepository.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<StudentResponse>(entity);
        }

        public async Task<IEnumerable<StudentResponse>> GetStudentsByGroupAsync(GetStudentsByGroupRequest request)
        {
            var entities = await _studentRepository.GetStudentsByGroupAsync(request.GroupId);
            return _mapper.Map<IEnumerable<StudentResponse>>(entities);
        }

        public async Task<bool> UpdateStudentAsync(UpdateStudentRequest request)
        {
            var entity = _mapper.Map<Student>(request);
            return await _studentRepository.UpdateStudentAsync(entity);
        }
    }

}
