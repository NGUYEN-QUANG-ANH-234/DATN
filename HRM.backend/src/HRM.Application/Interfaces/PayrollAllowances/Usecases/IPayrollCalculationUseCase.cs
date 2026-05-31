using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;

namespace HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Usecases
{
    public interface IPayrollCalculationUseCase
    {
        Task<PayrollCalculationResultDto> ExecuteCalculationAsync(PayrollPeriodDto dto, int actorAccountId, string actorRole, CancellationToken ct = default);
        Task<PayrollAdjustmentDto> CreateAdjustmentAsync(CreatePayrollAdjustmentDto dto, int actorAccountId, string actorRole, CancellationToken ct = default);
        Task<List<PayrollAdjustmentDto>> GetAdjustmentsAsync(byte month, short year, string actorRole, CancellationToken ct = default);
    }
}
