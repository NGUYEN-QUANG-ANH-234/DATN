namespace HRM.backend.src.HRM.Application.Interfaces.System.Services
{
    public interface IIdempotencyService
    {
        Task<int?> FindResourceIdAsync(string scope, string idempotencyKey, CancellationToken ct = default);
        Task SaveAsync(
            string scope,
            string idempotencyKey,
            string resourceType,
            int resourceId,
            int? accountId = null,
            CancellationToken ct = default);
    }
}
