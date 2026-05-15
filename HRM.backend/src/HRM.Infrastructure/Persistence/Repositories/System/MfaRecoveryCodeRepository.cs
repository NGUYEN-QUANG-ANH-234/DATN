using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.System
{
    public class MfaRecoveryCodeRepository : BaseRepository<MfaRecoveryCode>, IMfaRecoveryCodeRepository
    {
        public MfaRecoveryCodeRepository(MyDbContext context) : base(context) { }

        public async Task AddBulkAsync(IEnumerable<MfaRecoveryCode> codes, CancellationToken ct = default)
        {
            // Dùng _dbSet kế thừa từ BaseRepository
            await _dbSet.AddRangeAsync(codes);
        }

        public async Task<MfaRecoveryCode?> GetUnusedCodeAsync(int accountId, string plainCode, CancellationToken ct = default)
        {
            // Dùng _dbSet kế thừa từ BaseRepository
            var unusedCodes = await _dbSet
                .Where(c => c.AccountId == accountId)
                .ToListAsync();

            foreach (var code in unusedCodes)
            {
                if (BCrypt.Net.BCrypt.Verify(plainCode, code.CodeHash))
                {
                    return code;
                }
            }

            return null;
        }

        public async Task DeleteAllUserCodesAsync(int userId, CancellationToken ct = default)
        {
            var userCodes = await _dbSet.Where(c => c.AccountId == userId).ToListAsync();
            _dbSet.RemoveRange(userCodes);
        }
    }
}