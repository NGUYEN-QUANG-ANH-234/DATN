using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace HRM.backend.src.HRM.Application.Interfaces
{
    public interface ILockService
    {
        Task<T> GetWithLockAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> action,
            TimeSpan? acquireTimeout = null,
            CancellationToken cancellationToken = default);
    }
}
