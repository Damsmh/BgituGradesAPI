using BgutuGrades.Entities;
using BgutuGrades.Models.Key;

namespace BgutuGrades.Services
{
    public interface IKeyService
    {
        Task<IEnumerable<KeyResponse>> GetKeysAsync();
        Task<KeyResponse> GetKeyAsync(string key);
        Task<KeyResponse> GenerateKeyAsync(Role role);
        Task<bool> DeleteKeyAsync(string key);
    }
    public class KeyService : IKeyService
    {
        public Task<KeyResponse> GenerateKeyAsync(Role role)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteKeyAsync(string key)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<KeyResponse>> GetKeysAsync()
        {
            throw new NotImplementedException();
        }

        public Task<KeyResponse> GetKeyAsync(string key)
        {
            throw new NotImplementedException();
        }
    }
}
