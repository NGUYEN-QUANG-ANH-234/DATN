using Microsoft.Extensions.Caching.Distributed;

namespace HRM.backend.src.HRM.Application.Interfaces
{
    public interface IAppCache
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T data, TimeSpan? absoluteExpireTime = null, TimeSpan? unusedExpireTime = null);
        Task RemoveAsync(string key);
    }
}
