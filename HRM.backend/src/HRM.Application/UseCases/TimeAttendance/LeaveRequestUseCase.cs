using HRM.backend.src.HRM.Application.DTOs.TimeAttendance;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Usecases;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;

namespace HRM.backend.src.HRM.Application.UseCases.TimeAttendance
{
    public class LeaveRequestUseCase : ILeaveRequestUseCase
    {
        private readonly ILeaveRequestRepository _leaveReqRepo;
        private readonly ILeaveBalanceRepository _leaveBalRepo;
        private readonly ILeaveTypeRepository _leaveTypeRepo;
        private readonly IBaseRepository<MaternityLeave> _maternityLeaveRepo;
        private readonly IBaseRepository<EmploymentServicePeriod> _servicePeriodRepo;
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly ICompanyCalendarRepository _companyCalendarRepo;
        private readonly IApprovalConflictGuard _approvalConflictGuard;
        private readonly IEmailService _emailService;
        private readonly INotificationTemplateRenderer _templateRenderer;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;
        private readonly IIdempotencyService _idempotencyService;

        public LeaveRequestUseCase(
            ILeaveRequestRepository leaveReqRepo,
            ILeaveBalanceRepository leaveBalRepo,
            ILeaveTypeRepository leaveTypeRepo,
            IBaseRepository<MaternityLeave> maternityLeaveRepo,
            IBaseRepository<EmploymentServicePeriod> servicePeriodRepo,
            IAttendanceRepository attendanceRepo,
            IEmployeeRepository employeeRepo,
            ICompanyCalendarRepository companyCalendarRepo,
            IApprovalConflictGuard approvalConflictGuard,
            IEmailService emailService,
            INotificationTemplateRenderer templateRenderer,
            IUnitOfWork unitOfWork,
            ILockService lockService,
            IIdempotencyService idempotencyService)
        {
            _leaveReqRepo = leaveReqRepo;
            _leaveBalRepo = leaveBalRepo;
            _leaveTypeRepo = leaveTypeRepo;
            _maternityLeaveRepo = maternityLeaveRepo;
            _servicePeriodRepo = servicePeriodRepo;
            _attendanceRepo = attendanceRepo;
            _employeeRepo = employeeRepo;
            _companyCalendarRepo = companyCalendarRepo;
            _approvalConflictGuard = approvalConflictGuard;
            _emailService = emailService;
            _templateRenderer = templateRenderer;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
            _idempotencyService = idempotencyService;
        }

        public async Task<int> CreateAsync(CreateLeaveRequestDto dto, int actorAccountId, CancellationToken ct = default, string? idempotencyKey = null)
        {
            var existingResourceId = string.IsNullOrWhiteSpace(idempotencyKey)
                ? null
                : await _idempotencyService.FindResourceIdAsync("LEAVE_CREATE", idempotencyKey, ct);
            if (existingResourceId.HasValue)
                return existingResourceId.Value;

            ValidateDateRange(dto.StartDate, dto.EndDate);

            var employee = await GetEmployeeByAccountAsync(actorAccountId, ct);
            return await _lockService.GetWithLockAsync(
                $"leave_create_{employee.Id}_{dto.StartDate:yyyyMMdd}_{dto.EndDate:yyyyMMdd}",
                async (innerCt) =>
                {
                    var leaveType = await _leaveTypeRepo.GetByIdAsync(dto.LeaveTypeId, innerCt)
                        ?? throw new InvalidOperationException("Loại phép không tồn tại.");

                    var requestedDays = await CountBusinessDaysAsync(dto.StartDate, dto.EndDate, innerCt);
                    var finalLeaveType = leaveType;

                    if (ShouldDeductLeaveBalance(leaveType))
                    {
                        var balance = await _leaveBalRepo.GetBalanceAsync(employee.Id, leaveType.Id, (short)dto.StartDate.Year, innerCt);
                        var availableDays = (balance?.TotalDays ?? 0) - (balance?.UsedDays ?? 0);
                        if (availableDays < requestedDays)
                        {
                            finalLeaveType = (await _leaveTypeRepo.FindAsync(t => !t.IsPaid, innerCt)).FirstOrDefault()
                                ?? throw new InvalidOperationException("Quỹ phép không đủ và chưa cấu hình loại nghỉ không lương.");
                        }
                    }

                    var skipsManagerReview = await _approvalConflictGuard.RequiresDirectorApprovalAsync(employee.Id, innerCt);
                    var startDate = dto.StartDate.Date;
                    var endDate = dto.EndDate.Date;

                    await EnsureLeaveRequestDoesNotConflictAsync(employee.Id, finalLeaveType.Id, startDate, endDate, innerCt);

                    var request = new LeaveRequest
                    {
                        EmployeeId = employee.Id,
                        LeaveTypeId = finalLeaveType.Id,
                        StartDate = startDate,
                        EndDate = endDate,
                        Reason = dto.Reason.Trim(),
                        Status = skipsManagerReview ? LeaveRequestStatus.PendingHR : LeaveRequestStatus.PendingDept,
                        DeadlineAt = DateTime.UtcNow.AddHours(skipsManagerReview ? 24 : 48)
                    };

                    await _leaveReqRepo.AddRequestAsync(request);
                    await _unitOfWork.CommitAsync(innerCt);
                    if (!string.IsNullOrWhiteSpace(idempotencyKey))
                    {
                        await _idempotencyService.SaveAsync("LEAVE_CREATE", idempotencyKey, "LeaveRequest", request.Id, actorAccountId, innerCt);
                        await _unitOfWork.CommitAsync(innerCt);
                    }
                    await SendLeaveNotificationAsync("LEAVE_REQUEST_CREATED", request, employee, finalLeaveType, requestedDays, innerCt);
                    return request.Id;
                },
                cancellationToken: ct);
        }

        public async Task<IEnumerable<LeaveRequestResponseDto>> GetMyRequestsAsync(int actorAccountId, CancellationToken ct = default)
        {
            var employee = await GetEmployeeByAccountAsync(actorAccountId, ct);
            return (await _leaveReqRepo.GetByEmployeeAsync(employee.Id, ct)).Select(MapToResponse);
        }

        public async Task<IEnumerable<LeaveRequestResponseDto>> GetPendingDeptAsync(int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            if (IsAdmin(actorRoleName))
                return (await _leaveReqRepo.GetPendingDeptAsync(null, ct)).Select(MapToResponse);

            if (!IsManager(actorRoleName))
                throw new UnauthorizedAccessException("Chỉ Trưởng phòng hoặc Admin được xem đơn nghỉ chờ thẩm định.");

            var managedDeptIds = await GetManagedDepartmentIdsAsync(actorAccountId, ct);
            if (managedDeptIds.Count == 0)
                throw new UnauthorizedAccessException("Tài khoản Trưởng phòng chưa được gắn phòng ban.");

            return (await _leaveReqRepo.GetPendingDeptAsync(null, ct))
                .Where(r => r.Employee?.DeptId.HasValue == true && managedDeptIds.Contains(r.Employee.DeptId.Value))
                .Select(MapToResponse);
        }

        public async Task<IEnumerable<LeaveRequestResponseDto>> GetPendingDirectorAsync(string actorRoleName, CancellationToken ct = default)
        {
            EnsureDirectorOrAdmin(actorRoleName);
            return (await _leaveReqRepo.GetByStatusAsync(LeaveRequestStatus.PendingDirector, ct)).Select(MapToResponse);
        }

        public async Task<IEnumerable<LeaveRequestResponseDto>> GetPendingHRAsync(string actorRoleName, CancellationToken ct = default)
        {
            EnsureHrOrAdmin(actorRoleName);
            return (await _leaveReqRepo.GetByStatusAsync(LeaveRequestStatus.PendingHR, ct)).Select(MapToResponse);
        }

        public async Task<bool> ReviewByDeptAsync(int id, ReviewLeaveRequestDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            return await _lockService.GetWithLockAsync($"leave_{id}", async (innerCt) =>
            {
                var request = await GetRequestOrThrowAsync(id, innerCt);
                if (request.Status != LeaveRequestStatus.PendingDept)
                    throw new InvalidOperationException("Đơn nghỉ không ở trạng thái chờ Trưởng phòng thẩm định.");

                await EnsureManagerCanAccessAsync(request.Employee, actorAccountId, actorRoleName, innerCt);
                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(request.EmployeeId!.Value, actorAccountId, innerCt);

                request.Status = dto.IsApproved ? LeaveRequestStatus.PendingHR : LeaveRequestStatus.RejectedByDept;
                request.DeadlineAt = dto.IsApproved ? DateTime.UtcNow.AddHours(24) : null;

                await _leaveReqRepo.UpdateAsync(request, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
                return true;
            }, cancellationToken: ct);
        }

        public async Task<bool> HrConfirmAsync(int id, ReviewLeaveRequestDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            EnsureHrOrAdmin(actorRoleName);

            return await _lockService.GetWithLockAsync($"leave_{id}", async (innerCt) =>
            {
                var request = await GetRequestOrThrowAsync(id, innerCt);
                if (request.Status != LeaveRequestStatus.PendingHR)
                    throw new InvalidOperationException("Đơn nghỉ phép không ở trạng thái chờ HR ghi nhận.");

                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(request.EmployeeId!.Value, actorAccountId, innerCt);

                if (!dto.IsApproved)
                {
                    request.Status = LeaveRequestStatus.RejectedByHR;
                    request.DeadlineAt = null;
                    await _leaveReqRepo.UpdateAsync(request, innerCt);
                    await _unitOfWork.CommitAsync(innerCt);
                    var rejectedDays = await CountBusinessDaysAsync(request.StartDate!.Value, request.EndDate!.Value, innerCt);
                    await SendLeaveNotificationAsync("LEAVE_REQUEST_REJECTED", request, request.Employee, request.LeaveType, rejectedDays, innerCt, dto.Note);
                    return true;
                }

                await _lockService.GetWithLockAsync(
                    $"leave_balance_{request.EmployeeId!.Value}_{request.StartDate!.Value.Year}",
                    async (balanceCt) =>
                    {
                        await ApproveAndSyncAsync(request, LeaveRequestStatus.Approved, actorAccountId, balanceCt);
                        return true;
                    },
                    cancellationToken: innerCt);

                var approvedDays = await CountBusinessDaysAsync(request.StartDate!.Value, request.EndDate!.Value, innerCt);
                await SendLeaveNotificationAsync("LEAVE_REQUEST_APPROVED", request, request.Employee, request.LeaveType, approvedDays, innerCt);
                return true;
            }, cancellationToken: ct);
        }

        public async Task<bool> FinalApproveAsync(int id, ReviewLeaveRequestDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            EnsureDirectorOrAdmin(actorRoleName);

            return await _lockService.GetWithLockAsync($"leave_{id}", async (innerCt) =>
            {
                var request = await GetRequestOrThrowAsync(id, innerCt);
                if (request.Status != LeaveRequestStatus.PendingDirector)
                    throw new InvalidOperationException("Đơn nghỉ không ở trạng thái chờ Giám đốc phê duyệt.");

                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(request.EmployeeId!.Value, actorAccountId, innerCt);

                if (!dto.IsApproved)
                {
                    request.Status = LeaveRequestStatus.RejectedByDirector;
                    request.DeadlineAt = null;
                    await _leaveReqRepo.UpdateAsync(request, innerCt);
                    await _unitOfWork.CommitAsync(innerCt);
                    var rejectedDays = await CountBusinessDaysAsync(request.StartDate!.Value, request.EndDate!.Value, innerCt);
                    await SendLeaveNotificationAsync("LEAVE_REQUEST_REJECTED", request, request.Employee, request.LeaveType, rejectedDays, innerCt, dto.Note);
                    return true;
                }

                await _lockService.GetWithLockAsync(
                    $"leave_balance_{request.EmployeeId!.Value}_{request.StartDate!.Value.Year}",
                    async (balanceCt) =>
                    {
                        await ApproveAndSyncAsync(request, LeaveRequestStatus.Approved, actorAccountId, balanceCt);
                        return true;
                    },
                    cancellationToken: innerCt);

                var approvedDays = await CountBusinessDaysAsync(request.StartDate!.Value, request.EndDate!.Value, innerCt);
                await SendLeaveNotificationAsync("LEAVE_REQUEST_APPROVED", request, request.Employee, request.LeaveType, approvedDays, innerCt);
                return true;
            }, cancellationToken: ct);
        }

        private async Task ApproveAndSyncAsync(LeaveRequest request, LeaveRequestStatus finalStatus, int approvedByAccountId, CancellationToken ct)
        {
            if (request.EmployeeId == null || request.LeaveTypeId == null || request.StartDate == null || request.EndDate == null)
                throw new InvalidOperationException("Don nghi phep thieu du lieu bat buoc.");

            var businessDates = await EnumerateBusinessDatesAsync(request.StartDate.Value, request.EndDate.Value, ct);
            var days = businessDates.Count;
            var leaveType = request.LeaveType ?? await _leaveTypeRepo.GetByIdAsync(request.LeaveTypeId.Value, ct)
                ?? throw new InvalidOperationException("Loại phép không tồn tại.");

            request.Status = finalStatus;
            request.DeadlineAt = null;

            if (ShouldDeductLeaveBalance(leaveType))
                await _leaveBalRepo.DeductAsync(request.EmployeeId.Value, request.LeaveTypeId.Value, (short)request.StartDate.Value.Year, days, ct);

            await _attendanceRepo.SyncLeaveToAttendanceAsync(
                request.EmployeeId.Value,
                businessDates,
                AttendanceStatus.OnLeave);

            await SyncMaternityLeaveAsync(request, leaveType, approvedByAccountId, ct);

            await _leaveReqRepo.UpdateAsync(request, ct);
            await _unitOfWork.CommitAsync(ct);
        }

        private async Task SyncMaternityLeaveAsync(LeaveRequest request, LeaveType leaveType, int approvedByAccountId, CancellationToken ct)
        {
            if (leaveType.Category != LeaveCategory.Maternity)
                return;

            var employeeId = request.EmployeeId!.Value;
            var startDate = request.StartDate!.Value.Date;
            var endDate = request.EndDate!.Value.Date;

            var existing = (await _maternityLeaveRepo.FindAsync(m => m.LeaveRequestId == request.Id, ct)).FirstOrDefault();
            if (existing == null)
            {
                await _maternityLeaveRepo.AddAsync(new MaternityLeave
                {
                    EmployeeId = employeeId,
                    LeaveRequestId = request.Id,
                    StartDate = startDate,
                    EndDate = endDate,
                    ExpectedReturnDate = endDate.AddDays(1),
                    Status = MaternityLeaveStatus.Active,
                    ApprovedByAccountId = approvedByAccountId,
                    ApprovedAt = DateTime.UtcNow,
                    Note = request.Reason
                }, ct);
            }
            else
            {
                existing.StartDate = startDate;
                existing.EndDate = endDate;
                existing.ExpectedReturnDate = endDate.AddDays(1);
                existing.Status = MaternityLeaveStatus.Active;
                existing.ApprovedByAccountId = approvedByAccountId;
                existing.ApprovedAt = DateTime.UtcNow;
                existing.Note = request.Reason;
                _maternityLeaveRepo.Update(existing);
            }

            var employee = request.Employee ?? await _employeeRepo.GetByIdAsync(employeeId, ct);
            if (employee != null)
            {
                employee.Status = EmployeeStatus.OnMaternityLeave;
                _employeeRepo.Update(employee);
            }

            var existingPeriod = (await _servicePeriodRepo.FindAsync(
                p => p.SourceType == "MaternityLeave" && p.SourceId == request.Id,
                ct)).FirstOrDefault();

            if (existingPeriod == null)
            {
                await _servicePeriodRepo.AddAsync(new EmploymentServicePeriod
                {
                    EmployeeId = employeeId,
                    PeriodStart = startDate,
                    PeriodEnd = endDate,
                    PeriodType = EmploymentServicePeriodType.MaternityLeave,
                    IsActualWorkingTime = false,
                    IsSocialInsuranceContributed = false,
                    IsUnemploymentInsuranceContributed = false,
                    IsExcludedFromSeverance = false,
                    SourceType = "MaternityLeave",
                    SourceId = request.Id,
                    Note = "Ghi nhận tự động từ đơn nghỉ thai sản đã duyệt."
                }, ct);
            }
            else
            {
                existingPeriod.PeriodStart = startDate;
                existingPeriod.PeriodEnd = endDate;
                existingPeriod.PeriodType = EmploymentServicePeriodType.MaternityLeave;
                existingPeriod.Note = "Cập nhật tự động từ đơn nghỉ thai sản đã duyệt.";
                _servicePeriodRepo.Update(existingPeriod);
            }
        }

        private async Task<LeaveRequest> GetRequestOrThrowAsync(int id, CancellationToken ct)
        {
            return await _leaveReqRepo.GetDetailAsync(id, ct)
                ?? throw new InvalidOperationException("Đơn nghỉ phép không tồn tại.");
        }

        private async Task<Employee> GetEmployeeByAccountAsync(int accountId, CancellationToken ct)
        {
            return await _employeeRepo.GetByAccountIdAsync(accountId, ct)
                ?? throw new UnauthorizedAccessException("Tài khoản chưa liên kết hồ sơ nhân sự.");
        }

        private async Task EnsureLeaveRequestDoesNotConflictAsync(
            int employeeId,
            int leaveTypeId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken ct)
        {
            var exactDuplicate = (await _leaveReqRepo.FindAsync(r =>
                r.EmployeeId == employeeId &&
                r.LeaveTypeId == leaveTypeId &&
                r.StartDate == startDate &&
                r.EndDate == endDate,
                ct)).FirstOrDefault();

            if (exactDuplicate != null)
                throw new InvalidOperationException($"Đã có đơn nghỉ phép #{exactDuplicate.Id} cho loại nghỉ và khoảng thời gian này. Vui lòng kiểm tra lại danh sách đơn nghỉ phép.");

            var overlappingRequest = (await _leaveReqRepo.FindAsync(r =>
                r.EmployeeId == employeeId &&
                r.StartDate.HasValue &&
                r.EndDate.HasValue &&
                r.StartDate.Value <= endDate &&
                r.EndDate.Value >= startDate &&
                r.Status != LeaveRequestStatus.Rejected &&
                r.Status != LeaveRequestStatus.RejectedByDept &&
                r.Status != LeaveRequestStatus.RejectedByDirector &&
                r.Status != LeaveRequestStatus.RejectedByHR,
                ct)).FirstOrDefault();

            if (overlappingRequest != null)
                throw new InvalidOperationException($"Đã có đơn nghỉ phép #{overlappingRequest.Id} trùng thời gian với khoảng nghỉ này. Vui lòng kiểm tra lại trước khi gửi đơn mới.");
        }

        private async Task SendLeaveNotificationAsync(
            string templateKey,
            LeaveRequest request,
            Employee? employee,
            LeaveType? leaveType,
            decimal days,
            CancellationToken ct,
            string? rejectReason = null)
        {
            var toEmail = employee?.Account?.Email ?? employee?.PersonalEmail;
            if (string.IsNullOrWhiteSpace(toEmail))
                return;

            var (subject, body) = await _templateRenderer.RenderAsync(
                templateKey,
                new Dictionary<string, string>
                {
                    ["name"] = employee?.FullName ?? string.Empty,
                    ["leave_type"] = leaveType?.TypeName ?? string.Empty,
                    ["start_date"] = request.StartDate?.ToString("dd/MM/yyyy") ?? string.Empty,
                    ["end_date"] = request.EndDate?.ToString("dd/MM/yyyy") ?? string.Empty,
                    ["days"] = days.ToString("0.##"),
                    ["status"] = request.Status.ToString(),
                    ["reason"] = rejectReason ?? string.Empty
                },
                ct);

            await _emailService.SendEmailAsync(toEmail, subject, body);
            await _unitOfWork.CommitAsync(ct);
        }

        private async Task EnsureManagerCanAccessAsync(Employee? targetEmployee, int actorAccountId, string actorRoleName, CancellationToken ct)
        {
            if (IsAdmin(actorRoleName))
                return;

            if (!IsManager(actorRoleName))
                throw new UnauthorizedAccessException("Chỉ Trưởng phòng được thẩm định đơn nghỉ phép.");

            if (targetEmployee == null)
                throw new InvalidOperationException("Đơn nghỉ phép chưa liên kết nhân viên.");

            var managedDeptIds = await GetManagedDepartmentIdsAsync(actorAccountId, ct);
            if (!targetEmployee.DeptId.HasValue || !managedDeptIds.Contains(targetEmployee.DeptId.Value))
                throw new UnauthorizedAccessException("Trưởng phòng chỉ được thẩm định đơn nghỉ của nhân viên trong phòng ban mình.");
        }

        private async Task<HashSet<int>> GetManagedDepartmentIdsAsync(int actorAccountId, CancellationToken ct)
        {
            var deptIds = await _employeeRepo.GetManagedDepartmentIdsByAccountIdAsync(actorAccountId, ct);
            if (deptIds.Count == 0)
                throw new UnauthorizedAccessException("Tai khoan Truong phong chua duoc gan phong ban quan ly.");
            return deptIds.ToHashSet();
        }

        private static void ValidateDateRange(DateTime startDate, DateTime endDate)
        {
            if (startDate.Date < DateTime.UtcNow.Date.AddDays(-7))
                throw new InvalidOperationException("Không thể tạo đơn nghỉ cho ngày quá xa trong quá khứ.");

            if (endDate.Date < startDate.Date)
                throw new InvalidOperationException("Ngay ket thuc phai lon hon hoac bang ngay bat dau.");

            if (CountBusinessDays(startDate, endDate) <= 0)
                throw new InvalidOperationException("Khoang nghi phai co it nhat mot ngay lam viec.");
        }

        private static decimal CountBusinessDays(DateTime startDate, DateTime endDate)
        {
            return EnumerateBusinessDates(startDate, endDate).Count();
        }

        private static IEnumerable<DateTime> EnumerateBusinessDates(DateTime startDate, DateTime endDate)
        {
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                    yield return date;
            }
        }

        private async Task<decimal> CountBusinessDaysAsync(DateTime startDate, DateTime endDate, CancellationToken ct)
        {
            return (await EnumerateBusinessDatesAsync(startDate, endDate, ct)).Count;
        }

        private async Task<List<DateTime>> EnumerateBusinessDatesAsync(DateTime startDate, DateTime endDate, CancellationToken ct)
        {
            var calendars = await LoadCompanyCalendarsAsync(startDate, endDate, ct);
            var dayOffDates = calendars
                .SelectMany(calendar => calendar.Days)
                .Where(day => !day.IsWorkingDayOverride &&
                              day.DayType is CompanyCalendarDayType.PublicHoliday
                                  or CompanyCalendarDayType.CompanyHoliday
                                  or CompanyCalendarDayType.CompensatoryDayOff
                                  or CompanyCalendarDayType.SpecialPaidLeave
                                  or CompanyCalendarDayType.UnpaidCompanyClosure)
                .Select(day => day.Date.Date)
                .ToHashSet();
            var workingOverrides = calendars
                .SelectMany(calendar => calendar.Days)
                .Where(day => day.IsWorkingDayOverride ||
                              day.DayType == CompanyCalendarDayType.CompensatoryWorkingDay)
                .Select(day => day.Date.Date)
                .ToHashSet();

            var dates = new List<DateTime>();
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                if (workingOverrides.Contains(date) ||
                    (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday &&
                     !dayOffDates.Contains(date)))
                {
                    dates.Add(date);
                }
            }

            return dates;
        }

        private async Task<List<CompanyCalendar>> LoadCompanyCalendarsAsync(DateTime startDate, DateTime endDate, CancellationToken ct)
        {
            var calendars = new List<CompanyCalendar>();
            for (var year = startDate.Year; year <= endDate.Year; year++)
            {
                var calendar = await _companyCalendarRepo.GetActiveByYearAsync((short)year, ct);
                if (calendar != null)
                    calendars.Add(calendar);
            }

            return calendars;
        }

        private static void EnsureDirectorOrAdmin(string actorRoleName)
        {
            if (!IsDirector(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chỉ Giám đốc hoặc Admin được phê duyệt cuối cùng đơn nghỉ phép.");
        }

        private static void EnsureHrOrAdmin(string actorRoleName)
        {
            if (!IsHr(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chỉ HR hoặc Admin được ghi nhận đơn nghỉ phép.");
        }

        private static LeaveRequestResponseDto MapToResponse(LeaveRequest request)
        {
            return new LeaveRequestResponseDto
            {
                Id = request.Id,
                EmployeeId = request.EmployeeId ?? 0,
                EmployeeCode = request.Employee?.EmployeeCode ?? string.Empty,
                EmployeeName = request.Employee?.FullName ?? "Không xác định",
                DepartmentName = request.Employee?.Department?.DeptName,
                LeaveTypeId = request.LeaveTypeId ?? 0,
                LeaveTypeName = request.LeaveType?.TypeName ?? "Không xác định",
                IsPaidLeave = request.LeaveType?.IsPaid ?? false,
                LeaveCategory = request.LeaveType?.Category.ToString() ?? string.Empty,
                StartDate = request.StartDate ?? DateTime.MinValue,
                EndDate = request.EndDate ?? DateTime.MinValue,
                RequestedDays = request.StartDate.HasValue && request.EndDate.HasValue
                    ? CountBusinessDays(request.StartDate.Value, request.EndDate.Value)
                    : 0,
                Reason = request.Reason ?? string.Empty,
                Status = request.Status.ToString(),
                DeadlineAt = request.DeadlineAt
            };
        }

        private static bool IsAdmin(string role) => string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        private static bool IsHr(string role) => string.Equals(role, "HR", StringComparison.OrdinalIgnoreCase);
        private static bool IsManager(string role) => string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase);
        private static bool IsDirector(string role) => string.Equals(role, "Director", StringComparison.OrdinalIgnoreCase);
        private static bool ShouldDeductLeaveBalance(LeaveType leaveType) =>
            leaveType.IsPaid && leaveType.DeductAnnualLeave;
    }
}
