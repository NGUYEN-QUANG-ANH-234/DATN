using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Services;
using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;

namespace HRM.backend.src.HRM.Application.Services.TimeAttendance
{
    public class AttendancePenaltyGeneratorService : IAttendancePenaltyGeneratorService
    {
        private const int DailyLateThresholdMinutes = 30;
        private const int MonthlyLateThresholdMinutes = 120;
        private const int DailyEarlyLeaveThresholdMinutes = 30;
        private const int MonthlyEarlyLeaveCountThreshold = 3;

        private readonly IPenaltyRecordRepository _penaltyRecordRepo;

        public AttendancePenaltyGeneratorService(IPenaltyRecordRepository penaltyRecordRepo)
        {
            _penaltyRecordRepo = penaltyRecordRepo;
        }

        public async Task GenerateFromDailySummariesAsync(IEnumerable<AttendanceDailySummary> dailySummaries, CancellationToken ct = default)
        {
            var summaries = dailySummaries
                .Where(s => !s.IsPayrollLocked)
                .OrderBy(s => s.EmployeeId)
                .ThenBy(s => s.WorkDate)
                .ToList();

            foreach (var summary in summaries)
            {
                await GenerateDailyLatePenaltyAsync(summary, ct);
                await GenerateDailyEarlyLeavePenaltyAsync(summary, ct);
                await GenerateAbsencePenaltyAsync(summary, ct);
                await GenerateUnpaidLeaveReviewRecordAsync(summary, ct);
            }

            foreach (var employeePeriod in summaries.GroupBy(s => new
            {
                s.EmployeeId,
                Period = ResolvePeriod(s.WorkDate)
            }))
            {
                await GenerateMonthlyLatePenaltyAsync(employeePeriod.Key.EmployeeId, employeePeriod.Key.Period, employeePeriod.ToList(), ct);
                await GenerateMonthlyEarlyLeavePenaltyAsync(employeePeriod.Key.EmployeeId, employeePeriod.Key.Period, employeePeriod.ToList(), ct);
            }
        }

        private async Task GenerateDailyLatePenaltyAsync(AttendanceDailySummary summary, CancellationToken ct)
        {
            if (summary.LateMinutes <= DailyLateThresholdMinutes)
                return;

            await AddDailyPenaltyIfNeededAsync(
                summary,
                "ATTENDANCE_LATE_DAILY_OVER_30",
                ViolationType.AttendanceLate,
                PenaltySeverity.Low,
                0.5m,
                $"Đi muộn {summary.LateMinutes} phút vào ngày {summary.WorkDate:dd/MM/yyyy}.",
                affectsAttendance: true,
                affectsPerformance: true,
                affectsPersonnelDecision: false,
                deductedMinutes: summary.LateMinutes,
                deductedWorkday: null,
                ct);
        }

        private async Task GenerateDailyEarlyLeavePenaltyAsync(AttendanceDailySummary summary, CancellationToken ct)
        {
            if (summary.EarlyLeaveMinutes <= DailyEarlyLeaveThresholdMinutes)
                return;

            await AddDailyPenaltyIfNeededAsync(
                summary,
                "EARLY_LEAVE_DAILY_OVER_30",
                ViolationType.EarlyLeave,
                PenaltySeverity.Low,
                0.5m,
                $"Về sớm {summary.EarlyLeaveMinutes} phút vào ngày {summary.WorkDate:dd/MM/yyyy}.",
                affectsAttendance: true,
                affectsPerformance: true,
                affectsPersonnelDecision: false,
                deductedMinutes: summary.EarlyLeaveMinutes,
                deductedWorkday: null,
                ct);
        }

        private async Task GenerateAbsencePenaltyAsync(AttendanceDailySummary summary, CancellationToken ct)
        {
            if (summary.AttendanceStatus != AttendanceDailyStatus.Absence)
                return;

            await AddDailyPenaltyIfNeededAsync(
                summary,
                "UNAUTHORIZED_ABSENCE_DAILY",
                ViolationType.UnauthorizedAbsence,
                PenaltySeverity.High,
                1.5m,
                $"Vắng mặt không ghi nhận công/không có phép vào ngày {summary.WorkDate:dd/MM/yyyy}.",
                affectsAttendance: true,
                affectsPerformance: true,
                affectsPersonnelDecision: true,
                deductedMinutes: null,
                deductedWorkday: 1m,
                ct);
        }

        private async Task GenerateUnpaidLeaveReviewRecordAsync(AttendanceDailySummary summary, CancellationToken ct)
        {
            if (summary.AttendanceStatus != AttendanceDailyStatus.UnpaidLeave)
                return;

            await AddDailyPenaltyIfNeededAsync(
                summary,
                "UNPAID_LEAVE_ATTENDANCE_REVIEW",
                ViolationType.ProcessViolation,
                PenaltySeverity.Low,
                0m,
                $"Ngày nghỉ không lương {summary.WorkDate:dd/MM/yyyy} cần đối chiếu trước khi chốt công.",
                affectsAttendance: true,
                affectsPerformance: false,
                affectsPersonnelDecision: false,
                deductedMinutes: null,
                deductedWorkday: summary.WorkdayValue > 0 ? summary.WorkdayValue : 1m,
                ct);
        }

        private async Task GenerateMonthlyLatePenaltyAsync(int employeeId, string period, List<AttendanceDailySummary> summaries, CancellationToken ct)
        {
            var totalLateMinutes = summaries.Sum(s => s.LateMinutes);
            if (totalLateMinutes <= MonthlyLateThresholdMinutes)
                return;

            await AddMonthlyPenaltyIfNeededAsync(
                employeeId,
                period,
                "ATTENDANCE_LATE_MONTHLY_OVER_120",
                ViolationType.AttendanceLate,
                PenaltySeverity.Medium,
                1m,
                $"Tổng số phút đi muộn trong kỳ {period} là {totalLateMinutes} phút, vượt ngưỡng {MonthlyLateThresholdMinutes} phút.",
                affectsAttendance: false,
                affectsPerformance: true,
                affectsPersonnelDecision: true,
                deductedMinutes: null,
                ct);
        }

        private async Task GenerateMonthlyEarlyLeavePenaltyAsync(int employeeId, string period, List<AttendanceDailySummary> summaries, CancellationToken ct)
        {
            var earlyLeaveDays = summaries.Count(s => s.EarlyLeaveMinutes > 0);
            var totalEarlyLeaveMinutes = summaries.Sum(s => s.EarlyLeaveMinutes);
            if (earlyLeaveDays < MonthlyEarlyLeaveCountThreshold)
                return;

            await AddMonthlyPenaltyIfNeededAsync(
                employeeId,
                period,
                "EARLY_LEAVE_REPEATED_MONTHLY",
                ViolationType.EarlyLeave,
                PenaltySeverity.Medium,
                1m,
                $"Về sớm {earlyLeaveDays} ngày trong kỳ {period}, tổng {totalEarlyLeaveMinutes} phút.",
                affectsAttendance: false,
                affectsPerformance: true,
                affectsPersonnelDecision: true,
                deductedMinutes: null,
                ct);
        }

        private async Task AddDailyPenaltyIfNeededAsync(
            AttendanceDailySummary summary,
            string ruleCode,
            ViolationType violationType,
            PenaltySeverity severity,
            decimal penaltyPoint,
            string reason,
            bool affectsAttendance,
            bool affectsPerformance,
            bool affectsPersonnelDecision,
            int? deductedMinutes,
            decimal? deductedWorkday,
            CancellationToken ct)
        {
            if (summary.Id <= 0)
                return;

            if (await _penaltyRecordRepo.ExistsForReferenceAsync(PenaltySourceType.Attendance, summary.Id, ruleCode, ct))
                return;

            await _penaltyRecordRepo.AddAsync(new PenaltyRecord
            {
                EmployeeId = summary.EmployeeId,
                Period = ResolvePeriod(summary.WorkDate),
                SourceType = PenaltySourceType.Attendance,
                ReferenceId = summary.Id,
                RuleCode = ruleCode,
                PenaltyPoint = penaltyPoint,
                Reason = reason,
                Status = PenaltyRecordStatus.PendingHRReview,
                OccurredAt = summary.WorkDate,
                ViolationType = violationType,
                Severity = severity,
                AffectsAttendance = affectsAttendance,
                AffectsPerformance = affectsPerformance,
                AffectsPersonnelDecision = affectsPersonnelDecision,
                DeductedMinutes = deductedMinutes,
                DeductedWorkday = deductedWorkday,
                CreatedBySystem = true,
                CreatedAt = DateTime.UtcNow
            }, ct);
        }

        private async Task AddMonthlyPenaltyIfNeededAsync(
            int employeeId,
            string period,
            string ruleCode,
            ViolationType violationType,
            PenaltySeverity severity,
            decimal penaltyPoint,
            string reason,
            bool affectsAttendance,
            bool affectsPerformance,
            bool affectsPersonnelDecision,
            int? deductedMinutes,
            CancellationToken ct)
        {
            if (await _penaltyRecordRepo.ExistsForEmployeePeriodRuleAsync(employeeId, period, PenaltySourceType.Attendance, ruleCode, ct))
                return;

            await _penaltyRecordRepo.AddAsync(new PenaltyRecord
            {
                EmployeeId = employeeId,
                Period = period,
                SourceType = PenaltySourceType.Attendance,
                ReferenceId = null,
                RuleCode = ruleCode,
                PenaltyPoint = penaltyPoint,
                Reason = reason,
                Status = PenaltyRecordStatus.PendingHRReview,
                OccurredAt = ResolvePeriodStart(period),
                ViolationType = violationType,
                Severity = severity,
                AffectsAttendance = affectsAttendance,
                AffectsPerformance = affectsPerformance,
                AffectsPersonnelDecision = affectsPersonnelDecision,
                DeductedMinutes = deductedMinutes,
                CreatedBySystem = true,
                CreatedAt = DateTime.UtcNow
            }, ct);
        }

        private static string ResolvePeriod(DateTime workDate) => $"{workDate.Month:00}/{workDate.Year}";

        private static DateTime ResolvePeriodStart(string period)
        {
            var parts = period.Split('/');
            return parts.Length == 2 &&
                   int.TryParse(parts[0], out var month) &&
                   int.TryParse(parts[1], out var year)
                ? new DateTime(year, month, 1)
                : DateTime.UtcNow.Date;
        }
    }
}
