using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace cardscore_api.Services
{
    public class RedisService
    {
        private readonly IDistributedCache _cache;

        public RedisService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<T?> Get<T>(string key)
        {
            var cachedData = await _cache.GetStringAsync(key);

            if (!cachedData.IsNullOrEmpty())
            {
                return JsonSerializer.Deserialize<T>(cachedData);
            }

            return default(T);
        }

        public async Task Set(string key, object value, TimeSpan time)
        {
            await _cache.SetStringAsync(key, JsonSerializer.Serialize(value), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = time
            });
        }

        public async Task Remove(string key)
        {
            await _cache.RemoveAsync(key);
        }

        public async Task SetOneMins(string key, object value)
        {
            await Set(key,value,TimeSpan.FromSeconds(1));
        }

        public string CreateKey(string key)
        {
            return key;
        }

        public string CreateKeyFromRequest(HttpRequest request)
        {
            return request.GetEncodedUrl();
        }

        public string CreateKeyFromRequestWithUser(HttpRequest request)
        {
            return request.GetEncodedUrl() + request.Headers["UserId"];
        }

    }
}
