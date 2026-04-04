using AutoMapper;
using BgituGrades.DTO;
using BgituGrades.Entities;
using BgituGrades.Features;
using BgituGrades.Models.Group;
using BgituGrades.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BgituGrades.Services
{
    public interface IGroupService
    {
        Task<IEnumerable<GroupResponse>> GetGroupsByDisciplineAsync(int disciplineId, CancellationToken cancellationToken);
        Task<IEnumerable<GroupResponse>> GetAllAsync(CancellationToken cancellationToken);
        Task<GroupResponse> CreateGroupAsync(CreateGroupRequest request, CancellationToken cancellationToken);
        Task<GroupResponse?> GetGroupByIdAsync(int id, CancellationToken cancellationToken);
        Task<bool> UpdateGroupAsync(UpdateGroupRequest request, CancellationToken cancellationToken);
        Task<bool> DeleteGroupAsync(int id, CancellationToken cancellationToken);
        Task<IEnumerable<GroupDTO>> GetAllGroupsDtoAsync(CancellationToken cancellationToken);
        Task<IEnumerable<GroupDTO>> GetGroupsDtoByDisciplineAsync(int disciplineId, CancellationToken cancellationToken);
        Task<IEnumerable<ArchivedGroupResponse>> GetArchivedGroupsByPeriodAsync(int semester, int year, CancellationToken cancellationToken);
        Task<IEnumerable<CourseReponse>> GetCoursesAsync(CancellationToken cancellationToken);
        Task<IEnumerable<CourseReponse>> GetArchivedCoursesByPeriodAsync(GetByPeriodRequest request, CancellationToken cancellationToken);
        Task<IEnumerable<GroupResponse>> GetGroupsByCoursesAsync(int[] courses, CancellationToken cancellationToken);
        Task<IEnumerable<ArchivedGroupResponse>> GetArchivedGroupsByCoursesAndPeriodAsync(GetArchivedByCoursesRequest request, CancellationToken cancellationToken);
        Task<GroupDTO?> GetGroupDtoByIdAsync(int id, CancellationToken cancellationToken);
    }

    public class GroupService(IGroupRepository groupRepository, IMapper mapper, IDistributedCache cache) : IGroupService
    {
        private readonly IGroupRepository _groupRepository = groupRepository;
        private readonly IMapper _mapper = mapper;
        private readonly IDistributedCache _cache = cache;
        private const string AllGroupsKey = "group:all";
        private const string GroupsByDisciplineKey = "group:discipline:";
        private const string GroupByCourseKey = "group_by_course";

        public async Task<GroupResponse> CreateGroupAsync(CreateGroupRequest request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<Group>(request);
            var createdEntity = await _groupRepository.CreateGroupAsync(entity, cancellationToken: cancellationToken);

            await _cache.RemoveAsync(AllGroupsKey);
            return _mapper.Map<GroupResponse>(createdEntity);
        }

        public async Task<bool> DeleteGroupAsync(int id, CancellationToken cancellationToken)
        {
            var result = await _groupRepository.DeleteGroupAsync(id, cancellationToken: cancellationToken);
            if (result)
            {
                await _cache.RemoveAsync(AllGroupsKey);
            }
            return result;
        }

        public async Task<IEnumerable<GroupResponse>> GetAllAsync(CancellationToken cancellationToken)
        {
            var cached = await GetFromCacheAsync<IEnumerable<GroupResponse>>(AllGroupsKey);
            if (cached != null)
                return cached;

            var groups = await _groupRepository.GetAllAsync(cancellationToken: cancellationToken);
            var response = _mapper.Map<IEnumerable<GroupResponse>>(groups).ToList();
            await SetCacheAsync(AllGroupsKey, response, TimeSpan.FromHours(2));
            return response;
        }

        public async Task<GroupResponse?> GetGroupByIdAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await _groupRepository.GetByIdAsync(id, cancellationToken: cancellationToken);
            return entity == null ? null : _mapper.Map<GroupResponse>(entity);
        }

        public async Task<IEnumerable<GroupResponse>> GetGroupsByDisciplineAsync(int disciplineId, CancellationToken cancellationToken)
        {
            var cacheKey = $"{GroupsByDisciplineKey}{disciplineId}";
            var cached = await GetFromCacheAsync<IEnumerable<GroupResponse>>(cacheKey);
            if (cached != null)
                return cached;

            var entities = await _groupRepository.GetGroupsByDisciplineAsync(disciplineId, cancellationToken: cancellationToken);
            var result = _mapper.Map<IEnumerable<GroupResponse>>(entities).ToList();
            await SetCacheAsync(cacheKey, result, TimeSpan.FromHours(2));
            return result;
        }

        public async Task<bool> UpdateGroupAsync(UpdateGroupRequest request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<Group>(request);
            var result = await _groupRepository.UpdateGroupAsync(entity, cancellationToken: cancellationToken);
            if (result)
            {
                await _cache.RemoveAsync(AllGroupsKey);
            }
            return result;
        }

        public async Task<IEnumerable<GroupDTO>> GetAllGroupsDtoAsync(CancellationToken cancellationToken)
        {
            var groups = await _groupRepository.GetAllAsync(cancellationToken: cancellationToken);
            return _mapper.Map<IEnumerable<GroupDTO>>(groups);
        }

        public async Task<IEnumerable<GroupDTO>> GetGroupsDtoByDisciplineAsync(int disciplineId, CancellationToken cancellationToken)
        {
            var entities = await _groupRepository.GetGroupsByDisciplineAsync(disciplineId, cancellationToken: cancellationToken);
            return _mapper.Map<IEnumerable<GroupDTO>>(entities);
        }

        public async Task<GroupDTO?> GetGroupDtoByIdAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await _groupRepository.GetByIdAsync(id, cancellationToken: cancellationToken);
            return entity == null ? null : _mapper.Map<GroupDTO>(entity);
        }

        public async Task<IEnumerable<ArchivedGroupResponse>> GetArchivedGroupsByPeriodAsync(int semester, int year, CancellationToken cancellationToken)
        {
            var archived = await _groupRepository.GetArchivedByPeriod(semester, year, cancellationToken);
            var results = _mapper.Map<IEnumerable<ArchivedGroupResponse>>(archived);
            return results;
        }

        public async Task<IEnumerable<CourseReponse>> GetCoursesAsync(CancellationToken cancellationToken)
        {
            return await _groupRepository.GetCoursesAsync(cancellationToken);
        }

        public async Task<IEnumerable<CourseReponse>> GetArchivedCoursesByPeriodAsync(GetByPeriodRequest request, CancellationToken cancellationToken)
        {
            return await _groupRepository.GetArchivedCoursesByPeriodAsync(request.Year, request.Semester, cancellationToken);
        }

        public async Task<IEnumerable<GroupResponse>> GetGroupsByCoursesAsync(int[] courses, CancellationToken cancellationToken)
        {
            var results = new List<GroupResponse>();
            var missingCourses = new List<int>();

            foreach (var course in courses)
            {
                var singleCacheKey = $"{GroupByCourseKey}:{course}";
                var cached = await GetFromCacheAsync<List<GroupResponse>>(singleCacheKey);
                if (cached != null)
                {
                    results.AddRange(cached);
                }
                else
                {
                    missingCourses.Add(course);
                }
            }

            if (missingCourses.Count != 0)
            {
                var entities = await _groupRepository.GetGroupsByCoursesAsync([.. missingCourses], cancellationToken);
                if (entities != null && entities.Any())
                {
                    foreach (var course in missingCourses)
                    {
                        var groupsForCourse = entities
                            .Where(g => g.CourseNumber == course)
                            .ToList();

                        var mapped = _mapper.Map<List<GroupResponse>>(groupsForCourse);
                        await SetCacheAsync($"{GroupByCourseKey}:{course}", mapped, TimeSpan.FromHours(2));
                        results.AddRange(mapped);
                    }
                }
            }

            return results.DistinctBy(g => g.Id);
        }

        public async Task<IEnumerable<ArchivedGroupResponse>> GetArchivedGroupsByCoursesAndPeriodAsync(GetArchivedByCoursesRequest request, CancellationToken cancellationToken)
        {
            return await _groupRepository.GetArchivedGroupsByCoursesAndPeriodAsync(request, cancellationToken);
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