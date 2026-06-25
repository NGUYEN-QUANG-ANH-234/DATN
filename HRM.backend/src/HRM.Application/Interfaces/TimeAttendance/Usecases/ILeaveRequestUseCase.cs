using HRM.backend.src.HRM.Application.DTOs.TimeAttendance;

namespace HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Usecases
{
    public interface ILeaveRequestUseCase
    {
        Task<int> CreateAsync(CreateLeaveRequestDto dto, int actorAccountId, CancellationToken ct = default, string? idempotencyKey = null);
        Task<IEnumerable<LeaveRequestResponseDto>> GetMyRequestsAsync(int actorAccountId, CancellationToken ct = default);
        Task<IEnumerable<LeaveRequestResponseDto>> GetPendingDeptAsync(int actorAccountId, string actorRoleName, CancellationToken ct = default);
        Task<IEnumerable<LeaveRequestResponseDto>> GetPendingHRAsync(string actorRoleName, CancellationToken ct = default);
        Task<IEnumerable<LeaveRequestResponseDto>> GetPendingDirectorAsync(string actorRoleName, CancellationToken ct = default);
        Task<bool> ReviewByDeptAsync(int id, ReviewLeaveRequestDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default);
        Task<bool> HrConfirmAsync(int id, ReviewLeaveRequestDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default);
        Task<bool> FinalApproveAsync(int id, ReviewLeaveRequestDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default);
    }
}
