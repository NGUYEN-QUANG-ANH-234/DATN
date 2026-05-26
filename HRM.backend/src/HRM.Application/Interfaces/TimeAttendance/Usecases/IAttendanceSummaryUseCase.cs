using HRM.backend.src.HRM.Application.DTOs.TimeAttendance;

namespace HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Usecases
{
    public interface IAttendanceSummaryUseCase
    {
        Task<IEnumerable<AttendanceSummaryResponseDto>> GenerateMonthlyAsync(GenerateAttendanceSummaryDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default);
        Task<IEnumerable<AttendanceSummaryResponseDto>> GetMonthlyAsync(byte month, short year, string actorRoleName, CancellationToken ct = default);
    }
}
