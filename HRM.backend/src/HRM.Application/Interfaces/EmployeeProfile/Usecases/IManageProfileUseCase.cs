using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;

namespace HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases
{
    public interface IManageProfileUseCase
    {
        Task<int> RequestProfileUpdateAsync(int employeeId, ProfileUpdateRequestDto dto, CancellationToken ct = default);
        Task<MyProfileDto?> GetMyProfileAsync(int employeeId, CancellationToken ct = default);
        Task<List<MyContractDto>> GetMyContractsAsync(int employeeId, CancellationToken ct = default);
        Task<bool> ReviewProfileUpdateAsync(int requestId, int hrAccountId, string actorRoleName, ReviewProfileUpdateDto dto, CancellationToken ct = default);
        Task<List<PendingProfileRequestDto>> GetPendingProfileRequestsAsync(int actorAccountId, string actorRoleName, CancellationToken ct = default);
    }
}
