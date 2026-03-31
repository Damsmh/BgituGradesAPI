using AutoMapper;
using BgituGrades.DTO;
using BgituGrades.Entities;
using BgituGrades.Models.Discipline;
using BgituGrades.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BgituGrades.Services
{
    public interface IDisciplineService
    {
        Task<IEnumerable<DisciplineResponse>> GetAllDisciplinesAsync(CancellationToken cancellationToken);
        Task<DisciplineResponse> CreateDisciplineAsync(CreateDisciplineRequest request, CancellationToken cancellationToken);
        Task<DisciplineResponse?> GetDisciplineByIdAsync(int id, CancellationToken cancellationToken);
        Task<IEnumerable<DisciplineResponse>?> GetDisciplineByGroupIdAsync(int[] groupId, CancellationToken cancellationToken);
        Task<bool> UpdateDisciplineAsync(UpdateDisciplineRequest request, CancellationToken cancellationToken);
        Task<bool> DeleteDisciplineAsync(int id, CancellationToken cancellationToken);
        Task<IEnumerable<DisciplineDTO>> GetAllDisciplinesDtoAsync(CancellationToken cancellationToken);
        Task<IEnumerable<DisciplineDTO>?> GetDisciplinesDtoByGroupIdAsync(int groupId, CancellationToken cancellationToken);
        Task<DisciplineDTO?> GetDisciplineDtoByIdAsync(int id, CancellationToken cancellationToken);
    }
    public class DisciplineService(IDisciplineRepository disciplineRepository, IMapper mapper, IDistributedCache cache) : IDisciplineService
    {
        private readonly IDisciplineRepository _disciplineRepository = disciplineRepository;
        private readonly IMapper _mapper = mapper;
        private readonly IDistributedCache _cache = cache;
        private const string AllDisciplinesKey = "discipline:all";
        private const string DisciplineByGroupKey = "discipline:group:";

        public async Task<DisciplineResponse> CreateDisciplineAsync(CreateDisciplineRequest request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<Discipline>(request);
            var createdEntity = await _disciplineRepository.CreateDisciplineAsync(entity, cancellationToken: cancellationToken);
            await _cache.RemoveAsync(AllDisciplinesKey);
            return _mapper.Map<DisciplineResponse>(createdEntity);
        }

        public async Task<bool> DeleteDisciplineAsync(int id, CancellationToken cancellationToken)
        {
            var result = await _disciplineRepository.DeleteDisciplineAsync(id, cancellationToken: cancellationToken);
            if (result)
            {
                await _cache.RemoveAsync(AllDisciplinesKey);
            }
            return result;
        }

        public async Task<IEnumerable<DisciplineResponse>> GetAllDisciplinesAsync(CancellationToken cancellationToken)
        {
            var cached = await GetFromCacheAsync<IEnumerable<DisciplineResponse>>(AllDisciplinesKey);
            if (cached != null)
                return cached;

            var entities = await _disciplineRepository.GetAllAsync(cancellationToken: cancellationToken);
            var result = _mapper.Map<IEnumerable<DisciplineResponse>>(entities).ToList();
            await SetCacheAsync(AllDisciplinesKey, result, TimeSpan.FromHours(2));
            return result;
        }

        public async Task<IEnumerable<DisciplineResponse>?> GetDisciplineByGroupIdAsync(int[] groupIds, CancellationToken cancellationToken)
        {
            var cacheKey = $"{DisciplineByGroupKey}{groupIds}";
            var cached = await GetFromCacheAsync<List<DisciplineResponse>>(cacheKey);
            if (cached != null)
                return cached;

            var entities = await _disciplineRepository.GetByGroupIdsAsync(groupIds, cancellationToken: cancellationToken);
            if (entities == null)
                return null;

            var result = _mapper.Map<List<DisciplineResponse>>(entities);
            await SetCacheAsync(cacheKey, result, TimeSpan.FromHours(2));
            return result;
        }

        public async Task<DisciplineResponse?> GetDisciplineByIdAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await _disciplineRepository.GetByIdAsync(id, cancellationToken: cancellationToken);
            return entity == null ? null : _mapper.Map<DisciplineResponse>(entity);
        }

        public async Task<bool> UpdateDisciplineAsync(UpdateDisciplineRequest request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<Discipline>(request);
            var result = await _disciplineRepository.UpdateDisciplineAsync(entity, cancellationToken: cancellationToken);
            if (result)
            {
                await _cache.RemoveAsync(AllDisciplinesKey);
            }
            return result;
        }

        public async Task<IEnumerable<DisciplineDTO>> GetAllDisciplinesDtoAsync(CancellationToken cancellationToken)
        {
            var entities = await _disciplineRepository.GetAllAsync(cancellationToken: cancellationToken);
            return _mapper.Map<IEnumerable<DisciplineDTO>>(entities);
        }

        public async Task<IEnumerable<DisciplineDTO>?> GetDisciplinesDtoByGroupIdAsync(int groupId, CancellationToken cancellationToken)
        {
            var entities = await _disciplineRepository.GetByGroupIdAsync(groupId, cancellationToken: cancellationToken);
            return entities == null ? null : _mapper.Map<List<DisciplineDTO>>(entities);
        }

        public async Task<DisciplineDTO?> GetDisciplineDtoByIdAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await _disciplineRepository.GetByIdAsync(id, cancellationToken: cancellationToken);
            return entity == null ? null : _mapper.Map<DisciplineDTO>(entity);
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
