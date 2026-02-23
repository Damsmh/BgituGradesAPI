using AutoMapper;
using BgituGrades.Entities;
using BgituGrades.Models.Key;
using BgituGrades.Repositories;
using System.Security.Cryptography;

namespace BgituGrades.Services
{
    public interface IKeyService
    {
        Task<IEnumerable<KeyResponse>> GetKeysAsync();
        Task<KeyResponse> GetKeyAsync(string key);
        Task<KeyResponse> GenerateKeyAsync(Role role);
        Task<bool> DeleteKeyAsync(string key);
    }
    public class KeyService(IKeyRepository keyRepository, IMapper mapper) : IKeyService
    {
        private readonly IKeyRepository _keyRepository = keyRepository;
        private readonly IMapper _mapper = mapper;
        public async Task<KeyResponse> GenerateKeyAsync(Role role)
        {
            var newKey = RandomNumberGenerator.GetHexString(64, true);
            var apiKey = new ApiKey
            {
                Key = newKey,
                OwnerName = "bgitugrades",
                Role = role.ToString(),
                ExpiryDate = role == Role.STUDENT ? DateTime.UtcNow.AddDays(30) : null
            };

            var createdKey = await _keyRepository.CreateKeyAsync(apiKey);
            var response = _mapper.Map<KeyResponse>(createdKey);
            return response;
        }

        public async Task<bool> DeleteKeyAsync(string key)
        {
            return await _keyRepository.DeleteKeyAsync(key);
        }

        public async Task<IEnumerable<KeyResponse>> GetKeysAsync()
        {
            var storedKeys = await _keyRepository.GetKeysAsync();
            var response = _mapper.Map<IEnumerable<KeyResponse>>(storedKeys);
            return response;
        }

        public async Task<KeyResponse> GetKeyAsync(string key)
        {
            var storedKey = await _keyRepository.GetAsync(key);
            var response = _mapper.Map<KeyResponse>(storedKey);
            return response;
        }
    }
}
