using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.System
{
    public interface IPayrollPolicyRepository : IBaseRepository<PayrollPolicy>
    {
        Task<List<PayrollPolicy>> GetByFilterAsync(PayrollPolicyType? policyType, bool includeInactive, CancellationToken ct = default);
        Task<PayrollPolicy?> GetByIdForUpdateAsync(int id, CancellationToken ct = default);
        Task<List<PayrollPolicy>> GetByTypeAndCodeAsync(PayrollPolicyType policyType, string code, CancellationToken ct = default);
    }
}
