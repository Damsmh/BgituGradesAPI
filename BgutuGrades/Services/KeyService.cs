using BgutuGrades.Entities;
using BgutuGrades.Models.Key;
using BgutuGrades.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace BgutuGrades.Services
{
    public interface IKeyService
    {
        Task<IEnumerable<KeyResponse>> GetKeysAsync();
        Task<KeyResponse> GetKeyAsync(string key);
        Task<KeyResponse> GenerateKeyAsync(Role role);
        Task<bool> DeleteKeyAsync(string key);
    }
    public class KeyService(IKeyRepository keyRepository) : IKeyService
    {
        private readonly IKeyRepository _keyRepository = keyRepository;
        public Task<KeyResponse> GenerateKeyAsync(Role role)
        {
            throw new NotImplementedException();
            //var newKey = RandomNumberGenerator.GetHexString(32, true);
            //var apiKey = new ApiKey
            //{
            //    Key = newKey,
            //    OwnerName = "PukiKaki",
            //    Role = role.ToString(),
            //    ExpiryDate = role == Role.STUDENT ? DateTime.UtcNow.AddDays(30) : null
            //};

            //_dbContext.ApiKeys.Add(apiKey);
            //await _dbContext.SaveChangesAsync();

            //var shareLink = $"{Request.Scheme}://{Request.Host}/api/grades?Key={newKey}";
            //return Ok(new { ShareLink = shareLink });
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
