using AutoMapper;
using BgutuGrades.Models.Discipline;
using BgutuGrades.Repositories;
using Grades.Entities;

namespace BgutuGrades.Services
{
    public interface IDisciplineService
    {
        Task<IEnumerable<DisciplineResponse>> GetAllDisciplinesAsync();
        Task<DisciplineResponse> CreateDisciplineAsync(CreateDisciplineRequest request);
        Task<DisciplineResponse?> GetDisciplineByIdAsync(int id);
        Task<IEnumerable<DisciplineResponse>?> GetDisciplineByGroupIdAsync(int groupId);
        Task<bool> UpdateDisciplineAsync(UpdateDisciplineRequest request);
        Task<bool> DeleteDisciplineAsync(int id);
    }
    public class DisciplineService(IDisciplineRepository disciplineRepository, IMapper mapper) : IDisciplineService
    {
        private readonly IDisciplineRepository _disciplineRepository = disciplineRepository;
        private readonly IMapper _mapper = mapper;

        public async Task<DisciplineResponse> CreateDisciplineAsync(CreateDisciplineRequest request)
        {
            var entity = _mapper.Map<Discipline>(request);
            var createdEntity = await _disciplineRepository.CreateDisciplineAsync(entity);
            return _mapper.Map<DisciplineResponse>(createdEntity);
        }

        public async Task<bool> DeleteDisciplineAsync(int id)
        {
            return await _disciplineRepository.DeleteDisciplineAsync(id);
        }

        public async Task<IEnumerable<DisciplineResponse>> GetAllDisciplinesAsync()
        {
            var entities = await _disciplineRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<DisciplineResponse>>(entities);
        }

        public async Task<IEnumerable<DisciplineResponse>?> GetDisciplineByGroupIdAsync(int groupId)
        {
            var entities = await _disciplineRepository.GetByGroupIdAsync(groupId);
            return entities == null ? null : _mapper.Map<List<DisciplineResponse>>(entities);
        }

        public async Task<DisciplineResponse?> GetDisciplineByIdAsync(int id)
        {
            var entity = await _disciplineRepository.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<DisciplineResponse>(entity);
        }

        public async Task<bool> UpdateDisciplineAsync(UpdateDisciplineRequest request)
        {
            var entity = _mapper.Map<Discipline>(request);
            return await _disciplineRepository.UpdateDisciplineAsync(entity);
        }
    }

}
