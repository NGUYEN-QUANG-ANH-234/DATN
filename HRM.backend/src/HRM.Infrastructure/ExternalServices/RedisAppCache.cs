using HRM.backend.src.HRM.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

public class RedisAppCache : IAppCache
{
    private readonly IDistributedCache _cache;

    public RedisAppCache(IDistributedCache cache) => _cache = cache;

    public async Task<T?> GetAsync<T>(string key)
    {
        var jsonData = await _cache.GetStringAsync(key);
        return jsonData is null ? default : JsonSerializer.Deserialize<T>(jsonData);
    }

    public async Task SetAsync<T>(string key, T data, TimeSpan? absoluteExpireTime = null, TimeSpan? unusedExpireTime = null, CancellationToken ct = default)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpireTime ?? TimeSpan.FromMinutes(60),
            SlidingExpiration = unusedExpireTime 
        };
        var jsonData = JsonSerializer.Serialize(data);
        await _cache.SetStringAsync(key, jsonData, options);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default) => await _cache.RemoveAsync(key, ct);
}