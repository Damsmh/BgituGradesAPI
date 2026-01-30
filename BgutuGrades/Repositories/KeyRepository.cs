using BgutuGrades.Entities;

namespace BgutuGrades.Repositories
{
    public interface IKeyRepository
    {
        Task<IEnumerable<ApiKey>> GetKeysAsync();
        Task<ApiKey> CreateKeyAsync(ApiKey entity);
        Task<ApiKey?> GetAsync(string key);
        Task<bool> DeleteKeyAsync(string key);
    }
    public class KeyRepository : IKeyRepository
    {
        public Task<ApiKey> CreateKeyAsync(ApiKey entity)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteKeyAsync(string key)
        {
            throw new NotImplementedException();
        }

        public Task<ApiKey?> GetAsync(string key)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ApiKey>> GetKeysAsync()
        {
            throw new NotImplementedException();
        }
    }
}
