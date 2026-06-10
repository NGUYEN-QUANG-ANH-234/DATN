using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Usecases
{
    public interface IProjectBonusImportUseCase
    {
        Task<ProjectBonusImportPreviewDto> PreviewAsync(ProjectBonusImportRequestDto dto, int actorAccountId, string actorRole, CancellationToken ct = default);
        Task<ProjectBonusImportBatchDto> ImportAsync(ProjectBonusImportRequestDto dto, int actorAccountId, string actorRole, CancellationToken ct = default);
        Task<List<ProjectBonusImportBatchDto>> GetBatchesAsync(byte? month, short? year, ProjectBonusImportStatus? status, string actorRole, CancellationToken ct = default);
        Task<ProjectBonusImportBatchDto> GetDetailAsync(int id, string actorRole, CancellationToken ct = default);
        Task<ProjectBonusImportBatchDto> SubmitAsync(int id, int actorAccountId, string actorRole, CancellationToken ct = default);
        Task<ProjectBonusImportBatchDto> CancelAsync(int id, int actorAccountId, string actorRole, string? note, CancellationToken ct = default);
        Task<ProjectBonusImportBatchDto> DirectorReviewAsync(int id, ReviewProjectBonusImportDto dto, int actorAccountId, string actorRole, CancellationToken ct = default);
        Task<List<ProjectBonusImportBatchDto>> GetPendingDirectorAsync(int actorAccountId, string actorRole, CancellationToken ct = default);
    }
}
