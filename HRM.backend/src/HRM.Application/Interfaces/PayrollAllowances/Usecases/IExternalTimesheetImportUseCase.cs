using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Usecases
{
    public interface IExternalTimesheetImportUseCase
    {
        Task<ExternalTimesheetImportPreviewDto> PreviewAsync(ExternalTimesheetImportRequestDto dto, int actorAccountId, string actorRole, CancellationToken ct = default);
        Task<ExternalTimesheetImportBatchDto> ImportAsync(ExternalTimesheetImportRequestDto dto, int actorAccountId, string actorRole, CancellationToken ct = default);
        Task<List<ExternalTimesheetImportBatchDto>> GetBatchesAsync(byte? month, short? year, ExternalTimesheetImportStatus? status, string actorRole, CancellationToken ct = default);
        Task<ExternalTimesheetImportBatchDto> GetDetailAsync(int id, string actorRole, CancellationToken ct = default);
        Task<ExternalTimesheetImportBatchDto> SubmitAsync(int id, int actorAccountId, string actorRole, CancellationToken ct = default);
        Task<ExternalTimesheetImportBatchDto> CancelAsync(int id, int actorAccountId, string actorRole, string? note, CancellationToken ct = default);
        Task<ExternalTimesheetImportBatchDto> DirectorReviewAsync(int id, ReviewExternalTimesheetImportDto dto, int actorAccountId, string actorRole, CancellationToken ct = default);
        Task<List<ExternalTimesheetImportBatchDto>> GetPendingDirectorAsync(int actorAccountId, string actorRole, CancellationToken ct = default);
    }
}
