using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;

namespace HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases
{
    public interface IContractAddendumUseCase
    {
        Task<ContractAddendumResponseDto> CreateDraftAsync(int contractId, CreateContractAddendumDto dto, CancellationToken ct, string? idempotencyKey = null);
        Task<ContractAddendumResponseDto> UpdateDraftAsync(int addendumId, CreateContractAddendumDto dto, CancellationToken ct);
        Task<ContractDocumentPreviewDto> PreviewDocumentAsync(int addendumId, CancellationToken ct);
        Task<ContractDocumentDownloadDto> DownloadDocumentDocAsync(int addendumId, CancellationToken ct);
        Task<ContractDocumentDownloadDto> DownloadDocumentPdfAsync(int addendumId, CancellationToken ct);
        Task<ContractDocumentPreviewDto> IssueDocumentAsync(int addendumId, IssueContractDocumentDto dto, int actorAccountId, string actorRoleName, CancellationToken ct);
        Task SubmitAsync(int addendumId, CancellationToken ct);
        Task ReviewByDeptAsync(int addendumId, int actorAccountId, string actorRoleName, ReviewContractAddendumDto dto, CancellationToken ct);
        Task ConfirmByHrAsync(int addendumId, int actorAccountId, string actorRoleName, ReviewContractAddendumDto dto, CancellationToken ct);
        Task EmployeeConfirmAsync(int addendumId, int actorAccountId, ReviewContractAddendumDto dto, CancellationToken ct);
        Task DirectorReviewAsync(int addendumId, int actorAccountId, string actorRoleName, ReviewContractAddendumDto dto, CancellationToken ct);
        Task RequestRevisionAsync(int addendumId, int actorAccountId, string actorRoleName, RequestRevisionDto dto, CancellationToken ct);
        Task ApproveAsync(int addendumId, int actorAccountId, string actorRoleName, CancellationToken ct);
        Task RejectAsync(int addendumId, int actorAccountId, string actorRoleName, string? reason, CancellationToken ct);
        Task<IEnumerable<ContractAddendumResponseDto>> GetByContractAsync(int contractId, CancellationToken ct);
        Task<IEnumerable<ContractAddendumResponseDto>> GetMyPendingEmployeeAsync(int actorAccountId, CancellationToken ct);
        Task<IEnumerable<ContractAddendumResponseDto>> GetPendingDeptAsync(int actorAccountId, string actorRoleName, CancellationToken ct);
        Task<IEnumerable<ContractAddendumResponseDto>> GetPendingHRAsync(CancellationToken ct);
        Task<IEnumerable<ContractAddendumResponseDto>> GetPendingDirectorAsync(int actorAccountId, string actorRoleName, CancellationToken ct);
        Task<IEnumerable<ContractAddendumResponseDto>> GetAllAsync(CancellationToken ct);
    }
}
