using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;

namespace HRM.backend.src.HRM.Application.Interfaces.System.UseCases
{
    public interface IPayrollFeatureToggleUseCase
    {
        Task<PayrollFeatureToggleDto> GetAsync(CancellationToken ct = default);
        Task<PayrollFeatureToggleDto> UpdateAsync(PayrollFeatureToggleDto dto, int actorAccountId, CancellationToken ct = default);
    }
}
