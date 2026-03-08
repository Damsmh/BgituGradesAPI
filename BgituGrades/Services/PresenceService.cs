using AutoMapper;
using BgituGrades.DTO;
using BgituGrades.Entities;
using BgituGrades.Models.Class;
using BgituGrades.Models.Presence;
using BgituGrades.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BgituGrades.Services
{
    public interface IPresenceService
    {
        Task<IEnumerable<PresenceResponse>> GetAllPresencesAsync();
        Task<PresenceResponse> CreatePresenceAsync(CreatePresenceRequest request);
        Task<IEnumerable<PresenceResponse>> GetPresencesByDisciplineAndGroupAsync(GetPresenceByDisciplineAndGroupRequest request);
        Task<bool> DeletePresenceByStudentAndDateAsync(DeletePresenceByStudentAndDateRequest request);
        Task UpdatePresenceAsync(UpdatePresenceRequest request);
        Task<FullGradePresenceResponse> UpdateOrCreatePresenceAsync(UpdatePresenceGradeRequest request);
        Task<IEnumerable<PresenceDTO>> GetAllPresencesDtoAsync();
        Task<PresenceDTO?> GetPresenceDtoByIdAsync(int id);
    }
    public class PresenceService(IPresenceRepository presenceRepository, IMapper mapper, IDistributedCache cache) : IPresenceService
    {
        private readonly IPresenceRepository _presenceRepository = presenceRepository;
        private readonly IMapper _mapper = mapper;
        private readonly IDistributedCache _cache = cache;
        private const string CacheKeyPrefix = "presence:";
        private const string AllPresencesKey = "presence:all";


        public async Task<PresenceResponse> CreatePresenceAsync(CreatePresenceRequest request)
        {
            var entity = _mapper.Map<Presence>(request);
            var createdEntity = await _presenceRepository.CreatePresenceAsync(entity);

            await InvalidateCacheAsync(request.DisciplineId, request.StudentId);
            return _mapper.Map<PresenceResponse>(createdEntity);
        }

        public async Task<IEnumerable<PresenceResponse>> GetAllPresencesAsync()
        {

            var cached = await GetFromCacheAsync<IEnumerable<PresenceResponse>>(AllPresencesKey);
            if (cached != null)
                return cached;

            var entities = await _presenceRepository.GetAllPresencesAsync();
            var result = _mapper.Map<IEnumerable<PresenceResponse>>(entities).ToList();
            await SetCacheAsync(AllPresencesKey, result, TimeSpan.FromHours(1));
            return result;
        }

        public async Task<IEnumerable<PresenceResponse>> GetPresencesByDisciplineAndGroupAsync(GetPresenceByDisciplineAndGroupRequest request)
        {

            var cacheKey = $"{CacheKeyPrefix}discipline:{request.DisciplineId}:group:{request.GroupId}";

            var cached = await GetFromCacheAsync<IEnumerable<PresenceResponse>>(cacheKey);
            if (cached != null)
                return cached;

            var entities = await _presenceRepository.GetPresencesByDisciplineAndGroupAsync(request.DisciplineId, request.GroupId);
            var result = _mapper.Map<IEnumerable<PresenceResponse>>(entities).ToList();
            await SetCacheAsync(cacheKey, result, TimeSpan.FromHours(2));
            return result;
        }

        public async Task<bool> DeletePresenceByStudentAndDateAsync(DeletePresenceByStudentAndDateRequest request)
        {
            var result = await _presenceRepository.DeletePresenceByStudentAndDateAsync(request.StudentId, request.Date);
            if (result)
            {
                await InvalidateCacheAsync(0, request.StudentId);
            }
            return result;
        }

        public async Task UpdatePresenceAsync(UpdatePresenceRequest request)
        {
            var entity = _mapper.Map<Presence>(request);
            await _presenceRepository.UpdatePresenceAsync(entity);
        }

        public async Task<FullGradePresenceResponse> UpdateOrCreatePresenceAsync(UpdatePresenceGradeRequest request)
        {
            var presence = await _presenceRepository.GetAsync(request.DisciplineId, request.StudentId, request.Date);

            if (presence != null)
            {
                presence.IsPresent = request.IsPresent;
                await _presenceRepository.UpdatePresenceAsync(presence);
            }
            else
            {
                presence = _mapper.Map<Presence>(request);
                await _presenceRepository.CreatePresenceAsync(presence);
            }


            await InvalidateCacheAsync(request.DisciplineId, request.StudentId);

            var response = new FullGradePresenceResponse
            {
                StudentId = request.StudentId,
                Presences = [new GradePresenceResponse {
                    ClassId = request.ClassId,
                    IsPresent = request.IsPresent,
                    Date = request.Date
                }]
            };
            return response;
        }

        public async Task<IEnumerable<PresenceDTO>> GetAllPresencesDtoAsync()
        {
            var entities = await _presenceRepository.GetAllPresencesAsync();
            return _mapper.Map<IEnumerable<PresenceDTO>>(entities);
        }

        public async Task<PresenceDTO?> GetPresenceDtoByIdAsync(int id)
        {
            var entity = await _presenceRepository.GetPresenceByIdAsync(id);
            return entity == null ? null : _mapper.Map<PresenceDTO>(entity);
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

        private async Task InvalidateCacheAsync(int disciplineId, int studentId)
        {
            try
            {
                await _cache.RemoveAsync(AllPresencesKey);
                if (disciplineId > 0)
                    await _cache.RemoveAsync($"{CacheKeyPrefix}discipline:{disciplineId}:*");
            }
            catch
            {

            }
        }
    }

}
