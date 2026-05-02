using HRM.backend.src.HRM.Application.Interfaces;
using System.Collections.Concurrent;

namespace HRM.backend.src.HRM.Infrastructure.ExternalServices
{
    public class LockService : ILockService
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public async Task<T> GetWithLockAsync<T>(string key, Func<Task<T>> action) 
        {
            var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            await semaphore.WaitAsync();
            try 
            {
                return await action();
            }
            finally 
            {
                semaphore.Release();
            }
        }
    }
}
