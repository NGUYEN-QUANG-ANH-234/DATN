using HRM.backend.src.HRM.Application.DTOs.TimeAttendance;

namespace HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Usecases
{
    public interface IOvertimeRequestUseCase
    {
        Task<int> CreateAsync(CreateOvertimeRequestDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default, string? idempotencyKey = null);
        Task<IReadOnlyList<int>> CreateBulkByManagerAsync(CreateBulkOvertimeRequestDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default, string? idempotencyKey = null);
        Task<IEnumerable<OvertimeEmployeeOptionDto>> GetAssignableEmployeesAsync(int actorAccountId, string actorRoleName, CancellationToken ct = default);
        Task<IEnumerable<OvertimeRequestResponseDto>> GetMyRequestsAsync(int actorAccountId, CancellationToken ct = default);
        Task<IEnumerable<OvertimeRequestResponseDto>> GetPendingManagerAsync(int actorAccountId, string actorRoleName, CancellationToken ct = default);
        Task<IEnumerable<OvertimeRequestResponseDto>> GetPendingHrAsync(string actorRoleName, CancellationToken ct = default);
        Task<IEnumerable<OvertimeRequestResponseDto>> GetPendingDirectorAsync(string actorRoleName, CancellationToken ct = default);
        Task<IEnumerable<OvertimeRequestResponseDto>> GetApprovedForHrAsync(string actorRoleName, int? month = null, int? year = null, CancellationToken ct = default);
        Task<bool> ReviewByManagerAsync(int id, ReviewOvertimeRequestDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default);
        Task<bool> ConfirmByHrAsync(int id, ReviewOvertimeRequestDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default);
        Task<bool> ReviewByDirectorAsync(int id, ReviewOvertimeRequestDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default);
        Task<OvertimeRequestResponseDto> ReconcileAsync(int id, int actorAccountId, string actorRoleName, CancellationToken ct = default);
    }
}
