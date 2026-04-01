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
        Task<IEnumerable<DisciplineResponse>?> GetArchivedDisciplinesByGroupIdsAsync(int[] groupIds, CancellationToken cancellationToken);
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
            var results = new List<DisciplineResponse>();
            var missingIds = new List<int>();

            foreach (var id in groupIds)
            {
                var singleCacheKey = $"{DisciplineByGroupKey}:{id}";
                var cached = await GetFromCacheAsync<List<DisciplineResponse>>(singleCacheKey);

                if (cached != null)
                {
                    results.AddRange(cached);
                }
                else
                {
                    missingIds.Add(id);
                }
            }

            if (missingIds.Count != 0)
            {
                var entities = await _disciplineRepository.GetByGroupIdsAsync([.. missingIds], cancellationToken);

                if (entities != null && entities.Any())
                {
                    

                    foreach (var groupId in missingIds)
                    {
                        var disciplinesForGroup = entities
                            .Where(d => d.Classes != null && d.Classes.Any(c => c.GroupId == groupId))
                            .ToList();
                        var mappedDisciplines = _mapper.Map<List<DisciplineResponse>>(disciplinesForGroup);

                        await SetCacheAsync($"{DisciplineByGroupKey}:{groupId}", mappedDisciplines, TimeSpan.FromHours(2));

                        results.AddRange(mappedDisciplines);
                    }
                }
            }

            return results.DistinctBy(d => d.Id);
        }

        public async Task<IEnumerable<DisciplineResponse>?> GetArchivedDisciplinesByGroupIdsAsync(int[] groupIds, CancellationToken cancellationToken)
        {
            var disciplines = await _disciplineRepository.GetArchivedByGroupIdsAsync(groupIds, cancellationToken);
            var results = _mapper.Map<List<DisciplineResponse>>(disciplines);
            return results;
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
