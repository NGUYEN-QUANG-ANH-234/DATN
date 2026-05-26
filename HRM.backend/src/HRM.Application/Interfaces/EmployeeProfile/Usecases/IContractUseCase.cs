using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;

namespace HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases
{
    public interface IContractUseCase
    {
        Task<int> CreateRequestAsync(int accountId, ContractRequestDto dto, CancellationToken ct, string? idempotencyKey = null);
        Task DeptReviewAsync(int contractId, int approverAccountId, string actorRoleName, ReviewContractDto dto, CancellationToken ct);
        Task HrCreateDraftAsync(int contractId, int actorAccountId, string actorRoleName, CreateDraftDto dto, CancellationToken ct);
        Task HrRejectAsync(int contractId, int actorAccountId, string actorRoleName, string reason, CancellationToken ct);
        Task NegotiateAsync(int contractId, int actorAccountId, NegotiateDto dto, CancellationToken ct);
        Task EmployeeAcceptAsync(int contractId, int actorAccountId, CancellationToken ct);
        Task DirectorReviewAsync(int contractId, int approverAccountId, string actorRoleName, ReviewContractDto dto, CancellationToken ct);

        // Query endpoints
        Task<IEnumerable<ContractResponseDto>> GetMyContractsAsync(int accountId, CancellationToken ct);
        Task<IEnumerable<ContractResponseDto>> GetAllContractsAsync(CancellationToken ct);
        Task<IEnumerable<ContractResponseDto>> GetPendingDeptAsync(CancellationToken ct);
        Task<IEnumerable<ContractResponseDto>> GetPendingHRAsync(CancellationToken ct);
        Task<IEnumerable<ContractResponseDto>> GetPendingDirectorAsync(CancellationToken ct);
    }
}
