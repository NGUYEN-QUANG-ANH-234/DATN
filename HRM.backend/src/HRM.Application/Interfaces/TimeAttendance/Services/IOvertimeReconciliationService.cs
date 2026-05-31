using HRM.backend.src.HRM.Core.Entities.TimeAttendance;

namespace HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Services
{
    public interface IOvertimeReconciliationService
    {
        Task ReconcileAsync(OvertimeRequest request, AttendanceLog? attendanceLog, CancellationToken ct = default);
    }
}
