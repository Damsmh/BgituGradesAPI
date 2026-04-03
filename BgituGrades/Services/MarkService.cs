using AutoMapper;
using BgituGrades.DTO;
using BgituGrades.Entities;
using BgituGrades.Models.Class;
using BgituGrades.Models.Mark;
using BgituGrades.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BgituGrades.Services
{
    public interface IMarkService
    {
        Task<IEnumerable<MarkResponse>> GetAllMarksAsync(CancellationToken cancellationToken);
        Task<MarkResponse> CreateMarkAsync(CreateMarkRequest request, CancellationToken cancellationToken);
        Task<IEnumerable<MarkResponse>> GetMarksByDisciplineAndGroupAsync(GetMarksByDisciplineAndGroupRequest request, CancellationToken cancellationToken);
        Task<bool> UpdateMarkAsync(UpdateMarkRequest request, CancellationToken cancellationToken);
        Task<bool> DeleteMarkByStudentAndWorkAsync(DeleteMarkByStudentAndWorkRequest request, CancellationToken cancellationToken);
        Task<FullGradeMarkResponse> UpdateOrCreateMarkAsync(UpdateMarkGradeRequest request, CancellationToken cancellationToken);
        Task<IEnumerable<MarkDTO>> GetAllMarksDtoAsync(CancellationToken cancellationToken);
        Task<MarkDTO?> GetMarkDtoByIdAsync(int id, CancellationToken cancellationToken);
    }
    public class MarkService(IMarkRepository markRepository, IMapper mapper, IDistributedCache cache) : IMarkService
    {
        private readonly IMarkRepository _markRepository = markRepository;
        private readonly IMapper _mapper = mapper;
        private readonly IDistributedCache _cache = cache;
        private const string CacheKeyPrefix = "mark:";
        private const string AllMarksKey = "mark:all";

        public async Task<MarkResponse> CreateMarkAsync(CreateMarkRequest request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<Mark>(request);
            var createdEntity = await _markRepository.CreateMarkAsync(entity, cancellationToken: cancellationToken);

            await InvalidateCacheAsync();
            return _mapper.Map<MarkResponse>(createdEntity);
        }

        public async Task<IEnumerable<MarkResponse>> GetAllMarksAsync(CancellationToken cancellationToken)
        {

            var cached = await GetFromCacheAsync<IEnumerable<MarkResponse>>(AllMarksKey);
            if (cached != null)
                return cached;

            var entities = await _markRepository.GetAllMarksAsync(cancellationToken: cancellationToken);
            var result = _mapper.Map<IEnumerable<MarkResponse>>(entities).ToList();
            await SetCacheAsync(AllMarksKey, result, TimeSpan.FromHours(1));
            return result;
        }

        public async Task<IEnumerable<MarkResponse>> GetMarksByDisciplineAndGroupAsync(GetMarksByDisciplineAndGroupRequest request, CancellationToken cancellationToken)
        {

            var cacheKey = $"{CacheKeyPrefix}discipline:{request.DisciplineId}:group:{request.GroupId}";

            var cached = await GetFromCacheAsync<IEnumerable<MarkResponse>>(cacheKey);
            if (cached != null)
                return cached;

            var entities = await _markRepository.GetMarksByDisciplineAndGroupAsync(request.DisciplineId, request.GroupId, cancellationToken: cancellationToken);
            var result = _mapper.Map<IEnumerable<MarkResponse>>(entities).ToList();
            await SetCacheAsync(cacheKey, result, TimeSpan.FromHours(2));
            return result;
        }

        public async Task<bool> UpdateMarkAsync(UpdateMarkRequest request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<Mark>(request);
            var result = await _markRepository.UpdateMarkAsync(entity, cancellationToken: cancellationToken);
            if (result)
            {
                await InvalidateCacheAsync();
            }
            return result;
        }

        public async Task<bool> DeleteMarkByStudentAndWorkAsync(DeleteMarkByStudentAndWorkRequest request, CancellationToken cancellationToken)
        {
            var result = await _markRepository.DeleteMarkByStudentAndWorkAsync(request.StudentId, request.WorkId, cancellationToken: cancellationToken);
            if (result)
            {
                await InvalidateCacheAsync();
            }
            return result;
        }

        public async Task<FullGradeMarkResponse> UpdateOrCreateMarkAsync(UpdateMarkGradeRequest request, CancellationToken cancellationToken)
        {
            var mark = await _markRepository.GetMarkByStudentAndWorkAsync(request.StudentId, request.WorkId, cancellationToken: cancellationToken);

            if (mark != null)
            {
                mark.Value = request.Value;
                await _markRepository.UpdateMarkAsync(mark, cancellationToken: cancellationToken);
            }
            else
            {
                mark = _mapper.Map<Mark>(request);
                await _markRepository.CreateMarkAsync(mark, cancellationToken: cancellationToken);
            }


            await InvalidateCacheAsync();

            var response = new FullGradeMarkResponse
            {
                StudentId = request.StudentId,
                Marks = [new GradeMarkResponse
                {
                    WorkId = request.WorkId,
                    Name = mark.Work!.Name!,
                    Value = request.Value,
                }]
            };
            return response;
        }

        public async Task<IEnumerable<MarkDTO>> GetAllMarksDtoAsync(CancellationToken cancellationToken)
        {
            var entities = await _markRepository.GetAllMarksAsync(cancellationToken: cancellationToken);
            return _mapper.Map<IEnumerable<MarkDTO>>(entities);
        }

        public async Task<MarkDTO?> GetMarkDtoByIdAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await _markRepository.GetMarkByIdAsync(id, cancellationToken: cancellationToken);
            return entity == null ? null : _mapper.Map<MarkDTO>(entity);
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

        private async Task InvalidateCacheAsync()
        {
            try
            {
                await _cache.RemoveAsync(AllMarksKey);
            }
            catch
            {

            }
        }
    }
}
