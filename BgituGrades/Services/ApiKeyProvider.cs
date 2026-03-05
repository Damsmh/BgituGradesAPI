using AspNetCore.Authentication.ApiKey;
using BgituGrades.Data;
using BgituGrades.Features;
using BgituGrades.Repositories;

namespace BgituGrades.Services
{
    public class ApiKeyProvider(AppDbContext dbContext, ITokenHasher hasher, IKeyRepository keyRepository) : IApiKeyProvider
    {
        private readonly ITokenHasher _hasher = hasher;
        private readonly IKeyRepository _keyRepository = keyRepository;

        public async Task<IApiKey> ProvideAsync(string key)
        {
            var lookupHash = _hasher.ComputeLookupHash(key);
            var storedKey = await _keyRepository.GetByLookupHashAsync(lookupHash);

            if (storedKey is null) return null;

            var verified = _hasher.Verify(key, storedKey.StoredHash);
            if (!verified) return null;

            if (storedKey.ExpiryDate is not null && storedKey.ExpiryDate < DateTime.UtcNow) return null;

            return storedKey;
        }
    }
}
