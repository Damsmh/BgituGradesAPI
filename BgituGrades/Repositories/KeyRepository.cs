using BgituGrades.Data;
using BgituGrades.Entities;
using Microsoft.EntityFrameworkCore;

namespace BgituGrades.Repositories
{
    public interface IKeyRepository
    {
        Task<IEnumerable<ApiKey>> GetKeysAsync();
        Task<ApiKey> CreateKeyAsync(ApiKey entity);
        Task<ApiKey?> GetAsync(string key);
        Task<bool> DeleteKeyAsync(string key);
    }
    public class KeyRepository(AppDbContext dbContext) : IKeyRepository
    {
        private readonly AppDbContext _dbContext = dbContext;
        public async Task<ApiKey> CreateKeyAsync(ApiKey entity)
        {
            await _dbContext.ApiKeys.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteKeyAsync(string key)
        {
            var storedKey = await GetAsync(key);
            _dbContext.ApiKeys.Remove(storedKey);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<ApiKey?> GetAsync(string key)
        {
            var storedKey = await _dbContext.ApiKeys.FindAsync(key);
            return storedKey;
        }

        public async Task<IEnumerable<ApiKey>> GetKeysAsync()
        {
            var keys = await _dbContext.ApiKeys.AsNoTracking().ToListAsync();
            return keys;
        }
    }
}
