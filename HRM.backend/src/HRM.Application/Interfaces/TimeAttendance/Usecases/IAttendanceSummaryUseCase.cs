using HRM.backend.src.HRM.Application.DTOs.TimeAttendance;

namespace HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Usecases
{
    public interface IAttendanceSummaryUseCase
    {
        Task<IEnumerable<AttendanceSummaryResponseDto>> GenerateMonthlyAsync(GenerateAttendanceSummaryDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default);
        Task<IEnumerable<AttendanceSummaryResponseDto>> GetMonthlyAsync(byte month, short year, string actorRoleName, CancellationToken ct = default);
        Task<IEnumerable<AttendancePeriodApprovalDto>> GetPendingApprovalPeriodsAsync(string actorRoleName, CancellationToken ct = default);
        Task<IEnumerable<AttendanceSummaryResponseDto>> SubmitMonthlyTimesheetAsync(CloseAttendancePeriodDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default);
        Task<IEnumerable<AttendanceSummaryResponseDto>> ApproveMonthlyTimesheetAsync(CloseAttendancePeriodDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default);
        Task<IEnumerable<AttendanceSummaryResponseDto>> LockMonthlyTimesheetAsync(CloseAttendancePeriodDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default);
        Task<IEnumerable<AttendanceDailySummaryResponseDto>> GetDailyAsync(byte month, short year, string actorRoleName, CancellationToken ct = default);
        Task<IEnumerable<AttendanceAdjustmentLogResponseDto>> GetAdjustmentLogsAsync(byte month, short year, string actorRoleName, CancellationToken ct = default);
        Task<AttendanceDailySummaryResponseDto> AdjustDailyAsync(int id, AdjustAttendanceDailySummaryDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default);
        Task<AttendanceDailyImportResultDto> ImportDailyAdjustmentsAsync(ImportAttendanceDailySummaryDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default);
        Task<AttendanceDailySummaryResponseDto> ApproveDailyAsync(int id, int actorAccountId, string actorRoleName, CancellationToken ct = default);
    }
}
