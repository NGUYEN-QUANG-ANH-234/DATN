using HRM.backend.src.HRM.Application.DTOs.TimeAttendance;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Services;
using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Usecases;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;
using System.Text.Json;

namespace HRM.backend.src.HRM.Application.UseCases.TimeAttendance
{
    public class AttendanceSummaryUseCase : IAttendanceSummaryUseCase
    {
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IOvertimeRequestRepository _overtimeRepo;
        private readonly ILeaveRequestRepository _leaveReqRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAttendanceSummaryRepository _summaryRepo;
        private readonly IWorkCalendarConfigRepository _calendarRepo;
        private readonly ICompanyCalendarRepository _companyCalendarRepo;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;
        private readonly IOvertimeReconciliationService _overtimeReconciliationService;
        private readonly IAttendancePenaltyGeneratorService _attendancePenaltyGeneratorService;

        public AttendanceSummaryUseCase(
            IAttendanceRepository attendanceRepo,
            IOvertimeRequestRepository overtimeRepo,
            ILeaveRequestRepository leaveReqRepo,
            IEmployeeRepository employeeRepo,
            IAttendanceSummaryRepository summaryRepo,
            IWorkCalendarConfigRepository calendarRepo,
            ICompanyCalendarRepository companyCalendarRepo,
            IAuditLogRepository auditLogRepo,
            IUnitOfWork unitOfWork,
            ILockService lockService,
            IOvertimeReconciliationService overtimeReconciliationService,
            IAttendancePenaltyGeneratorService attendancePenaltyGeneratorService)
        {
            _attendanceRepo = attendanceRepo;
            _overtimeRepo = overtimeRepo;
            _leaveReqRepo = leaveReqRepo;
            _employeeRepo = employeeRepo;
            _summaryRepo = summaryRepo;
            _calendarRepo = calendarRepo;
            _companyCalendarRepo = companyCalendarRepo;
            _auditLogRepo = auditLogRepo;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
            _overtimeReconciliationService = overtimeReconciliationService;
            _attendancePenaltyGeneratorService = attendancePenaltyGeneratorService;
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
            var approvedLeaves = await _leaveReqRepo.GetApprovedByPeriodAsync(periodStart, periodEnd, ct);
            var calendarConfigs = await _calendarRepo.GetByPeriodAsync(dto.Month, dto.Year, ct);
            var companyCalendar = await _companyCalendarRepo.GetActiveByYearAsync(dto.Year, ct);
            var activeEmployees = (await _employeeRepo.GetActiveWithDepartmentAsync(ct))
                .Where(e => !e.JoinedDate.HasValue || e.JoinedDate.Value.Date < periodEnd)
                .ToList();
            var employeesById = activeEmployees.ToDictionary(e => e.Id);

            var employeeIds = activeEmployees
                .Select(e => e.Id)
                .Concat(logs
                .Where(l => l.EmployeeId.HasValue)
                    .Select(l => l.EmployeeId!.Value))
                .Concat(approvedOt.Select(o => o.EmployeeId))
                .Concat(approvedLeaves.Where(l => l.EmployeeId.HasValue).Select(l => l.EmployeeId!.Value))
                .Distinct()
                .ToList();

            foreach (var employeeId in employeeIds)
            {
                var employeeLogs = logs.Where(l => l.EmployeeId == employeeId).ToList();
                var employeeOt = approvedOt.Where(o => o.EmployeeId == employeeId && !o.IsPayrollLocked).ToList();
                var employeeLeaves = approvedLeaves.Where(l => l.EmployeeId == employeeId).ToList();
                employeesById.TryGetValue(employeeId, out var employee);
                var policy = ResolveWorkdayPolicy(employee, employeeLogs, calendarConfigs, companyCalendar);

                foreach (var otRequest in employeeOt)
                {
                    await _overtimeReconciliationService.ReconcileAsync(
                        otRequest,
                        FindOverlappingAttendanceLog(employeeLogs, otRequest),
                        ct);
                    await _overtimeRepo.UpdateAsync(otRequest, ct);
                }

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

                var workdayResult = CalculateWorkdayResult(employeeLogs, employeeLeaves, policy);
                summary.WorkedMinutes = workdayResult.WorkedMinutes;
                summary.WorkDays = workdayResult.WorkDays;
                summary.PayableWorkHours = workdayResult.PayableWorkHours;
                summary.LateMinutes = employeeLogs.Sum(CalculateLateMinutes);
                summary.EarlyLeaveMinutes = employeeLogs.Sum(CalculateEarlyLeaveMinutes);
                summary.ActualOtMinutes = employeeOt.Sum(o => o.ActualOtMinutes);
                summary.GeneratedAt = DateTime.UtcNow;

                var dailyResults = BuildDailyResults(
                    employee,
                    employeeLogs,
                    employeeOt,
                    employeeLeaves,
                    policy,
                    periodStart,
                    ResolveGenerationPeriodEnd(periodEnd));
                foreach (var dailyResult in dailyResults)
                {
                    var dailySummary = await _summaryRepo.GetDailyByEmployeeDateAsync(employeeId, dailyResult.WorkDate, ct);
                    if (dailySummary?.IsPayrollLocked == true ||
                        dailySummary?.ApprovalStatus == AttendancePayrollApprovalStatus.Locked ||
                        dailySummary?.IsManualAdjusted == true ||
                        dailySummary?.ApprovalStatus == AttendancePayrollApprovalStatus.Approved)
                        continue;

                    if (dailySummary == null)
                    {
                        dailySummary = new AttendanceDailySummary
                        {
                            EmployeeId = employeeId,
                            WorkDate = dailyResult.WorkDate
                        };
                        await _summaryRepo.AddDailyAsync(dailySummary, ct);
                    }

                    dailySummary.FirstCheckIn = dailyResult.FirstCheckIn;
                    dailySummary.LastCheckOut = dailyResult.LastCheckOut;
                    dailySummary.WorkingMinutes = dailyResult.WorkingMinutes;
                    dailySummary.LateMinutes = dailyResult.LateMinutes;
                    dailySummary.EarlyLeaveMinutes = dailyResult.EarlyLeaveMinutes;
                    dailySummary.OvertimeMinutes = dailyResult.OvertimeMinutes;
                    dailySummary.WorkdayValue = dailyResult.WorkdayValue;
                    dailySummary.AttendanceStatus = dailyResult.AttendanceStatus;
                    dailySummary.ApprovalStatus = AttendancePayrollApprovalStatus.PendingHRReview;
                    dailySummary.PayrollPeriod = $"{dto.Month:00}/{dto.Year}";
                    dailySummary.GeneratedAt = DateTime.UtcNow;
                }
            }

            await _auditLogRepo.LogSystemEventAsync("GENERATE_ATTENDANCE_SUMMARY", actorAccountId, "attendance_summaries", $"Tổng hợp bảng công tháng {dto.Month:D2}/{dto.Year}");
            await _unitOfWork.CommitAsync(ct);

            var generatedDailySummaries = await _summaryRepo.GetDailyByPeriodAsync(dto.Month, dto.Year, ct);
            await _attendancePenaltyGeneratorService.GenerateFromDailySummariesAsync(generatedDailySummaries, ct);
            await _unitOfWork.CommitAsync(ct);

            return await GetMonthlyAsync(dto.Month, dto.Year, actorRoleName, ct);
        }

        public async Task<IEnumerable<AttendanceDailySummaryResponseDto>> GetDailyAsync(byte month, short year, string actorRoleName, CancellationToken ct = default)
        {
            EnsureHrOrAdmin(actorRoleName);
            ValidatePeriod(month, year);

            var daily = await _summaryRepo.GetDailyByPeriodAsync(month, year, ct);
            return daily.Select(MapDailyToResponse);
        }

        public async Task<AttendanceDailySummaryResponseDto> AdjustDailyAsync(int id, AdjustAttendanceDailySummaryDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            EnsureHrOrAdmin(actorRoleName);
            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new ArgumentException("Can nhap ly do dieu chinh bang cong.");

            var daily = await _summaryRepo.GetDailyByIdAsync(id, ct)
                ?? throw new ArgumentException("Không tìm thấy dòng bảng công ngày.");
            if (daily.IsPayrollLocked || daily.ApprovalStatus == AttendancePayrollApprovalStatus.Locked)
                throw new ArgumentException("Bảng công ngày đã khóa, không thể điều chỉnh.");

            var oldValue = SnapshotDaily(daily);
            if (dto.WorkingMinutes.HasValue) daily.WorkingMinutes = Math.Max(0, dto.WorkingMinutes.Value);
            if (dto.LateMinutes.HasValue) daily.LateMinutes = Math.Max(0, dto.LateMinutes.Value);
            if (dto.EarlyLeaveMinutes.HasValue) daily.EarlyLeaveMinutes = Math.Max(0, dto.EarlyLeaveMinutes.Value);
            if (dto.OvertimeMinutes.HasValue) daily.OvertimeMinutes = Math.Max(0, dto.OvertimeMinutes.Value);
            if (dto.WorkdayValue.HasValue) daily.WorkdayValue = Math.Min(1m, Math.Max(0m, dto.WorkdayValue.Value));
            if (dto.AttendanceStatus.HasValue) daily.AttendanceStatus = dto.AttendanceStatus.Value;

            daily.IsManualAdjusted = true;
            daily.AdjustedByAccountId = actorAccountId;
            daily.AdjustedAt = DateTime.UtcNow;
            daily.AdjustmentReason = dto.Reason.Trim();
            daily.ApprovalStatus = AttendancePayrollApprovalStatus.Approved;

            await _summaryRepo.AddAdjustmentLogAsync(new AttendanceAdjustmentLog
            {
                AttendanceDailySummaryId = daily.Id,
                OldValueJson = oldValue,
                NewValueJson = SnapshotDaily(daily),
                AdjustedByAccountId = actorAccountId,
                Reason = dto.Reason.Trim(),
                AdjustedAt = DateTime.UtcNow
            }, ct);
            await _auditLogRepo.LogSystemEventAsync("ADJUST_ATTENDANCE_DAILY_SUMMARY", actorAccountId, "attendance_daily_summaries", $"Dieu chinh bang cong ngay Id {id}: {dto.Reason}");
            await _unitOfWork.CommitAsync(ct);

            return MapDailyToResponse(daily);
        }

        public async Task<AttendanceDailySummaryResponseDto> ApproveDailyAsync(int id, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            EnsureHrOrAdmin(actorRoleName);

            var daily = await _summaryRepo.GetDailyByIdAsync(id, ct)
                ?? throw new ArgumentException("Không tìm thấy dòng bảng công ngày.");
            if (daily.IsPayrollLocked || daily.ApprovalStatus == AttendancePayrollApprovalStatus.Locked)
                throw new ArgumentException("Bảng công ngày đã khóa, không thể phê duyệt lại.");

            daily.ApprovalStatus = AttendancePayrollApprovalStatus.Approved;
            await _auditLogRepo.LogSystemEventAsync("APPROVE_ATTENDANCE_DAILY_SUMMARY", actorAccountId, "attendance_daily_summaries", $"Phe duyet bang cong ngay Id {id}");
            await _unitOfWork.CommitAsync(ct);

            return MapDailyToResponse(daily);
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

        private static WorkdayPolicy ResolveWorkdayPolicy(
            Employee? employee,
            List<AttendanceLog> employeeLogs,
            List<WorkCalendarConfig> calendarConfigs,
            CompanyCalendar? companyCalendar)
        {
            var deptId = employee?.DeptId ?? employeeLogs
                .Select(l => l.Employee?.DeptId)
                .FirstOrDefault(id => id.HasValue);
            var config = deptId.HasValue
                ? calendarConfigs.FirstOrDefault(c => c.DeptId == deptId.Value)
                : null;
            var holidayDates = ParseHolidayDates(config?.HolidayDatesJson);
            holidayDates.UnionWith(ResolveCompanyDayOffDates(companyCalendar));

            return new WorkdayPolicy(
                config is { StandardHoursPerDay: > 0 } ? config.StandardHoursPerDay : 8m,
                config?.IncludePaidLeaveInWorkDays ?? true,
                ParseWorkingDays(config?.WorkingDaysOfWeek),
                holidayDates,
                ResolveCompanyWorkingDayOverrides(companyCalendar));
        }

        private static WorkdayResult CalculateWorkdayResult(
            IEnumerable<AttendanceLog> logs,
            IEnumerable<LeaveRequest> approvedLeaves,
            WorkdayPolicy policy)
        {
            var workedMinutes = 0;
            decimal workDays = 0;
            decimal payableHours = 0;
            var leaveRequests = approvedLeaves.ToList();

            foreach (var dayGroup in logs.Where(l => l.CheckIn.HasValue).GroupBy(l => l.CheckIn!.Value.Date))
            {
                var workDate = dayGroup.Key;
                var leaveType = ResolveLeaveTypeForDate(leaveRequests, workDate);
                var dayWorkedMinutes = dayGroup.Sum(CalculateWorkedMinutes);
                var hasLeave = dayGroup.Any(l => l.Status == AttendanceStatus.OnLeave);

                if (dayWorkedMinutes > 0)
                {
                    var dayPayableHours = ConvertMinutesToPayableHours(dayWorkedMinutes, policy);
                    var dayWorkDays = ConvertPayableHoursToWorkDays(dayPayableHours, policy);
                    workedMinutes += dayWorkedMinutes;
                    workDays += dayWorkDays;
                    payableHours += dayPayableHours;
                }
                else if (hasLeave && ShouldCountLeaveAsWorkday(leaveType, policy))
                {
                    workDays += 1m;
                    payableHours += policy.StandardHoursPerDay;
                }
            }

            return new WorkdayResult(
                workedMinutes,
                Math.Round(workDays, 2, MidpointRounding.AwayFromZero),
                Math.Round(payableHours, 2, MidpointRounding.AwayFromZero));
        }

        private static List<DailyWorkdayResult> BuildDailyResults(
            Employee? employee,
            IEnumerable<AttendanceLog> logs,
            IEnumerable<OvertimeRequest> overtimeRequests,
            IEnumerable<LeaveRequest> approvedLeaves,
            WorkdayPolicy policy,
            DateTime periodStart,
            DateTime periodEnd)
        {
            var leaveRequests = approvedLeaves.ToList();
            var overtimeList = overtimeRequests.ToList();
            var logGroups = logs
                .Where(l => l.CheckIn.HasValue || l.CheckOut.HasValue || l.Status == AttendanceStatus.OnLeave)
                .GroupBy(l => l.WorkDate.Date)
                .ToDictionary(g => g.Key, g => g.OrderBy(l => l.CheckIn ?? l.WorkDate).ToList());
            var effectiveStart = employee?.JoinedDate.HasValue == true && employee.JoinedDate.Value.Date > periodStart
                ? employee.JoinedDate.Value.Date
                : periodStart;
            var results = new List<DailyWorkdayResult>();

            foreach (var workDate in EnumerateDates(effectiveStart, periodEnd))
            {
                logGroups.TryGetValue(workDate, out var orderedLogs);
                orderedLogs ??= new List<AttendanceLog>();

                var leaveType = ResolveLeaveTypeForDate(leaveRequests, workDate);
                var hasLeave = leaveType != null || orderedLogs.Any(l => l.Status == AttendanceStatus.OnLeave);
                var isWorkingDate = IsWorkingDate(workDate, policy);
                var hasAttendanceSignal = orderedLogs.Count > 0 || hasLeave || overtimeList.Any(o => o.WorkDate.Date == workDate);
                if (!isWorkingDate && !hasAttendanceSignal)
                    continue;

                var workedMinutes = orderedLogs.Sum(CalculateWorkedMinutes);
                var payableHours = ConvertMinutesToPayableHours(workedMinutes, policy);
                var workdayValue = ConvertPayableHoursToWorkDays(payableHours, policy);
                if (workedMinutes == 0 && hasLeave && ShouldCountLeaveAsWorkday(leaveType, policy))
                    workdayValue = 1m;

                var status = ResolveDailyStatus(workdayValue, workedMinutes, hasLeave, leaveType);
                var firstCheckIn = orderedLogs
                    .Where(l => l.CheckIn.HasValue)
                    .Select(l => l.CheckIn)
                    .OrderBy(v => v)
                    .FirstOrDefault();
                var lastCheckOut = orderedLogs
                    .Where(l => l.CheckOut.HasValue)
                    .Select(l => l.CheckOut)
                    .OrderByDescending(v => v)
                    .FirstOrDefault();

                results.Add(new DailyWorkdayResult(
                    workDate,
                    firstCheckIn,
                    lastCheckOut,
                    workedMinutes,
                    orderedLogs.Sum(CalculateLateMinutes),
                    orderedLogs.Sum(CalculateEarlyLeaveMinutes),
                    overtimeList.Where(o => o.WorkDate.Date == workDate).Sum(o => o.ActualOtMinutes),
                    Math.Round(workdayValue, 2, MidpointRounding.AwayFromZero),
                    status));
            }

            return results;
        }

        private static AttendanceDailyStatus ResolveDailyStatus(decimal workdayValue, int workedMinutes, bool hasLeave, LeaveType? leaveType)
        {
            if (hasLeave && workedMinutes == 0)
            {
                return leaveType?.Category switch
                {
                    LeaveCategory.Maternity => AttendanceDailyStatus.MaternityLeave,
                    LeaveCategory.Sick => AttendanceDailyStatus.SickLeave,
                    LeaveCategory.Unpaid => AttendanceDailyStatus.UnpaidLeave,
                    _ => AttendanceDailyStatus.PaidLeave
                };
            }

            if (workdayValue <= 0) return AttendanceDailyStatus.Absence;
            if (workdayValue < 1m) return AttendanceDailyStatus.HalfDay;
            return AttendanceDailyStatus.Present;
        }

        private static LeaveType? ResolveLeaveTypeForDate(IEnumerable<LeaveRequest> leaveRequests, DateTime workDate)
        {
            return leaveRequests
                .Where(r => r.StartDate.HasValue &&
                            r.EndDate.HasValue &&
                            r.StartDate.Value.Date <= workDate.Date &&
                            r.EndDate.Value.Date >= workDate.Date)
                .OrderByDescending(r => r.LeaveType?.Category == LeaveCategory.Maternity)
                .ThenByDescending(r => r.StartDate)
                .Select(r => r.LeaveType)
                .FirstOrDefault();
        }

        private static bool ShouldCountLeaveAsWorkday(LeaveType? leaveType, WorkdayPolicy policy)
        {
            if (!policy.IncludePaidLeaveInWorkDays)
                return false;

            return leaveType == null
                ? true
                : leaveType.IsPaid && leaveType.CountsAsWorkday;
        }

        private static AttendanceLog? FindOverlappingAttendanceLog(
            IEnumerable<AttendanceLog> logs,
            OvertimeRequest request)
        {
            return logs
                .Where(l => l.CheckIn.HasValue &&
                            l.CheckOut.HasValue &&
                            l.CheckIn.Value < request.EndAt &&
                            l.CheckOut.Value > request.StartAt)
                .OrderByDescending(l => l.CheckIn)
                .FirstOrDefault();
        }

        private static int CalculateWorkedMinutes(AttendanceLog log)
        {
            if (!log.CheckIn.HasValue || !log.CheckOut.HasValue)
                return 0;

            var totalMinutes = (int)Math.Max(0, Math.Floor((log.CheckOut.Value - log.CheckIn.Value).TotalMinutes));
            var breakMinutes = CalculateBreakOverlapMinutes(log);
            return Math.Max(0, totalMinutes - breakMinutes);
        }

        private static int CalculateBreakOverlapMinutes(AttendanceLog log)
        {
            if (log.WorkShift?.BreakStartTime == null || log.WorkShift.BreakEndTime == null)
                return 0;

            var checkIn = log.CheckIn!.Value;
            var checkOut = log.CheckOut!.Value;
            var breakStart = checkIn.Date.Add(log.WorkShift.BreakStartTime.Value);
            var breakEnd = checkIn.Date.Add(log.WorkShift.BreakEndTime.Value);

            var overlapStart = checkIn > breakStart ? checkIn : breakStart;
            var overlapEnd = checkOut < breakEnd ? checkOut : breakEnd;
            return overlapEnd > overlapStart
                ? (int)Math.Floor((overlapEnd - overlapStart).TotalMinutes)
                : 0;
        }

        private static decimal ConvertMinutesToPayableHours(
            int workedMinutes,
            WorkdayPolicy policy)
        {
            var workedHours = Math.Round(workedMinutes / 60m, 2, MidpointRounding.AwayFromZero);
            return Math.Min(workedHours, policy.StandardHoursPerDay);
        }

        private static decimal ConvertPayableHoursToWorkDays(
            decimal payableHours,
            WorkdayPolicy policy)
        {
            return policy.StandardHoursPerDay <= 0
                ? 0m
                : Math.Min(1m, Math.Max(0m, payableHours / policy.StandardHoursPerDay));
        }

        private static DateTime ResolveGenerationPeriodEnd(DateTime periodEnd)
        {
            var todayExclusive = DateTime.UtcNow.Date.AddDays(1);
            return periodEnd > todayExclusive ? todayExclusive : periodEnd;
        }

        private static IEnumerable<DateTime> EnumerateDates(DateTime startInclusive, DateTime endExclusive)
        {
            for (var date = startInclusive.Date; date < endExclusive.Date; date = date.AddDays(1))
                yield return date;
        }

        private static bool IsWorkingDate(DateTime workDate, WorkdayPolicy policy)
        {
            if (policy.WorkingDayOverrides.Contains(workDate.Date))
                return true;

            return policy.WorkingDaysOfWeek.Contains(workDate.DayOfWeek) &&
                   !policy.HolidayDates.Contains(workDate.Date);
        }

        private static HashSet<DateTime> ResolveCompanyDayOffDates(CompanyCalendar? companyCalendar)
        {
            if (companyCalendar == null)
                return new HashSet<DateTime>();

            return companyCalendar.Days
                .Where(day => !day.IsWorkingDayOverride &&
                              day.DayType is CompanyCalendarDayType.PublicHoliday
                                  or CompanyCalendarDayType.CompanyHoliday
                                  or CompanyCalendarDayType.CompensatoryDayOff
                                  or CompanyCalendarDayType.SpecialPaidLeave
                                  or CompanyCalendarDayType.UnpaidCompanyClosure)
                .Select(day => day.Date.Date)
                .ToHashSet();
        }

        private static HashSet<DateTime> ResolveCompanyWorkingDayOverrides(CompanyCalendar? companyCalendar)
        {
            if (companyCalendar == null)
                return new HashSet<DateTime>();

            return companyCalendar.Days
                .Where(day => day.IsWorkingDayOverride ||
                              day.DayType == CompanyCalendarDayType.CompensatoryWorkingDay)
                .Select(day => day.Date.Date)
                .ToHashSet();
        }

        private static HashSet<DayOfWeek> ParseWorkingDays(string? workingDaysOfWeek)
        {
            var defaultWorkingDays = new HashSet<DayOfWeek>
            {
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday
            };

            if (string.IsNullOrWhiteSpace(workingDaysOfWeek))
                return defaultWorkingDays;

            var parsed = new HashSet<DayOfWeek>();
            foreach (var token in workingDaysOfWeek.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(token.Trim(), out var dayNumber))
                {
                    if (dayNumber == 7)
                        parsed.Add(DayOfWeek.Sunday);
                    else if (dayNumber is >= 0 and <= 6)
                        parsed.Add((DayOfWeek)dayNumber);

                    continue;
                }

                if (Enum.TryParse<DayOfWeek>(token.Trim(), true, out var dayOfWeek))
                    parsed.Add(dayOfWeek);
            }

            return parsed.Count > 0 ? parsed : defaultWorkingDays;
        }

        private static HashSet<DateTime> ParseHolidayDates(string? holidayDatesJson)
        {
            if (string.IsNullOrWhiteSpace(holidayDatesJson))
                return new HashSet<DateTime>();

            try
            {
                var dateStrings = JsonSerializer.Deserialize<List<string>>(holidayDatesJson) ?? new List<string>();
                return dateStrings
                    .Where(value => DateTime.TryParse(value, out _))
                    .Select(value => DateTime.Parse(value).Date)
                    .ToHashSet();
            }
            catch (JsonException)
            {
                return holidayDatesJson
                    .Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(value => DateTime.TryParse(value, out _))
                    .Select(value => DateTime.Parse(value).Date)
                    .ToHashSet();
            }
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
                WorkedMinutes = summary.WorkedMinutes,
                WorkedHours = Math.Round(summary.WorkedMinutes / 60m, 2, MidpointRounding.AwayFromZero),
                PayableWorkHours = summary.PayableWorkHours,
                LateMinutes = summary.LateMinutes,
                EarlyLeaveMinutes = summary.EarlyLeaveMinutes,
                ActualOtMinutes = summary.ActualOtMinutes,
                IsPayrollLocked = summary.IsPayrollLocked,
                GeneratedAt = summary.GeneratedAt
            };
        }

        private static AttendanceDailySummaryResponseDto MapDailyToResponse(AttendanceDailySummary summary)
        {
            return new AttendanceDailySummaryResponseDto
            {
                Id = summary.Id,
                EmployeeId = summary.EmployeeId,
                EmployeeCode = summary.Employee.EmployeeCode,
                EmployeeName = summary.Employee.FullName,
                DepartmentName = summary.Employee.Department?.DeptName,
                WorkDate = summary.WorkDate,
                FirstCheckIn = summary.FirstCheckIn,
                LastCheckOut = summary.LastCheckOut,
                WorkingMinutes = summary.WorkingMinutes,
                LateMinutes = summary.LateMinutes,
                EarlyLeaveMinutes = summary.EarlyLeaveMinutes,
                OvertimeMinutes = summary.OvertimeMinutes,
                WorkdayValue = summary.WorkdayValue,
                AttendanceStatus = summary.AttendanceStatus,
                ApprovalStatus = summary.ApprovalStatus,
                IsManualAdjusted = summary.IsManualAdjusted,
                AdjustmentReason = summary.AdjustmentReason,
                IsPayrollLocked = summary.IsPayrollLocked,
                GeneratedAt = summary.GeneratedAt
            };
        }

        private static string SnapshotDaily(AttendanceDailySummary summary)
        {
            return JsonSerializer.Serialize(new
            {
                summary.WorkingMinutes,
                summary.LateMinutes,
                summary.EarlyLeaveMinutes,
                summary.OvertimeMinutes,
                summary.WorkdayValue,
                summary.AttendanceStatus,
                summary.ApprovalStatus,
                summary.AdjustmentReason
            });
        }

        private sealed record WorkdayPolicy(
            decimal StandardHoursPerDay,
            bool IncludePaidLeaveInWorkDays,
            HashSet<DayOfWeek> WorkingDaysOfWeek,
            HashSet<DateTime> HolidayDates,
            HashSet<DateTime> WorkingDayOverrides);

        private sealed record WorkdayResult(
            int WorkedMinutes,
            decimal WorkDays,
            decimal PayableWorkHours);

        private sealed record DailyWorkdayResult(
            DateTime WorkDate,
            DateTime? FirstCheckIn,
            DateTime? LastCheckOut,
            int WorkingMinutes,
            int LateMinutes,
            int EarlyLeaveMinutes,
            int OvertimeMinutes,
            decimal WorkdayValue,
            AttendanceDailyStatus AttendanceStatus);
    }
}
