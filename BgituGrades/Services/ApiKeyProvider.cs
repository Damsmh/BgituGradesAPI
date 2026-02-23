using AspNetCore.Authentication.ApiKey;
using BgituGrades.Data;

namespace BgituGrades.Services
{
    public class ApiKeyProvider(AppDbContext dbContext) : IApiKeyProvider
    {
        private readonly AppDbContext _dbContext = dbContext;

        public async Task<IApiKey> ProvideAsync(string key)
        {
            var apiKey = await _dbContext.ApiKeys.FindAsync(key);

            if (apiKey == null || (apiKey.ExpiryDate.HasValue && apiKey.ExpiryDate < DateTime.UtcNow))
            {
                return null;
            }
            return apiKey;
        }
    }
}
