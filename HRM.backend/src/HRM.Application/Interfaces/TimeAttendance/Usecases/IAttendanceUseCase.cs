using HRM.backend.src.HRM.Application.DTOs.TimeAttendance;

namespace HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Usecases
{
    public interface IAttendanceUseCase
    {
        Task<AttendanceTodayStatusDto> GetTodayStatusAsync(
            int accountId,
            CancellationToken ct = default);

        Task<AttendanceLogResponseDto> VerifyAndRecordAsync(
            int accountId,
            string clientIp,
            AttendanceGpsDto dto,
            CancellationToken ct = default);
    }
}
