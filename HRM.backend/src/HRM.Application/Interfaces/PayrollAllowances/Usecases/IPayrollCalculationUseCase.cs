using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;

namespace HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Usecases
{
    public interface IPayrollCalculationUseCase
    {
        Task<PayrollPreflightDto> GetPreflightAsync(PayrollPeriodDto dto, string actorRole, CancellationToken ct = default);
        Task<PayrollCalculationResultDto> ExecuteCalculationAsync(PayrollPeriodDto dto, int actorAccountId, string actorRole, CancellationToken ct = default);
        Task<PayrollRunSummaryDto> GetPayrollRunSummaryAsync(PayrollPeriodDto dto, string actorRole, CancellationToken ct = default);
        Task<List<PayrollRunSummaryDto>> GetPendingPayrollRunsAsync(string actorRole, CancellationToken ct = default);
        Task<PayrollRunSummaryDto> SubmitPayrollRunAsync(PayrollPeriodDto dto, int actorAccountId, string actorRole, CancellationToken ct = default);
        Task<PayrollRunSummaryDto> DirectorReviewPayrollRunAsync(PayrollPeriodDto dto, PayrollRunReviewDto review, int actorAccountId, string actorRole, CancellationToken ct = default);
        Task<PayrollRunSummaryDto> LockPayrollPeriodAsync(PayrollPeriodDto dto, int actorAccountId, string actorRole, CancellationToken ct = default);
        Task<PayrollAdjustmentDto> CreateAdjustmentAsync(CreatePayrollAdjustmentDto dto, int actorAccountId, string actorRole, CancellationToken ct = default);
        Task<List<PayrollAdjustmentDto>> GetAdjustmentsAsync(byte month, short year, string actorRole, CancellationToken ct = default);
    }
}
