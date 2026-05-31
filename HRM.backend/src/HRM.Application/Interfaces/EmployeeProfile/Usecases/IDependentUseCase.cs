using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;

namespace HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases
{
    public interface IDependentUseCase
    {
        Task<List<DependentDto>> GetMyDependentsAsync(int accountId, CancellationToken ct = default);
        Task<List<DependentDto>> GetEmployeeDependentsAsync(int employeeId, CancellationToken ct = default);
        Task<int> RequestCreateDependentAsync(int accountId, DependentRequestDto dto, CancellationToken ct = default);
        Task<int> RequestUpdateDependentAsync(int accountId, int dependentId, DependentRequestDto dto, CancellationToken ct = default);
        Task<int> RequestDeactivateDependentAsync(int accountId, int dependentId, CancellationToken ct = default);
        Task<List<PendingDependentRequestDto>> GetPendingRequestsAsync(int actorAccountId, string actorRoleName, CancellationToken ct = default);
        Task<bool> ReviewRequestAsync(int requestId, int actorAccountId, string actorRoleName, ReviewProfileUpdateDto dto, CancellationToken ct = default);
        Task<DependentDto> HrCreateDependentAsync(int employeeId, HrDependentDto dto, int actorAccountId, CancellationToken ct = default);
        Task<DependentDto> HrUpdateDependentAsync(int employeeId, int dependentId, HrDependentDto dto, int actorAccountId, CancellationToken ct = default);
        Task<bool> HrDeactivateDependentAsync(int employeeId, int dependentId, int actorAccountId, CancellationToken ct = default);
    }
}
