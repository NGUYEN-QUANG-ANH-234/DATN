using HRM.backend.src.HRM.Application.DTOs.TasksTraining;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.TasksTraining.Usecases;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;
using System.Text.Json;

namespace HRM.backend.src.HRM.Application.UseCases.TasksTraining
{
    public class PenaltyManagementUseCase : IPenaltyManagementUseCase
    {
        private readonly IPenaltyRecordRepository _penaltyRecordRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAttendanceSummaryRepository _attendanceSummaryRepo;
        private readonly IWorkCalendarConfigRepository _workCalendarConfigRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;

        public PenaltyManagementUseCase(
            IPenaltyRecordRepository penaltyRecordRepo,
            IEmployeeRepository employeeRepo,
            IAttendanceSummaryRepository attendanceSummaryRepo,
            IWorkCalendarConfigRepository workCalendarConfigRepo,
            IUnitOfWork unitOfWork,
            ILockService lockService)
        {
            _penaltyRecordRepo = penaltyRecordRepo;
            _employeeRepo = employeeRepo;
            _attendanceSummaryRepo = attendanceSummaryRepo;
            _workCalendarConfigRepo = workCalendarConfigRepo;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
        }

        public async Task<List<PenaltyRecordResponseDto>> GetRecordsAsync(string? status, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            var parsedStatus = ParseNullableStatus(status);
            var records = parsedStatus.HasValue
                ? await _penaltyRecordRepo.FindAsync(r => r.Status == parsedStatus.Value, ct)
                : await _penaltyRecordRepo.FindAsync(r =>
                    r.Status == PenaltyRecordStatus.PendingEmployeeExplanation ||
                    r.Status == PenaltyRecordStatus.PendingHRReview ||
                    r.Status == PenaltyRecordStatus.PendingDirectorApproval, ct);

            var result = new List<PenaltyRecordResponseDto>();
            foreach (var record in records.OrderByDescending(r => r.OccurredAt ?? r.CreatedAt))
            {
                if (await CanViewRecordAsync(record, actorAccountId, actorRoleName, ct))
                    result.Add(await MapAsync(record, ct));
            }

            return result;
        }

        public async Task<List<PenaltyRecordResponseDto>> GetEmployeeHistoryAsync(int employeeId, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            var targetEmployee = await GetEmployeeOrThrowAsync(employeeId, ct);
            await EnsureCanAccessEmployeeAsync(targetEmployee, actorAccountId, actorRoleName, ct);

            var records = await _penaltyRecordRepo.FindAsync(r => r.EmployeeId == employeeId, ct);
            var ordered = records
                .OrderByDescending(r => r.OccurredAt ?? r.CreatedAt)
                .ToList();

            var result = new List<PenaltyRecordResponseDto>();
            foreach (var record in ordered)
                result.Add(await MapAsync(record, ct));

            return result;
        }

        public async Task<List<PenaltyRecordResponseDto>> GetMyRecordsAsync(int actorAccountId, CancellationToken ct = default)
        {
            var employee = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct)
                ?? throw new UnauthorizedAccessException("Tài khoản chưa liên kết hồ sơ nhân sự.");

            var records = await _penaltyRecordRepo.FindAsync(r => r.EmployeeId == employee.Id, ct);
            var ordered = records
                .OrderByDescending(r => r.OccurredAt ?? r.CreatedAt)
                .ToList();

            var result = new List<PenaltyRecordResponseDto>();
            foreach (var record in ordered)
                result.Add(await MapAsync(record, ct));

            return result;
        }

        public async Task<PenaltyRecordResponseDto> GetDetailAsync(int id, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            var record = await GetRecordOrThrowAsync(id, ct);
            if (!await CanViewRecordAsync(record, actorAccountId, actorRoleName, ct))
                throw new UnauthorizedAccessException("Bạn không có quyền xem biên bản vi phạm này.");

            return await MapAsync(record, ct);
        }

        public async Task<PenaltyRecordResponseDto> CreateManualAsync(CreateManualPenaltyRecordDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            if (!IsManager(actorRoleName) && !IsHrOrAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chỉ Manager, HR hoặc Admin được lập biên bản vi phạm.");

            ValidateCreateDto(dto);
            var employee = await GetEmployeeOrThrowAsync(dto.EmployeeId, ct);
            if (IsManager(actorRoleName) && !IsHrOrAdmin(actorRoleName))
                await EnsureManagerCanAccessEmployeeAsync(employee, actorAccountId, ct);

            var violationType = ParseEnum<ViolationType>(dto.ViolationType, nameof(dto.ViolationType));
            var severity = ParseEnum<PenaltySeverity>(dto.Severity, nameof(dto.Severity));
            var occurredAt = dto.OccurredAt == default ? DateTime.UtcNow : dto.OccurredAt;

            var record = new PenaltyRecord
            {
                EmployeeId = dto.EmployeeId,
                Period = ResolvePeriod(dto.Period, occurredAt),
                SourceType = PenaltySourceType.Manual,
                RuleCode = ResolveRuleCode(dto.RuleCode, violationType),
                PenaltyPoint = Math.Max(0, dto.PenaltyPoint),
                Reason = dto.Description.Trim(),
                Status = dto.RequiresEmployeeExplanation
                    ? PenaltyRecordStatus.PendingEmployeeExplanation
                    : PenaltyRecordStatus.PendingHRReview,
                OccurredAt = occurredAt,
                ViolationType = violationType,
                Severity = severity,
                AffectsAttendance = dto.AffectsAttendance,
                AffectsPerformance = dto.AffectsPerformance,
                AffectsPersonnelDecision = dto.AffectsPersonnelDecision,
                CreatedBySystem = false,
                CreatedByAccountId = actorAccountId,
                ManagerNote = dto.ManagerNote?.Trim(),
                EvidenceFilePath = dto.EvidenceFilePath?.Trim(),
                DeductedMinutes = dto.AffectsAttendance ? NormalizeMinutes(dto.DeductedMinutes) : null,
                DeductedWorkday = dto.AffectsAttendance ? NormalizeWorkday(dto.DeductedWorkday) : null,
                CreatedAt = DateTime.UtcNow
            };

            await _penaltyRecordRepo.AddAsync(record, ct);
            await _unitOfWork.CommitAsync(ct);

            return await MapAsync(record, ct);
        }

        public async Task<PenaltyRecordResponseDto> SubmitExplanationAsync(int id, SubmitPenaltyExplanationDto dto, int actorAccountId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Explanation))
                throw new ArgumentException("Vui lòng nhập nội dung giải trình.");

            return await _lockService.GetWithLockAsync($"penalty_explanation_{id}", async innerCt =>
            {
                var record = await GetRecordOrThrowAsync(id, innerCt);
                if (record.Status != PenaltyRecordStatus.PendingEmployeeExplanation &&
                    record.Status != PenaltyRecordStatus.PendingHRReview)
                    throw new InvalidOperationException("Biên bản này không còn ở trạng thái chờ giải trình.");

                var employee = await GetEmployeeOrThrowAsync(record.EmployeeId, innerCt);
                if (employee.AccountId != actorAccountId)
                    throw new UnauthorizedAccessException("Chỉ nhân sự liên quan được gửi giải trình cho biên bản này.");

                record.EmployeeExplanation = dto.Explanation.Trim();
                if (!string.IsNullOrWhiteSpace(dto.EvidenceFilePath))
                    record.EvidenceFilePath = dto.EvidenceFilePath.Trim();

                if (record.Status == PenaltyRecordStatus.PendingEmployeeExplanation)
                    record.Status = PenaltyRecordStatus.PendingHRReview;

                _penaltyRecordRepo.Update(record);
                await _unitOfWork.CommitAsync(innerCt);
                return await MapAsync(record, innerCt);
            }, cancellationToken: ct);
        }

        public async Task<PenaltyRecordResponseDto> ReviewByHrAsync(int id, ReviewPenaltyRecordDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            if (!IsHrOrAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chỉ HR hoặc Admin được kiểm tra biên bản vi phạm.");

            return await _lockService.GetWithLockAsync($"penalty_hr_review_{id}", async innerCt =>
            {
                var record = await GetRecordOrThrowAsync(id, innerCt);
                if (record.Status != PenaltyRecordStatus.PendingHRReview)
                    throw new InvalidOperationException("Biên bản này chưa ở trạng thái chờ HR kiểm tra.");

                record.HRNote = dto.Note?.Trim();
                record.ReviewedAt = DateTime.UtcNow;

                if (!dto.IsApproved)
                {
                    record.Status = PenaltyRecordStatus.Rejected;
                    _penaltyRecordRepo.Update(record);
                    await _unitOfWork.CommitAsync(innerCt);
                    return await MapAsync(record, innerCt);
                }

                if (RequiresDirectorReview(record))
                {
                    record.Status = PenaltyRecordStatus.PendingDirectorApproval;
                }
                else
                {
                    record.ApprovedByAccountId = actorAccountId;
                    await ApplyApprovedRecordImpactAsync(record, actorAccountId, innerCt);
                }

                _penaltyRecordRepo.Update(record);
                await _unitOfWork.CommitAsync(innerCt);
                return await MapAsync(record, innerCt);
            }, cancellationToken: ct);
        }

        public async Task<PenaltyRecordResponseDto> ReviewByDirectorAsync(int id, ReviewPenaltyRecordDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            if (!IsDirectorOrAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chỉ Director hoặc Admin được duyệt biên bản vi phạm nghiêm trọng.");

            return await _lockService.GetWithLockAsync($"penalty_director_review_{id}", async innerCt =>
            {
                var record = await GetRecordOrThrowAsync(id, innerCt);
                if (record.Status != PenaltyRecordStatus.PendingDirectorApproval)
                    throw new InvalidOperationException("Biên bản này chưa ở trạng thái chờ Director duyệt.");

                record.HRNote = JoinNotes(record.HRNote, dto.Note);
                record.ReviewedAt = DateTime.UtcNow;
                if (dto.IsApproved)
                {
                    record.ApprovedByAccountId = actorAccountId;
                    await ApplyApprovedRecordImpactAsync(record, actorAccountId, innerCt);
                }
                else
                {
                    record.Status = PenaltyRecordStatus.Rejected;
                }

                _penaltyRecordRepo.Update(record);
                await _unitOfWork.CommitAsync(innerCt);
                return await MapAsync(record, innerCt);
            }, cancellationToken: ct);
        }

        private async Task ApplyApprovedRecordImpactAsync(PenaltyRecord record, int actorAccountId, CancellationToken ct)
        {
            if (!record.AffectsAttendance)
            {
                record.Status = PenaltyRecordStatus.Approved;
                return;
            }

            if (record.AttendanceAdjustmentLogId.HasValue || record.Status == PenaltyRecordStatus.Applied)
            {
                record.Status = PenaltyRecordStatus.Applied;
                return;
            }

            var daily = await TryResolveDailySummaryForPenaltyAsync(record, ct);
            if (daily == null)
            {
                record.Status = PenaltyRecordStatus.Approved;
                record.HRNote = AppendSystemNote(
                    record.HRNote,
                    "Bien ban da duoc ghi nhan co hieu luc, nhung chua ap dung dieu chinh cong vi chua co bang cong ngay tuong ung.");
                return;
            }

            if (daily.IsPayrollLocked || daily.ApprovalStatus == AttendancePayrollApprovalStatus.Locked)
                throw new InvalidOperationException("Bảng công ngày đã khóa, không thể áp dụng biên bản vi phạm.");

            var standardHours = await ResolveStandardHoursPerDayAsync(daily.EmployeeId, daily.WorkDate, ct);
            var oldValueJson = SnapshotDaily(daily);

            var deductedMinutes = ResolveDeductedMinutes(record, standardHours);
            var deductedWorkday = ResolveDeductedWorkday(record, standardHours);

            daily.WorkingMinutes = Math.Max(0, daily.WorkingMinutes - deductedMinutes);
            daily.WorkdayValue = Math.Max(0m, Math.Round(daily.WorkdayValue - deductedWorkday, 2, MidpointRounding.AwayFromZero));
            daily.IsManualAdjusted = true;
            daily.AdjustedByAccountId = actorAccountId;
            daily.AdjustedAt = DateTime.UtcNow;
            daily.AdjustmentReason = BuildAttendanceAdjustmentReason(record);
            daily.ApprovalStatus = AttendancePayrollApprovalStatus.Approved;
            daily.AttendanceStatus = ResolveAdjustedAttendanceStatus(record, daily);

            var adjustmentLog = new AttendanceAdjustmentLog
            {
                AttendanceDailySummaryId = daily.Id,
                OldValueJson = oldValueJson,
                NewValueJson = SnapshotDaily(daily),
                AdjustedByAccountId = actorAccountId,
                AdjustedAt = DateTime.UtcNow,
                Reason = daily.AdjustmentReason
            };
            await _attendanceSummaryRepo.AddAdjustmentLogAsync(adjustmentLog, ct);

            record.AttendanceAdjustmentLog = adjustmentLog;
            record.AppliedAt = DateTime.UtcNow;
            record.Status = PenaltyRecordStatus.Applied;

            await RecalculateMonthlyAttendanceSummaryAsync(daily.EmployeeId, daily.WorkDate, ct);
        }

        private async Task<AttendanceDailySummary?> TryResolveDailySummaryForPenaltyAsync(PenaltyRecord record, CancellationToken ct)
        {
            if (record.SourceType == PenaltySourceType.Attendance && record.ReferenceId.HasValue)
            {
                var referencedDaily = await _attendanceSummaryRepo.GetDailyByIdAsync(record.ReferenceId.Value, ct);
                if (referencedDaily != null)
                    return referencedDaily;
            }

            if (!record.OccurredAt.HasValue)
                return null;

            if (!record.OccurredAt.HasValue)
                throw new InvalidOperationException("Biên bản chưa có thời điểm vi phạm để xác định bảng công ngày.");

            var daily = await _attendanceSummaryRepo.GetDailyByEmployeeDateAsync(record.EmployeeId, record.OccurredAt.Value.Date, ct);
            if (daily == null)
                return null;

            if (daily == null)
                throw new InvalidOperationException("Không tìm thấy bảng công ngày tương ứng để áp dụng biên bản vi phạm.");

            return daily;
        }

        private async Task RecalculateMonthlyAttendanceSummaryAsync(int employeeId, DateTime workDate, CancellationToken ct)
        {
            var month = (byte)workDate.Month;
            var year = (short)workDate.Year;
            var summary = await _attendanceSummaryRepo.GetByEmployeePeriodAsync(employeeId, month, year, ct);
            if (summary == null)
            {
                summary = new AttendanceSummary
                {
                    EmployeeId = employeeId,
                    Month = month,
                    Year = year
                };
                await _attendanceSummaryRepo.AddAsync(summary, ct);
            }

            if (summary.IsPayrollLocked)
                throw new InvalidOperationException("Bảng công tháng đã khóa, không thể áp dụng biên bản vi phạm.");

            var dailySummaries = (await _attendanceSummaryRepo.GetDailyByPeriodAsync(month, year, ct))
                .Where(d => d.EmployeeId == employeeId)
                .ToList();
            var standardHours = await ResolveStandardHoursPerDayAsync(employeeId, workDate, ct);

            summary.WorkDays = Math.Round(dailySummaries.Sum(d => d.WorkdayValue), 2, MidpointRounding.AwayFromZero);
            summary.WorkedMinutes = dailySummaries.Sum(d => d.WorkingMinutes);
            summary.PayableWorkHours = Math.Round(summary.WorkDays * standardHours, 2, MidpointRounding.AwayFromZero);
            summary.LateMinutes = dailySummaries.Sum(d => d.LateMinutes);
            summary.EarlyLeaveMinutes = dailySummaries.Sum(d => d.EarlyLeaveMinutes);
            summary.ActualOtMinutes = dailySummaries.Sum(d => d.OvertimeMinutes);
            summary.GeneratedAt = DateTime.UtcNow;

            _attendanceSummaryRepo.Update(summary);
        }

        private async Task<decimal> ResolveStandardHoursPerDayAsync(int employeeId, DateTime workDate, CancellationToken ct)
        {
            var employee = await _employeeRepo.GetProfileByIdAsync(employeeId, ct);
            if (employee?.DeptId.HasValue == true)
            {
                var config = await _workCalendarConfigRepo.GetByDeptPeriodAsync(
                    employee.DeptId.Value,
                    (byte)workDate.Month,
                    (short)workDate.Year,
                    ct);
                if (config is { StandardHoursPerDay: > 0 })
                    return config.StandardHoursPerDay;
            }

            return 8m;
        }

        private static int ResolveDeductedMinutes(PenaltyRecord record, decimal standardHours)
        {
            var minutesFromWorkday = record.DeductedWorkday.HasValue
                ? (int)Math.Round(record.DeductedWorkday.Value * standardHours * 60m, MidpointRounding.AwayFromZero)
                : 0;
            return Math.Max(record.DeductedMinutes ?? 0, minutesFromWorkday);
        }

        private static decimal ResolveDeductedWorkday(PenaltyRecord record, decimal standardHours)
        {
            var workdayFromMinutes = record.DeductedMinutes.HasValue && standardHours > 0
                ? Math.Round(record.DeductedMinutes.Value / (standardHours * 60m), 2, MidpointRounding.AwayFromZero)
                : 0m;
            return Math.Min(1m, Math.Max(record.DeductedWorkday ?? 0m, workdayFromMinutes));
        }

        private static AttendanceDailyStatus ResolveAdjustedAttendanceStatus(PenaltyRecord record, AttendanceDailySummary daily)
        {
            if (record.ViolationType == ViolationType.UnauthorizedAbsence || daily.WorkdayValue <= 0)
                return AttendanceDailyStatus.Absence;
            if (daily.AttendanceStatus == AttendanceDailyStatus.UnpaidLeave)
                return AttendanceDailyStatus.UnpaidLeave;
            return AttendanceDailyStatus.ManualAdjusted;
        }

        private static string BuildAttendanceAdjustmentReason(PenaltyRecord record)
        {
            return $"Áp dụng biên bản vi phạm #{record.Id} ({record.RuleCode}): {record.Reason}";
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

        private static void ValidateCreateDto(CreateManualPenaltyRecordDto dto)
        {
            if (dto.EmployeeId <= 0)
                throw new ArgumentException("Vui lòng chọn nhân sự liên quan.");
            if (string.IsNullOrWhiteSpace(dto.Description))
                throw new ArgumentException("Vui lòng nhập mô tả vi phạm.");
            if (dto.PenaltyPoint < 0)
                throw new ArgumentException("Điểm trừ không được nhỏ hơn 0.");
            if (!dto.AffectsAttendance && (dto.DeductedMinutes.HasValue || dto.DeductedWorkday.HasValue))
                throw new ArgumentException("Chỉ được nhập phút/công điều chỉnh khi biên bản có ảnh hưởng bảng công.");
        }

        private async Task<PenaltyRecord> GetRecordOrThrowAsync(int id, CancellationToken ct)
        {
            return await _penaltyRecordRepo.GetByIdAsync(id, ct)
                ?? throw new InvalidOperationException("Không tìm thấy biên bản vi phạm.");
        }

        private async Task<Employee> GetEmployeeOrThrowAsync(int employeeId, CancellationToken ct)
        {
            return await _employeeRepo.GetProfileByIdAsync(employeeId, ct)
                ?? throw new InvalidOperationException("Không tìm thấy hồ sơ nhân sự.");
        }

        private async Task EnsureCanAccessEmployeeAsync(Employee targetEmployee, int actorAccountId, string actorRoleName, CancellationToken ct)
        {
            if (IsHrOrAdmin(actorRoleName) || IsDirector(actorRoleName))
                return;

            var actorEmployee = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct);
            if (actorEmployee?.Id == targetEmployee.Id)
                return;

            if (IsManager(actorRoleName))
            {
                await EnsureManagerCanAccessEmployeeAsync(targetEmployee, actorAccountId, ct);
                return;
            }

            throw new UnauthorizedAccessException("Bạn không có quyền truy cập dữ liệu vi phạm của nhân sự này.");
        }

        private async Task EnsureManagerCanAccessEmployeeAsync(Employee targetEmployee, int actorAccountId, CancellationToken ct)
        {
            var manager = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct)
                ?? throw new UnauthorizedAccessException("Tài khoản Manager chưa liên kết hồ sơ nhân sự.");

            var managedDeptIds = await _employeeRepo.GetManagedDepartmentIdsByAccountIdAsync(actorAccountId, ct);
            if (managedDeptIds.Count == 0 ||
                !targetEmployee.DeptId.HasValue ||
                !managedDeptIds.Contains(targetEmployee.DeptId.Value))
                throw new UnauthorizedAccessException("Manager chỉ được lập/xem biên bản nhân sự trong phòng ban của mình.");
        }

        private async Task<bool> CanViewRecordAsync(PenaltyRecord record, int actorAccountId, string actorRoleName, CancellationToken ct)
        {
            try
            {
                var employee = await GetEmployeeOrThrowAsync(record.EmployeeId, ct);
                await EnsureCanAccessEmployeeAsync(employee, actorAccountId, actorRoleName, ct);
                if (record.Status == PenaltyRecordStatus.PendingDirectorApproval)
                    return IsDirectorOrAdmin(actorRoleName) || IsHrOrAdmin(actorRoleName);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private async Task<PenaltyRecordResponseDto> MapAsync(PenaltyRecord record, CancellationToken ct)
        {
            var employee = await _employeeRepo.GetProfileByIdAsync(record.EmployeeId, ct);
            return new PenaltyRecordResponseDto
            {
                Id = record.Id,
                EmployeeId = record.EmployeeId,
                EmployeeCode = employee?.EmployeeCode ?? string.Empty,
                EmployeeName = employee?.FullName ?? string.Empty,
                DepartmentName = employee?.Department?.DeptName,
                Period = record.Period,
                SourceType = record.SourceType.ToString(),
                ReferenceId = record.ReferenceId,
                RuleCode = record.RuleCode,
                PenaltyPoint = record.PenaltyPoint,
                Reason = record.Reason,
                Status = record.Status.ToString(),
                OccurredAt = record.OccurredAt,
                ViolationType = record.ViolationType.ToString(),
                Severity = record.Severity.ToString(),
                AffectsAttendance = record.AffectsAttendance,
                AffectsPerformance = record.AffectsPerformance,
                AffectsPersonnelDecision = record.AffectsPersonnelDecision,
                CreatedBySystem = record.CreatedBySystem,
                CreatedByAccountId = record.CreatedByAccountId,
                EmployeeExplanation = record.EmployeeExplanation,
                ManagerNote = record.ManagerNote,
                HRNote = record.HRNote,
                EvidenceFilePath = record.EvidenceFilePath,
                ApprovedByAccountId = record.ApprovedByAccountId,
                AttendanceAdjustmentLogId = record.AttendanceAdjustmentLogId,
                DeductedMinutes = record.DeductedMinutes,
                DeductedWorkday = record.DeductedWorkday,
                PerformanceReviewId = record.PerformanceReviewId,
                ReviewedAt = record.ReviewedAt,
                AppliedAt = record.AppliedAt,
                CreatedAt = record.CreatedAt,
                RequiresDirectorReview = RequiresDirectorReview(record)
            };
        }

        private static TEnum ParseEnum<TEnum>(string value, string fieldName) where TEnum : struct, Enum
        {
            if (Enum.TryParse<TEnum>(value, true, out var parsed))
                return parsed;

            throw new ArgumentException($"{fieldName} không hợp lệ.");
        }

        private static PenaltyRecordStatus? ParseNullableStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return null;

            return ParseEnum<PenaltyRecordStatus>(status, nameof(status));
        }

        private static string ResolvePeriod(string? period, DateTime occurredAt)
        {
            return string.IsNullOrWhiteSpace(period)
                ? $"{occurredAt.Month:00}/{occurredAt.Year}"
                : period.Trim();
        }

        private static string ResolveRuleCode(string? ruleCode, ViolationType violationType)
        {
            return string.IsNullOrWhiteSpace(ruleCode)
                ? $"MANUAL_{violationType.ToString().ToUpperInvariant()}"
                : ruleCode.Trim().ToUpperInvariant();
        }

        private static int? NormalizeMinutes(int? minutes)
        {
            return minutes.HasValue ? Math.Max(0, minutes.Value) : null;
        }

        private static decimal? NormalizeWorkday(decimal? workday)
        {
            return workday.HasValue ? Math.Clamp(workday.Value, 0m, 1m) : null;
        }

        private static string? JoinNotes(string? existing, string? next)
        {
            if (string.IsNullOrWhiteSpace(next))
                return existing;
            if (string.IsNullOrWhiteSpace(existing))
                return next.Trim();
            return $"{existing.Trim()} | Director: {next.Trim()}";
        }

        private static string AppendSystemNote(string? existing, string note)
        {
            if (string.IsNullOrWhiteSpace(existing))
                return note;
            return $"{existing.Trim()} | {note}";
        }

        private static bool RequiresDirectorReview(PenaltyRecord record)
        {
            return record.AffectsPersonnelDecision ||
                   record.Severity is PenaltySeverity.High or PenaltySeverity.Critical;
        }

        private static bool IsManager(string role) =>
            role.Equals("Manager", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("Truong phong", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("Trưởng phòng", StringComparison.OrdinalIgnoreCase);

        private static bool IsHrOrAdmin(string role) =>
            role.Equals("HR", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        private static bool IsDirector(string role) =>
            role.Equals("Director", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("BOD", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("Giam doc", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("Giám đốc", StringComparison.OrdinalIgnoreCase);

        private static bool IsDirectorOrAdmin(string role) =>
            IsDirector(role) || role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }
}
