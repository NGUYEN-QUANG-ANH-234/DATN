using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;

namespace HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases
{
    public interface IContractAddendumUseCase
    {
        Task<ContractAddendumResponseDto> CreateDraftAsync(int contractId, CreateContractAddendumDto dto, CancellationToken ct);
        Task<ContractAddendumResponseDto> UpdateDraftAsync(int addendumId, CreateContractAddendumDto dto, CancellationToken ct);
        Task SubmitAsync(int addendumId, CancellationToken ct);
        Task ApproveAsync(int addendumId, CancellationToken ct);
        Task RejectAsync(int addendumId, string? reason, CancellationToken ct);
        Task<IEnumerable<ContractAddendumResponseDto>> GetByContractAsync(int contractId, CancellationToken ct);
        Task<IEnumerable<ContractAddendumResponseDto>> GetPendingDirectorAsync(CancellationToken ct);
        Task<IEnumerable<ContractAddendumResponseDto>> GetAllAsync(CancellationToken ct);
    }
}
