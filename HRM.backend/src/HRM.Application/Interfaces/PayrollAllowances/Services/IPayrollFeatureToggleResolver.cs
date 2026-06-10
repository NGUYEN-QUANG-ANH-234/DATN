using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;

namespace HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Services
{
    public interface IPayrollFeatureToggleResolver
    {
        Task<PayrollFeatureToggleDto> GetAsync(CancellationToken ct = default);
    }
}
