using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace HRM.backend.src.HRM.Application.Interfaces
{
    public interface ILockService
    {
        Task<T> GetWithLockAsync<T>(string Key, Func<Task<T>> action);
    }
}
