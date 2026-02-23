using AutoMapper;
using BgituGrades.Entities;
using BgituGrades.Models.Group;
using BgituGrades.Repositories;

namespace BgituGrades.Services
{
    public interface IGroupService
    {
        Task<IEnumerable<GroupResponse>> GetGroupsByDisciplineAsync(int disciplineId);
        Task<IEnumerable<GroupResponse>> GetAllAsync();
        Task<GroupResponse> CreateGroupAsync(CreateGroupRequest request);
        Task<GroupResponse?> GetGroupByIdAsync(int id);
        Task<bool> UpdateGroupAsync(UpdateGroupRequest request);
        Task<bool> DeleteGroupAsync(int id);
    }

    public class GroupService(IGroupRepository groupRepository, IMapper mapper) : IGroupService
    {
        private readonly IGroupRepository _groupRepository = groupRepository;
        private readonly IMapper _mapper = mapper;

        public async Task<GroupResponse> CreateGroupAsync(CreateGroupRequest request)
        {
            var entity = _mapper.Map<Group>(request);
            var createdEntity = await _groupRepository.CreateGroupAsync(entity);
            return _mapper.Map<GroupResponse>(createdEntity);
        }

        public async Task<bool> DeleteGroupAsync(int id)
        {
            return await _groupRepository.DeleteGroupAsync(id);
        }

        public async Task<IEnumerable<GroupResponse>> GetAllAsync()
        {
            var groups = await _groupRepository.GetAllAsync();
            var response = _mapper.Map<IEnumerable<GroupResponse>>(groups);
            return response;
        }

        public async Task<GroupResponse?> GetGroupByIdAsync(int id)
        {
            var entity = await _groupRepository.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<GroupResponse>(entity);
        }

        public async Task<IEnumerable<GroupResponse>> GetGroupsByDisciplineAsync(int disciplineId)
        {
            var entities = await _groupRepository.GetGroupsByDisciplineAsync(disciplineId);
            return _mapper.Map<IEnumerable<GroupResponse>>(entities);
        }

        public async Task<bool> UpdateGroupAsync(UpdateGroupRequest request)
        {
            var entity = _mapper.Map<Group>(request);
            return await _groupRepository.UpdateGroupAsync(entity);
        }
    }

}