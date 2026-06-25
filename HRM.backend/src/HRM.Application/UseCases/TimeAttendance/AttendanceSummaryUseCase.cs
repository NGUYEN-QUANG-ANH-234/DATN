using HRM.backend.src.HRM.Application.DTOs.TimeAttendance;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Services;
using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Usecases;
using HRM.backend.src.HRM.Application.Services.System;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

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
                LockKeys.AttendancePeriod(dto.Month, dto.Year),
                async (innerCt) => await GenerateMonthlyCoreAsync(dto, actorAccountId, actorRoleName, innerCt),
                cancellationToken: ct);
        }

        public async Task<IEnumerable<AttendancePeriodApprovalDto>> GetPendingApprovalPeriodsAsync(string actorRoleName, CancellationToken ct = default)
        {
            EnsureDirectorOrAdmin(actorRoleName);

            var pending = await _summaryRepo.GetPendingApprovalAsync(ct);
            return pending
                .GroupBy(x => new { x.Month, x.Year })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g => new AttendancePeriodApprovalDto
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Period = BuildPayrollPeriod(g.Key.Month, g.Key.Year),
                    Summaries = g.Select(MapToResponse).ToList()
                })
                .ToList();
        }

        public async Task<IEnumerable<AttendanceSummaryResponseDto>> SubmitMonthlyTimesheetAsync(CloseAttendancePeriodDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            EnsureHrOrAdmin(actorRoleName);
            ValidatePeriod(dto.Month, dto.Year);

            return await _lockService.GetWithLockAsync(
                LockKeys.AttendancePeriod(dto.Month, dto.Year),
                async (innerCt) =>
                {
                    var summaries = await LoadMonthlySummariesOrThrowAsync(dto.Month, dto.Year, innerCt);
                    if (summaries.All(s => s.IsPayrollLocked || s.ApprovalStatus == AttendancePayrollApprovalStatus.Locked))
                        throw new ArgumentException("Kỳ công đã khóa, không thể gửi chốt lại.");
                    if (summaries.Any(s => s.ApprovalStatus is not AttendancePayrollApprovalStatus.Draft and not AttendancePayrollApprovalStatus.PendingHRReview))
                        throw new ArgumentException("Chỉ kỳ công ở trạng thái bản nháp mới được gửi chốt.");

                    var now = DateTime.UtcNow;
                    var note = NormalizeNote(dto.Note);
                    foreach (var summary in summaries.Where(s => s.ApprovalStatus == AttendancePayrollApprovalStatus.Draft))
                    {
                        summary.ApprovalStatus = AttendancePayrollApprovalStatus.PendingHRReview;
                        summary.SubmittedByAccountId = actorAccountId;
                        summary.SubmittedAt = now;
                        summary.PeriodNote = note;
                    }

                    var dailySummaries = await _summaryRepo.GetDailyByPeriodAsync(dto.Month, dto.Year, innerCt);
                    foreach (var daily in dailySummaries.Where(d =>
                                 !d.IsPayrollLocked &&
                                 d.ApprovalStatus == AttendancePayrollApprovalStatus.Draft))
                    {
                        daily.ApprovalStatus = AttendancePayrollApprovalStatus.PendingHRReview;
                        daily.PayrollPeriod = BuildPayrollPeriod(dto.Month, dto.Year);
                    }

                    await _auditLogRepo.LogSystemEventAsync(
                        "SUBMIT_MONTHLY_TIMESHEET",
                        actorAccountId,
                        "attendance_summaries",
                        $"Gửi chốt kỳ công {dto.Month:D2}/{dto.Year}");
                    await _unitOfWork.CommitAsync(innerCt);

                    return await GetMonthlyAsync(dto.Month, dto.Year, actorRoleName, innerCt);
                },
                cancellationToken: ct);
        }

        public async Task<IEnumerable<AttendanceSummaryResponseDto>> ApproveMonthlyTimesheetAsync(CloseAttendancePeriodDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            EnsureDirectorOrAdmin(actorRoleName);
            ValidatePeriod(dto.Month, dto.Year);

            return await _lockService.GetWithLockAsync(
                LockKeys.AttendancePeriod(dto.Month, dto.Year),
                async (innerCt) =>
                {
                    var summaries = await LoadMonthlySummariesOrThrowAsync(dto.Month, dto.Year, innerCt);
                    if (summaries.Any(s => s.IsPayrollLocked || s.ApprovalStatus == AttendancePayrollApprovalStatus.Locked))
                        throw new ArgumentException("Kỳ công đã khóa, không thể duyệt lại.");
                    if (summaries.Any(s => s.ApprovalStatus is not AttendancePayrollApprovalStatus.PendingHRReview and not AttendancePayrollApprovalStatus.Approved))
                        throw new ArgumentException("Chỉ kỳ công đã gửi chốt mới được duyệt.");

                    var now = DateTime.UtcNow;
                    var note = NormalizeNote(dto.Note);
                    foreach (var summary in summaries.Where(s => s.ApprovalStatus == AttendancePayrollApprovalStatus.PendingHRReview))
                    {
                        summary.ApprovalStatus = AttendancePayrollApprovalStatus.Approved;
                        summary.ApprovedByAccountId = actorAccountId;
                        summary.ApprovedAt = now;
                        summary.PeriodNote = note ?? summary.PeriodNote;
                    }

                    var dailySummaries = await _summaryRepo.GetDailyByPeriodAsync(dto.Month, dto.Year, innerCt);
                    foreach (var daily in dailySummaries.Where(d => !d.IsPayrollLocked && d.ApprovalStatus != AttendancePayrollApprovalStatus.Locked))
                    {
                        daily.ApprovalStatus = AttendancePayrollApprovalStatus.Approved;
                        daily.PayrollPeriod = BuildPayrollPeriod(dto.Month, dto.Year);
                    }

                    await _auditLogRepo.LogSystemEventAsync(
                        "APPROVE_MONTHLY_TIMESHEET",
                        actorAccountId,
                        "attendance_summaries",
                        $"Duyệt kỳ công {dto.Month:D2}/{dto.Year}");
                    await _unitOfWork.CommitAsync(innerCt);

                    return await GetMonthlyAsync(dto.Month, dto.Year, actorRoleName, innerCt);
                },
                cancellationToken: ct);
        }

        public async Task<IEnumerable<AttendanceSummaryResponseDto>> LockMonthlyTimesheetAsync(CloseAttendancePeriodDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            EnsureHrOrAdmin(actorRoleName);
            ValidatePeriod(dto.Month, dto.Year);

            return await _lockService.GetWithLockAsync(
                LockKeys.AttendancePeriod(dto.Month, dto.Year),
                async (innerCt) =>
                {
                    var summaries = await LoadMonthlySummariesOrThrowAsync(dto.Month, dto.Year, innerCt);
                    if (summaries.Any(s => s.IsPayrollLocked || s.ApprovalStatus == AttendancePayrollApprovalStatus.Locked))
                        throw new ArgumentException("Kỳ công đã được khóa trước đó.");
                    if (summaries.Any(s => !s.IsPayrollLocked && s.ApprovalStatus is not AttendancePayrollApprovalStatus.Approved and not AttendancePayrollApprovalStatus.Locked))
                        throw new ArgumentException("Chỉ kỳ công đã được duyệt mới được khóa.");

                    var periodStart = new DateTime(dto.Year, dto.Month, 1);
                    var periodEnd = periodStart.AddMonths(1);
                    var payrollPeriod = BuildPayrollPeriod(dto.Month, dto.Year);
                    var now = DateTime.UtcNow;
                    var note = NormalizeNote(dto.Note);

                    foreach (var summary in summaries.Where(s => !s.IsPayrollLocked && s.ApprovalStatus == AttendancePayrollApprovalStatus.Approved))
                    {
                        summary.ApprovalStatus = AttendancePayrollApprovalStatus.Locked;
                        summary.IsPayrollLocked = true;
                        summary.LockedByAccountId = actorAccountId;
                        summary.LockedAt = now;
                        summary.PeriodNote = note ?? summary.PeriodNote;
                    }

                    var dailySummaries = await _summaryRepo.GetDailyByPeriodAsync(dto.Month, dto.Year, innerCt);
                    foreach (var daily in dailySummaries)
                    {
                        daily.ApprovalStatus = AttendancePayrollApprovalStatus.Locked;
                        daily.IsPayrollLocked = true;
                        daily.PayrollPeriod = payrollPeriod;
                    }

                    var overtimeRequests = await _overtimeRepo.GetApprovedByPeriodAsync(periodStart, periodEnd, innerCt);
                    foreach (var overtime in overtimeRequests.Where(o => !o.IsPayrollLocked))
                    {
                        overtime.IsPayrollLocked = true;
                        overtime.PayrollPeriod = payrollPeriod;
                        overtime.PayrollLockedAt = now;
                        overtime.Status = OvertimeRequestStatus.PayrollLocked;
                    }

                    var leaveRequests = await _leaveReqRepo.GetApprovedForPayrollLockByPeriodAsync(periodStart, periodEnd, innerCt);
                    foreach (var leave in leaveRequests)
                    {
                        leave.IsPayrollLocked = true;
                        leave.PayrollPeriod = payrollPeriod;
                        leave.PayrollLockedAt = now;
                    }

                    await _auditLogRepo.LogSystemEventAsync(
                        "LOCK_MONTHLY_TIMESHEET",
                        actorAccountId,
                        "attendance_summaries",
                        $"Khóa kỳ công {dto.Month:D2}/{dto.Year}. Daily={dailySummaries.Count}, OT={overtimeRequests.Count}, Leave={leaveRequests.Count}");
                    await _unitOfWork.CommitAsync(innerCt);

                    return await GetMonthlyAsync(dto.Month, dto.Year, actorRoleName, innerCt);
                },
                cancellationToken: ct);
        }

        private async Task<IEnumerable<AttendanceSummaryResponseDto>> GenerateMonthlyCoreAsync(GenerateAttendanceSummaryDto dto, int actorAccountId, string actorRoleName, CancellationToken ct)
        {
            var periodStart = new DateTime(dto.Year, dto.Month, 1);
            var periodEnd = periodStart.AddMonths(1);
            var existingSummaries = await _summaryRepo.GetByPeriodAsync(dto.Month, dto.Year, ct);
            if (existingSummaries.Any(IsClosedForRegeneration))
                throw new ArgumentException("Kỳ công đã được gửi chốt, duyệt hoặc khóa nên không thể tổng hợp lại.");

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
            EnsureHrDirectorOrAdmin(actorRoleName);
            ValidatePeriod(month, year);

            var daily = await _summaryRepo.GetDailyByPeriodAsync(month, year, ct);
            return daily.Select(MapDailyToResponse);
        }

        public async Task<IEnumerable<AttendanceAdjustmentLogResponseDto>> GetAdjustmentLogsAsync(byte month, short year, string actorRoleName, CancellationToken ct = default)
        {
            EnsureHrDirectorOrAdmin(actorRoleName);
            ValidatePeriod(month, year);

            var logs = await _summaryRepo.GetAdjustmentLogsByPeriodAsync(month, year, ct);
            return logs.Select(MapAdjustmentLogToResponse);
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

        public async Task<AttendanceDailyImportResultDto> ImportDailyAdjustmentsAsync(ImportAttendanceDailySummaryDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            EnsureHrOrAdmin(actorRoleName);
            ValidatePeriod(dto.Month, dto.Year);
            ValidateImportFile(dto.File);

            return await _lockService.GetWithLockAsync(
                LockKeys.AttendancePeriod(dto.Month, dto.Year),
                async (innerCt) =>
                {
                    var existingSummaries = await _summaryRepo.GetByPeriodAsync(dto.Month, dto.Year, innerCt);
                    if (existingSummaries.Any(IsClosedForRegeneration))
                        throw new ArgumentException("Kỳ công đã gửi chốt, duyệt hoặc khóa nên không thể import ghi đè bảng công ngày.");

                    var parsedRows = await ParseAttendanceImportRowsAsync(dto.File, innerCt);
                    var result = new AttendanceDailyImportResultDto { TotalRows = parsedRows.Count };
                    if (parsedRows.Count == 0)
                        throw new ArgumentException("File import không có dòng dữ liệu hợp lệ để xử lý.");

                    var employees = await _employeeRepo.GetActiveWithDepartmentAsync(innerCt);
                    var employeesByCode = employees
                        .Where(e => !string.IsNullOrWhiteSpace(e.EmployeeCode))
                        .GroupBy(e => e.EmployeeCode.Trim(), StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                    var periodStart = new DateTime(dto.Year, dto.Month, 1);
                    var periodEnd = periodStart.AddMonths(1);
                    var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var validRows = new List<ValidatedAttendanceImportRow>();

                    foreach (var row in parsedRows)
                    {
                        var errors = new List<string>(row.ParseErrors);
                        employeesByCode.TryGetValue(row.EmployeeCode.Trim(), out var employee);
                        if (employee == null)
                            errors.Add("Mã nhân viên không tồn tại hoặc không còn hoạt động.");

                        if (!row.WorkDate.HasValue)
                            errors.Add("Ngày công không hợp lệ.");
                        else if (row.WorkDate.Value.Date < periodStart || row.WorkDate.Value.Date >= periodEnd)
                            errors.Add($"Ngày công không thuộc kỳ {dto.Month:00}/{dto.Year}.");

                        if (!row.WorkingMinutes.HasValue)
                            errors.Add("Thiếu số phút/giờ làm việc.");
                        if (!row.WorkdayValue.HasValue)
                            errors.Add("Thiếu công quy đổi.");

                        if (employee != null && row.WorkDate.HasValue)
                        {
                            var key = $"{employee.Id}|{row.WorkDate.Value:yyyyMMdd}";
                            if (!seenKeys.Add(key))
                                errors.Add("File có nhiều dòng trùng mã nhân viên và ngày công.");
                        }

                        if (errors.Count > 0)
                        {
                            result.Errors.Add(new AttendanceDailyImportErrorDto
                            {
                                RowNumber = row.RowNumber,
                                EmployeeCode = row.EmployeeCode,
                                WorkDate = row.WorkDateText,
                                Message = string.Join(" ", errors)
                            });
                            continue;
                        }

                        validRows.Add(new ValidatedAttendanceImportRow(
                            row.RowNumber,
                            employee!,
                            row.WorkDate!.Value.Date,
                            row.WorkingMinutes!.Value,
                            row.LateMinutes ?? 0,
                            row.EarlyLeaveMinutes ?? 0,
                            row.OvertimeMinutes ?? 0,
                            row.WorkdayValue!.Value,
                            row.AttendanceStatus ?? AttendanceDailyStatus.Present,
                            row.Reason));
                    }

                    var affectedEmployeeIds = new HashSet<int>();
                    var touchedDailySummaries = new List<AttendanceDailySummary>();
                    var globalReason = NormalizeNote(dto.Reason) ?? "Import ghi đè bảng công ngày";
                    foreach (var row in validRows)
                    {
                        var daily = await _summaryRepo.GetDailyByEmployeeDateAsync(row.Employee.Id, row.WorkDate, innerCt);
                        if (daily is { IsPayrollLocked: true } || daily?.ApprovalStatus == AttendancePayrollApprovalStatus.Locked)
                        {
                            result.Errors.Add(new AttendanceDailyImportErrorDto
                            {
                                RowNumber = row.RowNumber,
                                EmployeeCode = row.Employee.EmployeeCode,
                                WorkDate = row.WorkDate.ToString("yyyy-MM-dd"),
                                Message = "Dòng bảng công ngày đã khóa, không thể ghi đè."
                            });
                            continue;
                        }

                        var isCreated = daily == null;
                        if (daily == null)
                        {
                            daily = new AttendanceDailySummary
                            {
                                EmployeeId = row.Employee.Id,
                                WorkDate = row.WorkDate
                            };
                            await _summaryRepo.AddDailyAsync(daily, innerCt);
                        }

                        var oldValue = isCreated ? "{}" : SnapshotDaily(daily);
                        daily.WorkingMinutes = row.WorkingMinutes;
                        daily.LateMinutes = row.LateMinutes;
                        daily.EarlyLeaveMinutes = row.EarlyLeaveMinutes;
                        daily.OvertimeMinutes = row.OvertimeMinutes;
                        daily.WorkdayValue = row.WorkdayValue;
                        daily.AttendanceStatus = row.AttendanceStatus;
                        daily.IsManualAdjusted = true;
                        daily.AdjustedByAccountId = actorAccountId;
                        daily.AdjustedAt = DateTime.UtcNow;
                        daily.AdjustmentReason = NormalizeNote(row.Reason) ?? globalReason;
                        daily.ApprovalStatus = AttendancePayrollApprovalStatus.Approved;
                        daily.PayrollPeriod = BuildPayrollPeriod(dto.Month, dto.Year);
                        daily.GeneratedAt = DateTime.UtcNow;

                        await _summaryRepo.AddAdjustmentLogAsync(new AttendanceAdjustmentLog
                        {
                            AttendanceDailySummaryId = daily.Id,
                            AttendanceDailySummary = daily,
                            OldValueJson = oldValue,
                            NewValueJson = SnapshotDaily(daily),
                            AdjustedByAccountId = actorAccountId,
                            Reason = daily.AdjustmentReason,
                            AdjustedAt = DateTime.UtcNow
                        }, innerCt);

                        affectedEmployeeIds.Add(row.Employee.Id);
                        touchedDailySummaries.Add(daily);
                        if (isCreated) result.CreatedRows++;
                        else result.UpdatedRows++;
                    }

                    await RecalculateMonthlySummariesFromDailyAsync(affectedEmployeeIds, dto.Month, dto.Year, touchedDailySummaries, innerCt);
                    result.ErrorRows = result.Errors.Count;

                    await _auditLogRepo.LogSystemEventAsync(
                        "IMPORT_ATTENDANCE_DAILY_SUMMARY",
                        actorAccountId,
                        "attendance_daily_summaries",
                        $"Import ghi đè bảng công ngày kỳ {dto.Month:00}/{dto.Year}. Updated={result.UpdatedRows}, Created={result.CreatedRows}, Errors={result.ErrorRows}.");
                    await _unitOfWork.CommitAsync(innerCt);

                    return result;
                },
                cancellationToken: ct);
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
            EnsureHrDirectorOrAdmin(actorRoleName);
            ValidatePeriod(month, year);

            var summaries = await _summaryRepo.GetByPeriodAsync(month, year, ct);
            return summaries.Select(MapToResponse);
        }

        private async Task<List<AttendanceSummary>> LoadMonthlySummariesOrThrowAsync(byte month, short year, CancellationToken ct)
        {
            var summaries = await _summaryRepo.GetByPeriodAsync(month, year, ct);
            if (summaries.Count == 0)
                throw new ArgumentException("Chưa có bảng công cho kỳ này. Vui lòng tổng hợp bảng công trước.");

            return summaries;
        }

        private static bool IsClosedForRegeneration(AttendanceSummary summary)
        {
            return summary.IsPayrollLocked ||
                   summary.ApprovalStatus is AttendancePayrollApprovalStatus.PendingHRReview
                       or AttendancePayrollApprovalStatus.Approved
                       or AttendancePayrollApprovalStatus.Locked;
        }

        private static string BuildPayrollPeriod(byte month, short year)
        {
            return $"{month:00}/{year}";
        }

        private static string? NormalizeNote(string? note)
        {
            var normalized = note?.Trim();
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        private async Task RecalculateMonthlySummariesFromDailyAsync(
            HashSet<int> employeeIds,
            byte month,
            short year,
            IReadOnlyCollection<AttendanceDailySummary> touchedDailySummaries,
            CancellationToken ct)
        {
            if (employeeIds.Count == 0)
                return;

            var dailyByPeriod = await _summaryRepo.GetDailyByPeriodAsync(month, year, ct);
            if (touchedDailySummaries.Count > 0)
            {
                var touchedByKey = touchedDailySummaries
                    .GroupBy(d => $"{d.EmployeeId}|{d.WorkDate:yyyyMMdd}", StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.Last(), StringComparer.OrdinalIgnoreCase);

                dailyByPeriod = dailyByPeriod
                    .Where(d => !touchedByKey.ContainsKey($"{d.EmployeeId}|{d.WorkDate:yyyyMMdd}"))
                    .Concat(touchedByKey.Values)
                    .ToList();
            }

            foreach (var employeeId in employeeIds)
            {
                var rows = dailyByPeriod.Where(d => d.EmployeeId == employeeId).ToList();
                if (rows.Count == 0)
                    continue;

                var summary = await _summaryRepo.GetByEmployeePeriodAsync(employeeId, month, year, ct);
                if (summary?.IsPayrollLocked == true || summary?.ApprovalStatus == AttendancePayrollApprovalStatus.Locked)
                    continue;

                if (summary == null)
                {
                    summary = new AttendanceSummary
                    {
                        EmployeeId = employeeId,
                        Month = month,
                        Year = year
                    };
                    await _summaryRepo.AddAsync(summary, ct);
                }

                summary.WorkedMinutes = rows.Sum(r => r.WorkingMinutes);
                summary.WorkDays = Math.Round(rows.Sum(r => r.WorkdayValue), 2, MidpointRounding.AwayFromZero);
                summary.PayableWorkHours = Math.Round(summary.WorkDays * 8m, 2, MidpointRounding.AwayFromZero);
                summary.LateMinutes = rows.Sum(r => r.LateMinutes);
                summary.EarlyLeaveMinutes = rows.Sum(r => r.EarlyLeaveMinutes);
                summary.ActualOtMinutes = rows.Sum(r => r.OvertimeMinutes);
                summary.ApprovalStatus = AttendancePayrollApprovalStatus.Draft;
                summary.GeneratedAt = DateTime.UtcNow;
            }
        }

        private static async Task<List<ParsedAttendanceImportRow>> ParseAttendanceImportRowsAsync(IFormFile file, CancellationToken ct)
        {
            var extension = Path.GetExtension(file.FileName);
            var rows = string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase)
                ? await ReadXlsxRowsAsync(file, ct)
                : await ReadDelimitedRowsAsync(file, ct);

            if (rows.Count <= 1)
                return new List<ParsedAttendanceImportRow>();

            var headers = rows[0];
            var headerIndex = headers
                .Select((header, index) => new { Key = NormalizeHeader(header), Index = index })
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Index, StringComparer.OrdinalIgnoreCase);

            var parsedRows = new List<ParsedAttendanceImportRow>();
            for (var i = 1; i < rows.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var cells = rows[i];
                if (cells.All(string.IsNullOrWhiteSpace))
                    continue;

                var errors = new List<string>();
                var workDateText = GetCell(cells, headerIndex, 1, "NgayCong", "Ngày công", "WorkDate", "Date");
                if (!TryParseDate(workDateText, out var workDate))
                    errors.Add("Ngày công không hợp lệ.");

                var workingMinutes = ParseWorkingMinutes(cells, headerIndex, errors);
                var lateMinutes = ParseOptionalInteger(cells, headerIndex, 3, errors, "Đi muộn", "DiMuon", "Đi muộn", "LateMinutes");
                var earlyLeaveMinutes = ParseOptionalInteger(cells, headerIndex, 4, errors, "Về sớm", "VeSom", "Về sớm", "EarlyLeaveMinutes");
                var overtimeMinutes = ParseOptionalInteger(cells, headerIndex, 5, errors, "OT", "OT", "LamThem", "Làm thêm", "OvertimeMinutes");
                var workdayValue = ParseWorkdayValue(cells, headerIndex, errors);
                var statusText = GetCell(cells, headerIndex, 7, "TrangThai", "Trạng thái", "AttendanceStatus", "Status");
                var status = string.IsNullOrWhiteSpace(statusText)
                    ? AttendanceDailyStatus.Present
                    : TryParseAttendanceStatus(statusText, out var parsedStatus)
                        ? parsedStatus
                        : (AttendanceDailyStatus?)null;
                if (!string.IsNullOrWhiteSpace(statusText) && status == null)
                    errors.Add("Trạng thái ngày công không hợp lệ.");

                parsedRows.Add(new ParsedAttendanceImportRow(
                    i + 1,
                    GetCell(cells, headerIndex, 0, "MaNhanVien", "Mã nhân viên", "EmployeeCode"),
                    workDateText,
                    workDate,
                    workingMinutes,
                    lateMinutes,
                    earlyLeaveMinutes,
                    overtimeMinutes,
                    workdayValue,
                    status,
                    NormalizeNote(GetCell(cells, headerIndex, 8, "LyDo", "Lý do", "GhiChu", "Ghi chú", "Reason", "Note")),
                    errors));
            }

            return parsedRows;
        }

        private static async Task<List<List<string>>> ReadDelimitedRowsAsync(IFormFile file, CancellationToken ct)
        {
            await using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var content = await reader.ReadToEndAsync(ct);
            var rawRows = content
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
            if (rawRows.Count == 0)
                return new List<List<string>>();

            var delimiter = ResolveDelimiter(rawRows[0]);
            return rawRows.Select(row => SplitDelimitedLine(row, delimiter)).ToList();
        }

        private static async Task<List<List<string>>> ReadXlsxRowsAsync(IFormFile file, CancellationToken ct)
        {
            await using var stream = file.OpenReadStream();
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            var sharedStrings = LoadSharedStrings(archive);
            var sheetEntry = archive.Entries
                .Where(e => e.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase) &&
                            e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.FullName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault()
                ?? throw new ArgumentException("Không tìm thấy sheet dữ liệu trong file Excel.");

            await using var sheetStream = sheetEntry.Open();
            var doc = await XDocument.LoadAsync(sheetStream, LoadOptions.None, ct);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var result = new List<List<string>>();

            foreach (var row in doc.Descendants(ns + "row"))
            {
                ct.ThrowIfCancellationRequested();
                var values = new SortedDictionary<int, string>();
                foreach (var cell in row.Elements(ns + "c"))
                {
                    var index = ResolveCellIndex((string?)cell.Attribute("r"));
                    values[index] = ReadXlsxCellValue(cell, sharedStrings, ns);
                }

                if (values.Count == 0)
                    continue;

                var max = values.Keys.Max();
                var cells = Enumerable.Range(0, max + 1)
                    .Select(index => values.TryGetValue(index, out var value) ? value : string.Empty)
                    .ToList();
                result.Add(cells);
            }

            return result;
        }

        private static List<string> LoadSharedStrings(ZipArchive archive)
        {
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
                return new List<string>();

            using var stream = entry.Open();
            var doc = XDocument.Load(stream);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            return doc.Descendants(ns + "si")
                .Select(si => string.Concat(si.Descendants(ns + "t").Select(t => t.Value)))
                .ToList();
        }

        private static string ReadXlsxCellValue(XElement cell, List<string> sharedStrings, XNamespace ns)
        {
            var type = (string?)cell.Attribute("t");
            if (string.Equals(type, "s", StringComparison.OrdinalIgnoreCase))
            {
                var indexText = cell.Element(ns + "v")?.Value;
                return int.TryParse(indexText, out var index) && index >= 0 && index < sharedStrings.Count
                    ? sharedStrings[index]
                    : string.Empty;
            }

            if (string.Equals(type, "inlineStr", StringComparison.OrdinalIgnoreCase))
                return string.Concat(cell.Descendants(ns + "t").Select(t => t.Value));

            return cell.Element(ns + "v")?.Value ?? string.Empty;
        }

        private static int ResolveCellIndex(string? reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
                return 0;

            var index = 0;
            foreach (var ch in reference)
            {
                if (!char.IsLetter(ch))
                    break;
                index = index * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
            }

            return Math.Max(0, index - 1);
        }

        private static void ValidateImportFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Vui lòng chọn file bảng công ngày.");
            if (file.Length > 10 * 1024 * 1024)
                throw new ArgumentException("File bảng công ngày không được vượt quá 10MB.");

            var extension = Path.GetExtension(file.FileName);
            if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Hệ thống chỉ hỗ trợ import file .xlsx hoặc .csv.");
        }

        private static char ResolveDelimiter(string headerLine)
        {
            var delimiters = new[] { ',', ';', '\t', '|' };
            return delimiters
                .Select(delimiter => new { Delimiter = delimiter, Count = headerLine.Count(ch => ch == delimiter) })
                .OrderByDescending(x => x.Count)
                .First().Delimiter;
        }

        private static List<string> SplitDelimitedLine(string line, char delimiter)
        {
            var cells = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                    continue;
                }

                if (ch == delimiter && !inQuotes)
                {
                    cells.Add(current.ToString().Trim());
                    current.Clear();
                    continue;
                }

                current.Append(ch);
            }

            cells.Add(current.ToString().Trim());
            return cells;
        }

        private static string GetCell(IReadOnlyList<string> cells, Dictionary<string, int> headerIndex, int fallbackIndex, params string[] keys)
        {
            foreach (var key in keys.Select(NormalizeHeader))
            {
                if (headerIndex.TryGetValue(key, out var index) && index >= 0 && index < cells.Count)
                    return cells[index].Trim();
            }

            return fallbackIndex >= 0 && fallbackIndex < cells.Count ? cells[fallbackIndex].Trim() : string.Empty;
        }

        private static string NormalizeHeader(string value)
        {
            return NormalizeToken(value)
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty);
        }

        private static string NormalizeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = value.Trim().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    builder.Append(char.ToLowerInvariant(ch));
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static int? ParseWorkingMinutes(IReadOnlyList<string> cells, Dictionary<string, int> headerIndex, List<string> errors)
        {
            var minutesText = GetCell(cells, headerIndex, -1, "PhutLamViec", "Phút làm việc", "WorkingMinutes", "WorkedMinutes");
            if (!string.IsNullOrWhiteSpace(minutesText))
            {
                if (TryParseInteger(minutesText, out var minutes))
                    return Math.Max(0, minutes);
                errors.Add("Phút làm việc không hợp lệ.");
                return null;
            }

            var hoursText = GetCell(cells, headerIndex, 2, "GioLam", "Giờ làm", "SoGioLam", "WorkedHours", "Hours");
            if (TryParseDecimal(hoursText, out var hours))
                return Math.Max(0, (int)Math.Round(hours * 60m, MidpointRounding.AwayFromZero));

            errors.Add("Giờ làm không hợp lệ.");
            return null;
        }

        private static int? ParseOptionalInteger(IReadOnlyList<string> cells, Dictionary<string, int> headerIndex, int fallbackIndex, List<string> errors, string label, params string[] keys)
        {
            var text = GetCell(cells, headerIndex, fallbackIndex, keys);
            if (string.IsNullOrWhiteSpace(text))
                return 0;
            if (TryParseInteger(text, out var value))
                return Math.Max(0, value);

            errors.Add($"{label} không hợp lệ.");
            return null;
        }

        private static decimal? ParseWorkdayValue(IReadOnlyList<string> cells, Dictionary<string, int> headerIndex, List<string> errors)
        {
            var text = GetCell(cells, headerIndex, 6, "Cong", "Công", "NgayCongQuyDoi", "WorkdayValue", "Workday");
            if (!TryParseDecimal(text, out var value))
            {
                errors.Add("Công quy đổi không hợp lệ.");
                return null;
            }

            return Math.Min(1m, Math.Max(0m, value));
        }

        private static bool TryParseInteger(string value, out int result)
        {
            result = 0;
            if (!TryParseDecimal(value, out var number))
                return false;
            result = (int)Math.Round(number, MidpointRounding.AwayFromZero);
            return true;
        }

        private static bool TryParseDecimal(string value, out decimal result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var normalized = value.Trim().Replace(" ", string.Empty).Replace(",", ".");
            return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
        }

        private static bool TryParseDate(string value, out DateTime? result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (DateTime.TryParse(value, new CultureInfo("vi-VN"), DateTimeStyles.None, out var parsed) ||
                DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                result = parsed.Date;
                return true;
            }

            if (double.TryParse(value.Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out var serial) &&
                serial > 20000)
            {
                result = DateTime.FromOADate(serial).Date;
                return true;
            }

            return false;
        }

        private static bool TryParseAttendanceStatus(string value, out AttendanceDailyStatus status)
        {
            var normalized = NormalizeToken(value);
            var map = new Dictionary<string, AttendanceDailyStatus>(StringComparer.OrdinalIgnoreCase)
            {
                ["present"] = AttendanceDailyStatus.Present,
                ["comat"] = AttendanceDailyStatus.Present,
                ["halfday"] = AttendanceDailyStatus.HalfDay,
                ["nuangay"] = AttendanceDailyStatus.HalfDay,
                ["paidleave"] = AttendanceDailyStatus.PaidLeave,
                ["nghihuongluong"] = AttendanceDailyStatus.PaidLeave,
                ["unpaidleave"] = AttendanceDailyStatus.UnpaidLeave,
                ["nghikhongluong"] = AttendanceDailyStatus.UnpaidLeave,
                ["absence"] = AttendanceDailyStatus.Absence,
                ["vangmat"] = AttendanceDailyStatus.Absence,
                ["holiday"] = AttendanceDailyStatus.Holiday,
                ["ngayle"] = AttendanceDailyStatus.Holiday,
                ["weekend"] = AttendanceDailyStatus.Weekend,
                ["cuoituan"] = AttendanceDailyStatus.Weekend,
                ["maternityleave"] = AttendanceDailyStatus.MaternityLeave,
                ["nghithaisan"] = AttendanceDailyStatus.MaternityLeave,
                ["sickleave"] = AttendanceDailyStatus.SickLeave,
                ["nghiom"] = AttendanceDailyStatus.SickLeave,
                ["manualadjusted"] = AttendanceDailyStatus.ManualAdjusted,
                ["dieuchinhthucong"] = AttendanceDailyStatus.ManualAdjusted
            };

            if (map.TryGetValue(NormalizeHeader(normalized), out status))
                return true;

            return Enum.TryParse(value, true, out status);
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

        private static void EnsureHrDirectorOrAdmin(string actorRoleName)
        {
            if (!string.Equals(actorRoleName, "HR", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(actorRoleName, "Director", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(actorRoleName, "Admin", StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Chỉ HR, Giám đốc hoặc Admin được xem kỳ công.");
        }

        private static void EnsureDirectorOrAdmin(string actorRoleName)
        {
            if (!string.Equals(actorRoleName, "Director", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(actorRoleName, "Admin", StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Chỉ Giám đốc hoặc Admin được duyệt kỳ công.");
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
                ApprovalStatus = summary.ApprovalStatus,
                SubmittedByAccountId = summary.SubmittedByAccountId,
                SubmittedAt = summary.SubmittedAt,
                ApprovedByAccountId = summary.ApprovedByAccountId,
                ApprovedAt = summary.ApprovedAt,
                LockedByAccountId = summary.LockedByAccountId,
                LockedAt = summary.LockedAt,
                PeriodNote = summary.PeriodNote,
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

        private static AttendanceAdjustmentLogResponseDto MapAdjustmentLogToResponse(AttendanceAdjustmentLog log)
        {
            var daily = log.AttendanceDailySummary;
            return new AttendanceAdjustmentLogResponseDto
            {
                Id = log.Id,
                AttendanceDailySummaryId = log.AttendanceDailySummaryId,
                EmployeeId = daily.EmployeeId,
                EmployeeCode = daily.Employee.EmployeeCode,
                EmployeeName = daily.Employee.FullName,
                DepartmentName = daily.Employee.Department?.DeptName,
                WorkDate = daily.WorkDate,
                AdjustedByAccountId = log.AdjustedByAccountId,
                AdjustedByName = log.AdjustedByAccount.FullName ?? log.AdjustedByAccount.Email,
                AdjustedAt = log.AdjustedAt,
                Reason = log.Reason,
                OldValueJson = log.OldValueJson,
                NewValueJson = log.NewValueJson
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

        private sealed record ParsedAttendanceImportRow(
            int RowNumber,
            string EmployeeCode,
            string WorkDateText,
            DateTime? WorkDate,
            int? WorkingMinutes,
            int? LateMinutes,
            int? EarlyLeaveMinutes,
            int? OvertimeMinutes,
            decimal? WorkdayValue,
            AttendanceDailyStatus? AttendanceStatus,
            string? Reason,
            List<string> ParseErrors);

        private sealed record ValidatedAttendanceImportRow(
            int RowNumber,
            Employee Employee,
            DateTime WorkDate,
            int WorkingMinutes,
            int LateMinutes,
            int EarlyLeaveMinutes,
            int OvertimeMinutes,
            decimal WorkdayValue,
            AttendanceDailyStatus AttendanceStatus,
            string? Reason);

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
