using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.System
{
    public class PayrollPolicyRepository : BaseRepository<PayrollPolicy>, IPayrollPolicyRepository
    {
        public PayrollPolicyRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<List<PayrollPolicy>> GetByFilterAsync(PayrollPolicyType? policyType, bool includeInactive, CancellationToken ct = default)
        {
            var query = _dbSet.AsNoTracking().AsQueryable();

            if (policyType.HasValue)
                query = query.Where(x => x.PolicyType == policyType.Value);

            if (!includeInactive)
                query = query.Where(x => x.IsActive);

            return await query
                .OrderBy(x => x.PolicyType)
                .ThenBy(x => x.Code)
                .ThenByDescending(x => x.EffectiveFrom)
                .ToListAsync(ct);
        }

        public async Task<PayrollPolicy?> GetByIdForUpdateAsync(int id, CancellationToken ct = default)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.Id == id, ct);
        }

        public async Task<List<PayrollPolicy>> GetByTypeAndCodeAsync(PayrollPolicyType policyType, string code, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(x => x.PolicyType == policyType && x.Code == code)
                .ToListAsync(ct);
        }
    }
}
