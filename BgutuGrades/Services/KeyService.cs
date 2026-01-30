using AutoMapper;
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
    public class KeyService(IKeyRepository keyRepository, IMapper mapper) : IKeyService
    {
        private readonly IKeyRepository _keyRepository = keyRepository;
        private readonly IMapper _mapper = mapper;
        public async Task<KeyResponse> GenerateKeyAsync(Role role)
        {
            var newKey = RandomNumberGenerator.GetHexString(32, true);
            var apiKey = new ApiKey
            {
                Key = newKey,
                OwnerName = "PukiKaki",
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
            var storedKeys = _keyRepository.GetKeysAsync();
            var response = _mapper.Map<IEnumerable<KeyResponse>>(storedKeys);
            return response;
        }

        public async Task<KeyResponse> GetKeyAsync(string key)
        {
            var storedKey = _keyRepository.GetAsync(key);
            var response = _mapper.Map<KeyResponse>(storedKey);
            return response;
        }
    }
}
