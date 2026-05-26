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
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IEmployeeRepository _employeeRepo;
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
            IAttendanceRepository attendanceRepo,
            IEmployeeRepository employeeRepo,
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
            _attendanceRepo = attendanceRepo;
            _employeeRepo = employeeRepo;
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
                        ?? throw new InvalidOperationException("Loai phep khong ton tai.");

                    var requestedDays = CountBusinessDays(dto.StartDate, dto.EndDate);
                    var finalLeaveType = leaveType;

                    if (leaveType.IsPaid)
                    {
                        var balance = await _leaveBalRepo.GetBalanceAsync(employee.Id, leaveType.Id, (short)dto.StartDate.Year, innerCt);
                        var availableDays = (balance?.TotalDays ?? 0) - (balance?.UsedDays ?? 0);
                        if (availableDays < requestedDays)
                        {
                            finalLeaveType = (await _leaveTypeRepo.FindAsync(t => !t.IsPaid, innerCt)).FirstOrDefault()
                                ?? throw new InvalidOperationException("Quy phep khong du va chua cau hinh loai nghi khong luong.");
                        }
                    }

                    var requiresDirectorApproval = await _approvalConflictGuard.RequiresDirectorApprovalAsync(employee.Id, innerCt);

                    var request = new LeaveRequest
                    {
                        EmployeeId = employee.Id,
                        LeaveTypeId = finalLeaveType.Id,
                        StartDate = dto.StartDate.Date,
                        EndDate = dto.EndDate.Date,
                        Reason = dto.Reason.Trim(),
                        Status = requiresDirectorApproval ? LeaveRequestStatus.PendingDirector : LeaveRequestStatus.PendingDept,
                        DeadlineAt = DateTime.UtcNow.AddHours(requiresDirectorApproval ? 24 : 48)
                    };

                    await _leaveReqRepo.AddRequestAsync(request);
                    await _unitOfWork.CommitAsync(innerCt);
                    await _idempotencyService.SaveAsync("LEAVE_CREATE", idempotencyKey ?? string.Empty, "LeaveRequest", request.Id, actorAccountId, innerCt);
                    await _unitOfWork.CommitAsync(innerCt);
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
                throw new UnauthorizedAccessException("Chi Truong phong hoac Admin duoc xem don nghi cho tham dinh.");

            var manager = await GetEmployeeByAccountAsync(actorAccountId, ct);
            if (!manager.DeptId.HasValue)
                throw new UnauthorizedAccessException("Tai khoan Truong phong chua duoc gan phong ban.");

            return (await _leaveReqRepo.GetPendingDeptAsync(manager.DeptId.Value, ct)).Select(MapToResponse);
        }

        public async Task<IEnumerable<LeaveRequestResponseDto>> GetPendingDirectorAsync(string actorRoleName, CancellationToken ct = default)
        {
            EnsureDirectorOrAdmin(actorRoleName);
            return (await _leaveReqRepo.GetByStatusAsync(LeaveRequestStatus.PendingDirector, ct)).Select(MapToResponse);
        }

        public async Task<bool> ReviewByDeptAsync(int id, ReviewLeaveRequestDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            return await _lockService.GetWithLockAsync($"leave_{id}", async (innerCt) =>
            {
                var request = await GetRequestOrThrowAsync(id, innerCt);
                if (request.Status != LeaveRequestStatus.PendingDept)
                    throw new InvalidOperationException("Don nghi khong o trang thai cho Truong phong tham dinh.");

                await EnsureManagerCanAccessAsync(request.Employee, actorAccountId, actorRoleName, innerCt);
                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(request.EmployeeId!.Value, actorAccountId, innerCt);

                request.Status = dto.IsApproved ? LeaveRequestStatus.PendingDirector : LeaveRequestStatus.RejectedByDept;
                request.DeadlineAt = dto.IsApproved ? DateTime.UtcNow.AddHours(24) : null;

                await _leaveReqRepo.UpdateAsync(request, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
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
                    throw new InvalidOperationException("Don nghi khong o trang thai cho Giam doc phe duyet.");

                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(request.EmployeeId!.Value, actorAccountId, innerCt);

                if (!dto.IsApproved)
                {
                    request.Status = LeaveRequestStatus.RejectedByDirector;
                    request.DeadlineAt = null;
                    await _leaveReqRepo.UpdateAsync(request, innerCt);
                    await _unitOfWork.CommitAsync(innerCt);
                    await SendLeaveNotificationAsync("LEAVE_REQUEST_REJECTED", request, request.Employee, request.LeaveType, CountBusinessDays(request.StartDate!.Value, request.EndDate!.Value), innerCt, dto.Note);
                    return true;
                }

                await _lockService.GetWithLockAsync(
                    $"leave_balance_{request.EmployeeId!.Value}_{request.StartDate!.Value.Year}",
                    async (balanceCt) =>
                    {
                        await ApproveAndSyncAsync(request, LeaveRequestStatus.Approved, balanceCt);
                        return true;
                    },
                    cancellationToken: innerCt);

                await SendLeaveNotificationAsync("LEAVE_REQUEST_APPROVED", request, request.Employee, request.LeaveType, CountBusinessDays(request.StartDate!.Value, request.EndDate!.Value), innerCt);
                return true;
            }, cancellationToken: ct);
        }

        private async Task ApproveAndSyncAsync(LeaveRequest request, LeaveRequestStatus finalStatus, CancellationToken ct)
        {
            if (request.EmployeeId == null || request.LeaveTypeId == null || request.StartDate == null || request.EndDate == null)
                throw new InvalidOperationException("Don nghi phep thieu du lieu bat buoc.");

            var days = CountBusinessDays(request.StartDate.Value, request.EndDate.Value);
            var leaveType = request.LeaveType ?? await _leaveTypeRepo.GetByIdAsync(request.LeaveTypeId.Value, ct)
                ?? throw new InvalidOperationException("Loai phep khong ton tai.");

            request.Status = finalStatus;
            request.DeadlineAt = null;

            if (leaveType.IsPaid)
                await _leaveBalRepo.DeductAsync(request.EmployeeId.Value, request.LeaveTypeId.Value, (short)request.StartDate.Value.Year, days, ct);

            await _attendanceRepo.SyncLeaveToAttendanceAsync(
                request.EmployeeId.Value,
                EnumerateBusinessDates(request.StartDate.Value, request.EndDate.Value).ToList(),
                AttendanceStatus.OnLeave);

            await _leaveReqRepo.UpdateAsync(request, ct);
            await _unitOfWork.CommitAsync(ct);
        }

        private async Task<LeaveRequest> GetRequestOrThrowAsync(int id, CancellationToken ct)
        {
            return await _leaveReqRepo.GetDetailAsync(id, ct)
                ?? throw new InvalidOperationException("Don nghi phep khong ton tai.");
        }

        private async Task<Employee> GetEmployeeByAccountAsync(int accountId, CancellationToken ct)
        {
            return await _employeeRepo.GetByAccountIdAsync(accountId, ct)
                ?? throw new UnauthorizedAccessException("Tai khoan chua lien ket ho so nhan su.");
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
                throw new UnauthorizedAccessException("Chi Truong phong duoc tham dinh don nghi phep.");

            if (targetEmployee == null)
                throw new InvalidOperationException("Don nghi phep chua lien ket nhan vien.");

            var manager = await GetEmployeeByAccountAsync(actorAccountId, ct);
            if (!manager.DeptId.HasValue || targetEmployee.DeptId != manager.DeptId)
                throw new UnauthorizedAccessException("Truong phong chi duoc tham dinh don nghi cua nhan vien trong phong ban minh.");
        }

        private static void ValidateDateRange(DateTime startDate, DateTime endDate)
        {
            if (startDate.Date < DateTime.UtcNow.Date.AddDays(-7))
                throw new InvalidOperationException("Khong the tao don nghi cho ngay qua xa trong qua khu.");

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

        private static void EnsureDirectorOrAdmin(string actorRoleName)
        {
            if (!IsDirector(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chi Giam doc hoac Admin duoc phe duyet cuoi cung don nghi phep.");
        }

        private static LeaveRequestResponseDto MapToResponse(LeaveRequest request)
        {
            return new LeaveRequestResponseDto
            {
                Id = request.Id,
                EmployeeId = request.EmployeeId ?? 0,
                EmployeeCode = request.Employee?.EmployeeCode ?? string.Empty,
                EmployeeName = request.Employee?.FullName ?? "Khong xac dinh",
                DepartmentName = request.Employee?.Department?.DeptName,
                LeaveTypeId = request.LeaveTypeId ?? 0,
                LeaveTypeName = request.LeaveType?.TypeName ?? "Khong xac dinh",
                IsPaidLeave = request.LeaveType?.IsPaid ?? false,
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
        private static bool IsManager(string role) => string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase);
        private static bool IsDirector(string role) => string.Equals(role, "Director", StringComparison.OrdinalIgnoreCase);
    }
}
