using AutoMapper;
using BgituGrades.Entities;
using BgituGrades.Models.Work;
using BgituGrades.Repositories;

namespace BgituGrades.Services
{
    public interface IWorkService
    {
        Task<IEnumerable<WorkResponse>> GetAllWorksAsync();
        Task<WorkResponse> CreateWorkAsync(CreateWorkRequest request);
        Task<WorkResponse?> GetWorkByIdAsync(int id);
        Task<bool> UpdateWorkAsync(UpdateWorkRequest request);
        Task<bool> DeleteWorkAsync(int id);
    }
    public class WorkService(IWorkRepository workRepository, IMapper mapper) : IWorkService
    {
        private readonly IWorkRepository _workRepository = workRepository;
        private readonly IMapper _mapper = mapper;

        public async Task<WorkResponse> CreateWorkAsync(CreateWorkRequest request)
        {
            var entity = _mapper.Map<Work>(request);
            var createdEntity = await _workRepository.CreateWorkAsync(entity);
            return _mapper.Map<WorkResponse>(createdEntity);
        }

        public async Task<bool> DeleteWorkAsync(int id)
        {
            return await _workRepository.DeleteWorkAsync(id);
        }

        public async Task<WorkResponse?> GetWorkByIdAsync(int id)
        {
            var entity = await _workRepository.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<WorkResponse>(entity);
        }

        public async Task<IEnumerable<WorkResponse>> GetAllWorksAsync()
        {
            var entities = await _workRepository.GetAllWorksAsync();
            return _mapper.Map<IEnumerable<WorkResponse>>(entities);
        }

        public async Task<bool> UpdateWorkAsync(UpdateWorkRequest request)
        {
            var entity = _mapper.Map<Work>(request);
            return await _workRepository.UpdateWorkAsync(entity);
        }
    }

}
