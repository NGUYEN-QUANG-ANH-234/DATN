using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Usecases
{
    public interface IPayrollFormulaManagementUseCase
    {
        Task<List<PayrollFormulaDto>> GetListAsync(FormulaStatus? status, string actorRole, CancellationToken ct = default);
        Task<PayrollFormulaDto> GetDetailAsync(int id, string actorRole, CancellationToken ct = default);
        Task<List<PayrollFormulaVariableDto>> GetVariablesAsync(string actorRole, CancellationToken ct = default);
        Task<PayrollFormulaValidationResultDto> ValidateAsync(UpsertPayrollFormulaDto dto, string actorRole, CancellationToken ct = default);
        Task<PayrollFormulaDto> CreateDraftAsync(UpsertPayrollFormulaDto dto, int actorAccountId, string actorRole, CancellationToken ct = default);
        Task<PayrollFormulaDto> UpdateDraftAsync(int id, UpsertPayrollFormulaDto dto, int actorAccountId, string actorRole, CancellationToken ct = default);
        Task<PayrollFormulaDto> CloneVersionAsync(int id, int actorAccountId, string actorRole, CancellationToken ct = default);
        Task<PayrollFormulaDto> SubmitForApprovalAsync(int id, int actorAccountId, string actorRole, CancellationToken ct = default);
        Task<PayrollFormulaDto> DirectorReviewAsync(int id, PayrollFormulaReviewDto dto, int actorAccountId, string actorRole, CancellationToken ct = default);
        Task<PayrollFormulaDto> ActivateAsync(int id, int actorAccountId, string actorRole, CancellationToken ct = default);
        Task<PayrollFormulaDto> ArchiveAsync(int id, PayrollFormulaActionNoteDto dto, int actorAccountId, string actorRole, CancellationToken ct = default);
    }
}
