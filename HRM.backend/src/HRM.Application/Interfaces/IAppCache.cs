using Microsoft.Extensions.Caching.Distributed;

namespace HRM.backend.src.HRM.Application.Interfaces
{
    public interface IAppCache
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T data, TimeSpan? absoluteExpireTime = null, TimeSpan? unusedExpireTime = null, CancellationToken ct = default);
        Task RemoveAsync(string key, CancellationToken ct = default);
    }
}
