using HRM.backend.src.HRM.Application.DTOs.TimeAttendance;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Usecases;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System.HRM.backend.src.HRM.Infrastructure.Repositories.Interfaces.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;

namespace HRM.backend.src.HRM.Application.UseCases.TimeAttendance
{
    public class AttendanceSummaryUseCase : IAttendanceSummaryUseCase
    {
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IOvertimeRequestRepository _overtimeRepo;
        private readonly IAttendanceSummaryRepository _summaryRepo;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;

        public AttendanceSummaryUseCase(
            IAttendanceRepository attendanceRepo,
            IOvertimeRequestRepository overtimeRepo,
            IAttendanceSummaryRepository summaryRepo,
            IAuditLogRepository auditLogRepo,
            IUnitOfWork unitOfWork,
            ILockService lockService)
        {
            _attendanceRepo = attendanceRepo;
            _overtimeRepo = overtimeRepo;
            _summaryRepo = summaryRepo;
            _auditLogRepo = auditLogRepo;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
        }

        public async Task<IEnumerable<AttendanceSummaryResponseDto>> GenerateMonthlyAsync(GenerateAttendanceSummaryDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            EnsureHrOrAdmin(actorRoleName);
            ValidatePeriod(dto.Month, dto.Year);

            return await _lockService.GetWithLockAsync(
                $"timesheet_{dto.Year}_{dto.Month:D2}",
                async (innerCt) => await GenerateMonthlyCoreAsync(dto, actorAccountId, actorRoleName, innerCt),
                cancellationToken: ct);
        }

        private async Task<IEnumerable<AttendanceSummaryResponseDto>> GenerateMonthlyCoreAsync(GenerateAttendanceSummaryDto dto, int actorAccountId, string actorRoleName, CancellationToken ct)
        {
            var periodStart = new DateTime(dto.Year, dto.Month, 1);
            var periodEnd = periodStart.AddMonths(1);

            var logs = await _attendanceRepo.FetchLogsByPeriodAsync(periodStart, periodEnd, ct);
            var approvedOt = await _overtimeRepo.GetApprovedByPeriodAsync(periodStart, periodEnd, ct);

            var employeeIds = logs
                .Where(l => l.EmployeeId.HasValue)
                .Select(l => l.EmployeeId!.Value)
                .Concat(approvedOt.Select(o => o.EmployeeId))
                .Distinct()
                .ToList();

            foreach (var employeeId in employeeIds)
            {
                var employeeLogs = logs.Where(l => l.EmployeeId == employeeId).ToList();
                var employeeOt = approvedOt.Where(o => o.EmployeeId == employeeId && !o.IsPayrollLocked).ToList();

                var summary = await _summaryRepo.GetByEmployeePeriodAsync(employeeId, dto.Month, dto.Year, ct);
                if (summary?.IsPayrollLocked == true)
                    continue;

                if (summary == null)
                {
                    summary = new AttendanceSummary
                    {
                        EmployeeId = employeeId,
                        Month = dto.Month,
                        Year = dto.Year
                    };
                    await _summaryRepo.AddAsync(summary, ct);
                }

                summary.WorkDays = CountWorkDays(employeeLogs);
                summary.LateMinutes = employeeLogs.Sum(CalculateLateMinutes);
                summary.EarlyLeaveMinutes = employeeLogs.Sum(CalculateEarlyLeaveMinutes);
                summary.ActualOtMinutes = employeeOt.Sum(o => o.ActualOtMinutes);
                summary.GeneratedAt = DateTime.UtcNow;
            }

            await _auditLogRepo.LogSystemEventAsync("GENERATE_ATTENDANCE_SUMMARY", actorAccountId, "attendance_summaries", $"Tổng hợp bảng công tháng {dto.Month:D2}/{dto.Year}");
            await _unitOfWork.CommitAsync(ct);

            return await GetMonthlyAsync(dto.Month, dto.Year, actorRoleName, ct);
        }

        public async Task<IEnumerable<AttendanceSummaryResponseDto>> GetMonthlyAsync(byte month, short year, string actorRoleName, CancellationToken ct = default)
        {
            EnsureHrOrAdmin(actorRoleName);
            ValidatePeriod(month, year);

            var summaries = await _summaryRepo.GetByPeriodAsync(month, year, ct);
            return summaries.Select(MapToResponse);
        }

        private static void ValidatePeriod(byte month, short year)
        {
            if (month < 1 || month > 12)
                throw new ArgumentException("Tháng tổng hợp công không hợp lệ.");
            if (year < 2000 || year > 2200)
                throw new ArgumentException("Năm tổng hợp công không hợp lệ.");
        }

        private static void EnsureHrOrAdmin(string actorRoleName)
        {
            if (!string.Equals(actorRoleName, "HR", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(actorRoleName, "Admin", StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Chỉ HR hoặc Admin được tổng hợp bảng công.");
        }

        private static decimal CountWorkDays(IEnumerable<AttendanceLog> logs)
        {
            return logs
                .Where(l => l.CheckIn.HasValue && l.CheckOut.HasValue)
                .Select(l => l.CheckIn!.Value.Date)
                .Distinct()
                .Count();
        }

        private static int CalculateLateMinutes(AttendanceLog log)
        {
            if (!log.CheckIn.HasValue || log.WorkShift?.StartTime == null)
                return 0;

            var latestValidCheckIn = log.WorkShift.StartTime.Value.Add(TimeSpan.FromMinutes(log.WorkShift.LateThresholdMins));
            return CalculateMinutesAfter(log.CheckIn.Value.TimeOfDay, latestValidCheckIn);
        }

        private static int CalculateEarlyLeaveMinutes(AttendanceLog log)
        {
            if (!log.CheckOut.HasValue || log.WorkShift?.EndTime == null)
                return 0;

            var earliestValidCheckOut = log.WorkShift.EndTime.Value.Subtract(TimeSpan.FromMinutes(log.WorkShift.EarlyLeaveThresholdMins));
            return CalculateMinutesAfter(earliestValidCheckOut, log.CheckOut.Value.TimeOfDay);
        }

        private static int CalculateMinutesAfter(TimeSpan later, TimeSpan earlier)
        {
            var minutes = (later - earlier).TotalMinutes;
            return minutes > 0 ? (int)Math.Ceiling(minutes) : 0;
        }

        private static AttendanceSummaryResponseDto MapToResponse(AttendanceSummary summary)
        {
            return new AttendanceSummaryResponseDto
            {
                Id = summary.Id,
                EmployeeId = summary.EmployeeId,
                EmployeeCode = summary.Employee.EmployeeCode,
                EmployeeName = summary.Employee.FullName,
                DepartmentName = summary.Employee.Department?.DeptName,
                Month = summary.Month,
                Year = summary.Year,
                WorkDays = summary.WorkDays,
                LateMinutes = summary.LateMinutes,
                EarlyLeaveMinutes = summary.EarlyLeaveMinutes,
                ActualOtMinutes = summary.ActualOtMinutes,
                IsPayrollLocked = summary.IsPayrollLocked,
                GeneratedAt = summary.GeneratedAt
            };
        }
    }
}
