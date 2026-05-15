using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using HRM.backend.src.HRM.Application.Interfaces;

namespace HRM.backend.src.HRM.Infrastructure.ExternalServices
{
    public class LockService : ILockService
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public async Task<T> GetWithLockAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> action,
            TimeSpan? acquireTimeout = null,
            CancellationToken cancellationToken = default)
        {
            var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            // Mặc định đợi tối đa 10 giây để lấy Lock, tránh chờ vô tận
            var timeout = acquireTimeout ?? TimeSpan.FromSeconds(10);

            // 1. Chờ lấy Lock có Timeout
            bool isAcquired = await semaphore.WaitAsync(timeout, cancellationToken);
            if (!isAcquired)
            {
                throw new TimeoutException($"Không thể nhận lock cho key '{key}' sau {timeout.TotalSeconds} giây. Hệ thống đang quá tải.");
            }

            try
            {
                // 2. Chạy Action (Truyền CancellationToken vào để có thể hủy ngang action nếu cần)
                return await action(cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}