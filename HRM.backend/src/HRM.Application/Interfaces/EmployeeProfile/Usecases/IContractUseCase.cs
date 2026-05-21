using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;

namespace HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases
{
    public interface IContractUseCase
    {
        Task CreateRequestAsync(int accountId, ContractRequestDto dto, CancellationToken ct);
        Task DeptReviewAsync(int contractId, ReviewContractDto dto, CancellationToken ct);
        Task HrCreateDraftAsync(int contractId, CreateDraftDto dto, CancellationToken ct);
        Task HrRejectAsync(int contractId, string reason, CancellationToken ct);
        Task NegotiateAsync(int contractId, NegotiateDto dto, CancellationToken ct);
        Task EmployeeAcceptAsync(int contractId, CancellationToken ct);
        Task DirectorReviewAsync(int contractId, ReviewContractDto dto, CancellationToken ct);

        // Query endpoints
        Task<IEnumerable<ContractResponseDto>> GetMyContractsAsync(int accountId, CancellationToken ct);
        Task<IEnumerable<ContractResponseDto>> GetAllContractsAsync(CancellationToken ct);
        Task<IEnumerable<ContractResponseDto>> GetPendingDeptAsync(CancellationToken ct);
        Task<IEnumerable<ContractResponseDto>> GetPendingHRAsync(CancellationToken ct);
        Task<IEnumerable<ContractResponseDto>> GetPendingDirectorAsync(CancellationToken ct);
    }
}
