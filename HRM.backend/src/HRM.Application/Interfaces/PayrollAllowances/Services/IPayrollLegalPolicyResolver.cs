using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;

namespace HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Services
{
    public interface IPayrollLegalPolicyResolver
    {
        Task<PayrollLegalPolicySet> ResolvePayrollPoliciesAsync(
            PayrollPeriodDto period,
            PayrollFeatureToggleDto featureToggles,
            CancellationToken ct = default);
    }
}
