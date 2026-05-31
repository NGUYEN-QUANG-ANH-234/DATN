using HRM.backend.src.HRM.Core.Entities.TimeAttendance;

namespace HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Services
{
    public interface IAttendancePenaltyGeneratorService
    {
        Task GenerateFromDailySummariesAsync(IEnumerable<AttendanceDailySummary> dailySummaries, CancellationToken ct = default);
    }
}
