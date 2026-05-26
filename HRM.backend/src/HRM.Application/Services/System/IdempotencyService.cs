using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;

namespace HRM.backend.src.HRM.Application.Services.System
{
    public class IdempotencyService : IIdempotencyService
    {
        private readonly IBaseRepository<IdempotencyRecord> _repo;

        public IdempotencyService(IBaseRepository<IdempotencyRecord> repo)
        {
            _repo = repo;
        }

        public async Task<int?> FindResourceIdAsync(string scope, string idempotencyKey, CancellationToken ct = default)
        {
            var key = NormalizeKey(idempotencyKey);
            if (string.IsNullOrWhiteSpace(key))
                return null;

            var now = DateTime.UtcNow;
            var record = (await _repo.FindAsync(x =>
                x.Scope == scope &&
                x.IdempotencyKey == key &&
                x.ExpiresAt > now, ct)).FirstOrDefault();

            return record?.ResourceId;
        }

        public async Task SaveAsync(
            string scope,
            string idempotencyKey,
            string resourceType,
            int resourceId,
            int? accountId = null,
            CancellationToken ct = default)
        {
            var key = NormalizeKey(idempotencyKey);
            if (string.IsNullOrWhiteSpace(key))
                return;

            await _repo.AddAsync(new IdempotencyRecord
            {
                Scope = scope,
                IdempotencyKey = key,
                ResourceType = resourceType,
                ResourceId = resourceId,
                AccountId = accountId
            }, ct);
        }

        private static string NormalizeKey(string idempotencyKey)
        {
            return idempotencyKey.Trim();
        }
    }
}
