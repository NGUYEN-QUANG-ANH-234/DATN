using HRM.backend.src.HRM.Application.DTOs.TimeAttendance;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Services;
using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Usecases;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;

namespace HRM.backend.src.HRM.Application.UseCases.TimeAttendance
{
    public class OvertimeRequestUseCase : IOvertimeRequestUseCase
    {
        private readonly IOvertimeRequestRepository _overtimeRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IApprovalConflictGuard _approvalConflictGuard;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;
        private readonly IAppCache _cache;
        private readonly IIdempotencyService _idempotencyService;
        private readonly IOvertimeReconciliationService _reconciliationService;

        public OvertimeRequestUseCase(
            IOvertimeRequestRepository overtimeRepo,
            IEmployeeRepository employeeRepo,
            IAttendanceRepository attendanceRepo,
            IAuditLogRepository auditLogRepo,
            IApprovalConflictGuard approvalConflictGuard,
            IUnitOfWork unitOfWork,
            ILockService lockService,
            IAppCache cache,
            IIdempotencyService idempotencyService,
            IOvertimeReconciliationService reconciliationService)
        {
            _overtimeRepo = overtimeRepo;
            _employeeRepo = employeeRepo;
            _attendanceRepo = attendanceRepo;
            _auditLogRepo = auditLogRepo;
            _approvalConflictGuard = approvalConflictGuard;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
            _cache = cache;
            _idempotencyService = idempotencyService;
            _reconciliationService = reconciliationService;
        }

        public async Task<int> CreateAsync(CreateOvertimeRequestDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default, string? idempotencyKey = null)
        {
            var existingResourceId = string.IsNullOrWhiteSpace(idempotencyKey)
                ? null
                : await _idempotencyService.FindResourceIdAsync("OVERTIME_CREATE", idempotencyKey, ct);
            if (existingResourceId.HasValue)
                return existingResourceId.Value;

            var range = ResolveTimeRange(dto.WorkDate, dto.StartTime, dto.EndTime);

            var actorEmployee = IsAdmin(actorRoleName)
                ? await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct)
                : await GetEmployeeByAccountAsync(actorAccountId, ct);
            var targetEmployee = await ResolveTargetEmployeeAsync(dto.EmployeeId, actorEmployee, actorAccountId, actorRoleName, ct);
            var isManagerCreatedForOther = (IsManager(actorRoleName) || IsAdmin(actorRoleName)) && targetEmployee.AccountId != actorAccountId;
            var requiresDirectorApproval = await _approvalConflictGuard.RequiresDirectorApprovalAsync(targetEmployee.Id, ct);

            return await _lockService.GetWithLockAsync(
                $"overtime_create_{targetEmployee.Id}_{dto.WorkDate:yyyyMMdd}",
                async (innerCt) =>
                {
                    await EnsureNoOverlapAsync(targetEmployee.Id, range.StartAt, range.EndAt, innerCt);

                    var request = new OvertimeRequest
                    {
                        EmployeeId = targetEmployee.Id,
                        RequestedByAccountId = actorAccountId,
                        WorkDate = dto.WorkDate.Date,
                        StartTime = dto.StartTime,
                        EndTime = dto.EndTime,
                        StartAt = range.StartAt,
                        EndAt = range.EndAt,
                        Reason = dto.Reason.Trim(),
                        ProjectCode = string.IsNullOrWhiteSpace(dto.ProjectCode) ? null : dto.ProjectCode.Trim(),
                        Status = requiresDirectorApproval
                            ? OvertimeRequestStatus.PendingDirector
                            : isManagerCreatedForOther ? OvertimeRequestStatus.PendingHR : OvertimeRequestStatus.PendingManager,
                        ManagerReviewerAccountId = isManagerCreatedForOther && !requiresDirectorApproval ? actorAccountId : null,
                        ManagerReviewedAt = isManagerCreatedForOther && !requiresDirectorApproval ? DateTime.UtcNow : null,
                        ManagerNote = isManagerCreatedForOther && !requiresDirectorApproval
                            ? IsAdmin(actorRoleName)
                                ? "Admin tạo yêu cầu OT cho nhân viên."
                                : "Manager tạo yêu cầu OT cho nhân viên trong phòng."
                            : null,
                        ApprovedMinutes = CalculateApprovedMinutes(range.StartAt, range.EndAt)
                    };

                    await _overtimeRepo.AddAsync(request, innerCt);
                    await _unitOfWork.CommitAsync(innerCt);
                    await _idempotencyService.SaveAsync("OVERTIME_CREATE", idempotencyKey ?? string.Empty, "OvertimeRequest", request.Id, actorAccountId, innerCt);
                    await _unitOfWork.CommitAsync(innerCt);
                    return request.Id;
                },
                cancellationToken: ct);
        }

        public async Task<IReadOnlyList<int>> CreateBulkByManagerAsync(CreateBulkOvertimeRequestDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default, string? idempotencyKey = null)
        {
            if (!IsManager(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chỉ Manager hoặc Admin được tạo OT hàng loạt cho nhân viên.");

            var range = ResolveTimeRange(dto.WorkDate, dto.StartTime, dto.EndTime);

            var employeeIds = dto.EmployeeIds.Distinct().ToList();
            if (employeeIds.Count == 0)
                throw new InvalidOperationException("Danh sách nhân viên OT không được để trống.");

            var actorEmployee = IsAdmin(actorRoleName)
                ? await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct)
                : await GetEmployeeByAccountAsync(actorAccountId, ct);
            var managedDeptIds = IsManager(actorRoleName) && !IsAdmin(actorRoleName)
                ? await GetManagedDepartmentIdsAsync(actorAccountId, ct)
                : new HashSet<int>();
            if (IsManager(actorRoleName) && !IsAdmin(actorRoleName) && false)
                throw new UnauthorizedAccessException("Tài khoản Manager chưa được gắn phòng ban.");

            var employees = (await _employeeRepo.FindAsync(e => employeeIds.Contains(e.Id), ct)).ToList();
            var missingIds = employeeIds.Except(employees.Select(e => e.Id)).ToList();
            if (missingIds.Count > 0)
                throw new InvalidOperationException($"Không tìm thấy nhân viên: {string.Join(", ", missingIds)}.");

            if (IsManager(actorRoleName) && !IsAdmin(actorRoleName))
            {
                var outsideDept = employees
                    .Where(e => !e.DeptId.HasValue || !managedDeptIds.Contains(e.DeptId.Value))
                    .Select(e => e.Id)
                    .ToList();
                if (outsideDept.Count > 0)
                    throw new UnauthorizedAccessException($"Manager chỉ được tạo OT cho nhân viên thuộc phòng ban của mình. Ngoài phạm vi: {string.Join(", ", outsideDept)}.");
            }

            var lockKey = $"overtime_create_bulk_{dto.WorkDate:yyyyMMdd}_{string.Join("_", employeeIds.OrderBy(id => id))}";
            return await _lockService.GetWithLockAsync(
                lockKey,
                async (innerCt) =>
                {
                    foreach (var employee in employees)
                    {
                        await EnsureNoOverlapAsync(employee.Id, range.StartAt, range.EndAt, innerCt);
                    }

                    var directorApprovalEmployeeIds = new HashSet<int>();
                    foreach (var employee in employees)
                    {
                        if (await _approvalConflictGuard.RequiresDirectorApprovalAsync(employee.Id, innerCt))
                            directorApprovalEmployeeIds.Add(employee.Id);
                    }

                    var now = DateTime.UtcNow;
                    var requests = employees.Select(employee => new OvertimeRequest
                    {
                        EmployeeId = employee.Id,
                        RequestedByAccountId = actorAccountId,
                        WorkDate = dto.WorkDate.Date,
                        StartTime = dto.StartTime,
                        EndTime = dto.EndTime,
                        StartAt = range.StartAt,
                        EndAt = range.EndAt,
                        Reason = dto.Reason.Trim(),
                        ProjectCode = string.IsNullOrWhiteSpace(dto.ProjectCode) ? null : dto.ProjectCode.Trim(),
                        Status = directorApprovalEmployeeIds.Contains(employee.Id) ? OvertimeRequestStatus.PendingDirector : OvertimeRequestStatus.PendingHR,
                        ManagerReviewerAccountId = directorApprovalEmployeeIds.Contains(employee.Id) ? null : actorAccountId,
                        ManagerReviewedAt = directorApprovalEmployeeIds.Contains(employee.Id) ? null : now,
                        ManagerNote = directorApprovalEmployeeIds.Contains(employee.Id)
                            ? null
                            : IsAdmin(actorRoleName)
                                ? "Admin tạo yêu cầu OT hàng loạt cho nhân viên."
                                : "Manager tạo yêu cầu OT hàng loạt cho nhân viên trong phòng.",
                        ApprovedMinutes = CalculateApprovedMinutes(range.StartAt, range.EndAt)
                    }).ToList();

                    await _overtimeRepo.AddRangeAsync(requests, innerCt);
                    var actorLabel = IsAdmin(actorRoleName) ? "Admin" : "Manager";
                    await _auditLogRepo.LogSystemEventAsync("OT_MANAGER_CREATE_BULK", actorAccountId, "overtime_requests", $"{actorLabel} tạo {requests.Count} yêu cầu OT hàng loạt.");
                    await _unitOfWork.CommitAsync(innerCt);

                    return requests.Select(r => r.Id).ToList();
                },
                cancellationToken: ct);
        }

        public async Task<IEnumerable<OvertimeEmployeeOptionDto>> GetAssignableEmployeesAsync(int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            if (!IsManager(actorRoleName) && !IsHr(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Bạn không có quyền chọn nhân viên để tạo OT.");

            if (IsManager(actorRoleName) && !IsAdmin(actorRoleName))
            {
                var manager = await GetEmployeeByAccountAsync(actorAccountId, ct);
                var managedDeptIds = await GetManagedDepartmentIdsAsync(actorAccountId, ct);
                if (managedDeptIds.Count == 0)
                    throw new UnauthorizedAccessException("Tài khoản Manager chưa được gắn phòng ban.");

                return await _cache.GetOrSetWithLockAsync(
                    $"employee_options_managed_depts_{actorAccountId}_{string.Join("_", managedDeptIds.OrderBy(id => id))}",
                    async (innerCt) =>
                    {
                        var employees = (await _employeeRepo.GetActiveWithDepartmentAsync(innerCt))
                            .Where(e => e.DeptId.HasValue &&
                                        managedDeptIds.Contains(e.DeptId.Value) &&
                                        e.Id != manager.Id)
                            .ToList();
                        return MapEmployeeOptions(employees);
                    },
                    TimeSpan.FromMinutes(10),
                    _lockService,
                    ct: ct);
            }

            return await _cache.GetOrSetWithLockAsync(
                "employee_options_all",
                async (innerCt) =>
                {
                    var employees = await _employeeRepo.GetActiveWithDepartmentAsync(innerCt);
                    return MapEmployeeOptions(employees);
                },
                TimeSpan.FromMinutes(10),
                _lockService,
                ct: ct);
        }

        public async Task<IEnumerable<OvertimeRequestResponseDto>> GetMyRequestsAsync(int actorAccountId, CancellationToken ct = default)
        {
            var employee = await GetEmployeeByAccountAsync(actorAccountId, ct);
            return (await _overtimeRepo.GetByEmployeeAsync(employee.Id, ct)).Select(MapToResponse);
        }

        public async Task<IEnumerable<OvertimeRequestResponseDto>> GetPendingManagerAsync(int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            if (!IsManager(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chỉ Manager hoặc Admin được xem danh sách OT cho Trưởng phòng duyệt.");

            if (IsAdmin(actorRoleName))
                return (await _overtimeRepo.GetByStatusAsync(OvertimeRequestStatus.PendingManager, ct)).Select(MapToResponse);

            var managedDeptIds = await GetManagedDepartmentIdsAsync(actorAccountId, ct);
            if (managedDeptIds.Count == 0)
                throw new UnauthorizedAccessException("Tài khoản Manager chưa được gắn phòng ban.");

            return (await _overtimeRepo.GetByStatusAsync(OvertimeRequestStatus.PendingManager, ct))
                .Where(r => r.Employee.DeptId.HasValue && managedDeptIds.Contains(r.Employee.DeptId.Value))
                .Select(MapToResponse);
        }

        public async Task<IEnumerable<OvertimeRequestResponseDto>> GetPendingHrAsync(string actorRoleName, CancellationToken ct = default)
        {
            if (!IsHr(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chi HR hoac Admin duoc xem danh sach OT cho HR xac nhan.");

            return (await _overtimeRepo.GetByStatusAsync(OvertimeRequestStatus.PendingHR, ct)).Select(MapToResponse);
        }

        public async Task<IEnumerable<OvertimeRequestResponseDto>> GetPendingDirectorAsync(string actorRoleName, CancellationToken ct = default)
        {
            if (!IsDirector(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chỉ Giám đốc hoặc Admin được xem danh sách OT cần duyệt trực tiếp.");

            return (await _overtimeRepo.GetByStatusAsync(OvertimeRequestStatus.PendingDirector, ct)).Select(MapToResponse);
        }

        public async Task<IEnumerable<OvertimeRequestResponseDto>> GetApprovedForHrAsync(string actorRoleName, int? month = null, int? year = null, CancellationToken ct = default)
        {
            if (!IsHr(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chi HR hoac Admin duoc xem danh sach OT da duyet.");

            DateTime? fromDate = null;
            DateTime? toDate = null;
            if (month.HasValue && year.HasValue)
            {
                fromDate = new DateTime(year.Value, month.Value, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            return (await _overtimeRepo.GetApprovedAsync(fromDate, toDate, ct)).Select(MapToResponse);
        }

        public async Task<bool> ReviewByManagerAsync(int id, ReviewOvertimeRequestDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            return await _lockService.GetWithLockAsync($"overtime_{id}", async (innerCt) =>
            {
                var request = await GetRequestOrThrowAsync(id, innerCt);
                if (request.Status != OvertimeRequestStatus.PendingManager)
                    throw new InvalidOperationException("Yêu cầu OT không ở trạng thái chờ Manager duyệt.");

                await EnsureManagerCanAccessAsync(request.Employee, actorAccountId, actorRoleName, innerCt);
                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(request.EmployeeId, actorAccountId, innerCt);

                request.ManagerReviewerAccountId = actorAccountId;
                request.ManagerReviewedAt = DateTime.UtcNow;
                request.ManagerNote = dto.Note;
                request.Status = dto.IsApproved ? OvertimeRequestStatus.PendingHR : OvertimeRequestStatus.Rejected;

                await _overtimeRepo.UpdateAsync(request, innerCt);
                await _auditLogRepo.LogSystemEventAsync("OT_MANAGER_REVIEW", actorAccountId, "overtime_requests", $"Manager {(dto.IsApproved ? "approved" : "rejected")} OT #{request.Id}");
                await _unitOfWork.CommitAsync(innerCt);
                return true;
            }, cancellationToken: ct);
        }

        public async Task<bool> ConfirmByHrAsync(int id, ReviewOvertimeRequestDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            if (!IsHr(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chi HR hoac Admin duoc xac nhan OT.");

            return await _lockService.GetWithLockAsync($"overtime_{id}", async (innerCt) =>
            {
                var request = await GetRequestOrThrowAsync(id, innerCt);
                if (request.Status != OvertimeRequestStatus.PendingHR)
                    throw new InvalidOperationException("Yêu cầu OT không ở trạng thái chờ HR xác nhận.");

                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(request.EmployeeId, actorAccountId, innerCt);

                request.HrReviewerAccountId = actorAccountId;
                request.HrReviewedAt = DateTime.UtcNow;
                request.HrNote = dto.Note;
                request.Status = dto.IsApproved ? OvertimeRequestStatus.Approved : OvertimeRequestStatus.Rejected;

                if (dto.IsApproved)
                    await _reconciliationService.ReconcileAsync(request, await _attendanceRepo.GetTodayLogAsync(request.EmployeeId, request.WorkDate, innerCt), innerCt);

                await _overtimeRepo.UpdateAsync(request, innerCt);
                await _auditLogRepo.LogSystemEventAsync("OT_HR_CONFIRM", actorAccountId, "overtime_requests", $"HR {(dto.IsApproved ? "confirmed" : "rejected")} OT #{request.Id}");
                await _unitOfWork.CommitAsync(innerCt);
                return true;
            }, cancellationToken: ct);
        }

        public async Task<bool> ReviewByDirectorAsync(int id, ReviewOvertimeRequestDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            if (!IsDirector(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chỉ Giám đốc hoặc Admin được duyệt OT trực tiếp.");

            return await _lockService.GetWithLockAsync($"overtime_{id}", async (innerCt) =>
            {
                var request = await GetRequestOrThrowAsync(id, innerCt);
                if (request.Status != OvertimeRequestStatus.PendingDirector)
                    throw new InvalidOperationException("Yêu cầu OT không ở trạng thái chờ Giám đốc duyệt.");

                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(request.EmployeeId, actorAccountId, innerCt);

                request.DirectorReviewerAccountId = actorAccountId;
                request.DirectorReviewedAt = DateTime.UtcNow;
                request.DirectorNote = dto.Note;
                request.Status = dto.IsApproved ? OvertimeRequestStatus.Approved : OvertimeRequestStatus.Rejected;

                if (dto.IsApproved)
                    await _reconciliationService.ReconcileAsync(request, await _attendanceRepo.GetTodayLogAsync(request.EmployeeId, request.WorkDate, innerCt), innerCt);

                await _overtimeRepo.UpdateAsync(request, innerCt);
                await _auditLogRepo.LogSystemEventAsync("OT_DIRECTOR_REVIEW", actorAccountId, "overtime_requests", $"Director {(dto.IsApproved ? "approved" : "rejected")} OT #{request.Id}");
                await _unitOfWork.CommitAsync(innerCt);
                return true;
            }, cancellationToken: ct);
        }

        public async Task<OvertimeRequestResponseDto> ReconcileAsync(int id, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            if (!IsHr(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chi HR hoac Admin duoc doi chieu OT.");

            return await _lockService.GetWithLockAsync($"overtime_{id}", async (innerCt) =>
            {
                var request = await GetRequestOrThrowAsync(id, innerCt);
                if (request.Status != OvertimeRequestStatus.Approved &&
                    request.Status != OvertimeRequestStatus.Reconciled)
                    throw new InvalidOperationException("Chỉ yêu cầu OT đã được duyệt mới được đối chiếu.");

                if (request.IsPayrollLocked)
                    throw new InvalidOperationException("Yêu cầu OT đã được khóa trong kỳ lương, không thể đối chiếu lại.");

                var attendanceLog = await _attendanceRepo.GetTodayLogAsync(request.EmployeeId, request.WorkDate, innerCt);
                await _reconciliationService.ReconcileAsync(request, attendanceLog, innerCt);

                await _overtimeRepo.UpdateAsync(request, innerCt);
                await _auditLogRepo.LogSystemEventAsync("OT_RECONCILE", actorAccountId, "overtime_requests", $"Doi chieu OT #{request.Id}: {request.ActualOtMinutes} phut");
                await _unitOfWork.CommitAsync(innerCt);

                return MapToResponse(request);
            }, cancellationToken: ct);
        }

        private async Task<OvertimeRequest> GetRequestOrThrowAsync(int id, CancellationToken ct)
        {
            return await _overtimeRepo.GetDetailAsync(id, ct)
                ?? throw new InvalidOperationException("Yêu cầu OT không tồn tại.");
        }

        private async Task<Employee> GetEmployeeByAccountAsync(int accountId, CancellationToken ct)
        {
            return await _employeeRepo.GetByAccountIdAsync(accountId, ct)
                ?? throw new UnauthorizedAccessException("Tài khoản chưa liên kết hồ sơ nhân sự.");
        }

        private async Task EnsureNoOverlapAsync(int employeeId, DateTime startAt, DateTime endAt, CancellationToken ct)
        {
            var hasOverlap = await _overtimeRepo.HasOverlappingRequestAsync(employeeId, startAt, endAt, null, ct);
            if (hasOverlap)
                throw new InvalidOperationException("Nhân viên đã có yêu cầu OT trùng khung giờ trong ngày này.");
        }

        private static List<OvertimeEmployeeOptionDto> MapEmployeeOptions(IEnumerable<Employee> employees)
        {
            return employees
                .OrderBy(e => e.FullName)
                .Select(e => new OvertimeEmployeeOptionDto
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    DepartmentName = e.Department?.DeptName
                })
                .ToList();
        }

        private async Task<Employee> ResolveTargetEmployeeAsync(int? employeeId, Employee? actorEmployee, int actorAccountId, string actorRoleName, CancellationToken ct)
        {
            if (!employeeId.HasValue)
            {
                if (actorEmployee != null)
                    return actorEmployee;

                throw new UnauthorizedAccessException("Tài khoản Admin cần chọn nhân viên khi tạo yêu cầu OT.");
            }

            if (actorEmployee != null && employeeId.Value == actorEmployee.Id)
                return actorEmployee;

            if (!IsManager(actorRoleName) && !IsHr(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Bạn không được tạo yêu cầu OT cho nhân viên khác.");

            var target = await _employeeRepo.GetByIdAsync(employeeId.Value, ct)
                ?? throw new InvalidOperationException("Nhân viên được chọn không tồn tại.");

            if (IsManager(actorRoleName) && !IsAdmin(actorRoleName) &&
                (!target.DeptId.HasValue || !(await GetManagedDepartmentIdsAsync(actorAccountId, ct)).Contains(target.DeptId.Value)))
                throw new UnauthorizedAccessException("Manager chỉ được tạo OT cho nhân viên thuộc phòng ban của mình.");

            return target;
        }

        private async Task EnsureManagerCanAccessAsync(Employee targetEmployee, int actorAccountId, string actorRoleName, CancellationToken ct)
        {
            if (IsAdmin(actorRoleName))
                return;

            if (!IsManager(actorRoleName))
                throw new UnauthorizedAccessException("Chi Manager duoc duyet buoc nghiep vu OT.");

            var managedDeptIds = await GetManagedDepartmentIdsAsync(actorAccountId, ct);
            if (!targetEmployee.DeptId.HasValue || !managedDeptIds.Contains(targetEmployee.DeptId.Value))
                throw new UnauthorizedAccessException("Manager chỉ được duyệt OT của nhân viên trong phòng ban mình.");
        }

        private async Task<HashSet<int>> GetManagedDepartmentIdsAsync(int actorAccountId, CancellationToken ct)
        {
            var deptIds = await _employeeRepo.GetManagedDepartmentIdsByAccountIdAsync(actorAccountId, ct);
            if (deptIds.Count == 0)
                throw new UnauthorizedAccessException("Tai khoan Manager chua duoc gan phong ban quan ly.");
            return deptIds.ToHashSet();
        }

        private static (DateTime StartAt, DateTime EndAt) ResolveTimeRange(DateTime workDate, TimeSpan startTime, TimeSpan endTime)
        {
            if (workDate.Date < DateTime.UtcNow.Date.AddDays(-7))
                throw new InvalidOperationException("Không thể đăng ký OT cho ngày quá xa trong quá khứ.");

            if (endTime == startTime)
                throw new InvalidOperationException("Giờ kết thúc OT không được trùng giờ bắt đầu.");

            var startAt = workDate.Date.Add(startTime);
            var endAt = workDate.Date.Add(endTime);
            if (endAt < startAt)
                endAt = endAt.AddDays(1);

            if ((endAt - startAt).TotalHours > 16)
                throw new InvalidOperationException("Khung OT không được vượt quá 16 giờ.");

            return (startAt, endAt);
        }

        private static int CalculateApprovedMinutes(DateTime startAt, DateTime endAt)
        {
            return (int)Math.Floor((endAt - startAt).TotalMinutes);
        }

        private static OvertimeRequestResponseDto MapToResponse(OvertimeRequest request)
        {
            return new OvertimeRequestResponseDto
            {
                Id = request.Id,
                EmployeeId = request.EmployeeId,
                EmployeeName = request.Employee.FullName,
                DepartmentName = request.Employee.Department?.DeptName,
                RequestedByAccountId = request.RequestedByAccountId,
                WorkDate = request.WorkDate,
                StartAt = request.StartAt,
                EndAt = request.EndAt,
                StartTime = request.StartTime.ToString(@"hh\:mm"),
                EndTime = request.EndTime.ToString(@"hh\:mm"),
                Reason = request.Reason,
                ProjectCode = request.ProjectCode,
                Status = request.Status.ToString(),
                ManagerNote = request.ManagerNote,
                HrNote = request.HrNote,
                DirectorNote = request.DirectorNote,
                ApprovedMinutes = request.ApprovedMinutes,
                ActualOtMinutes = request.ActualOtMinutes,
                IsPayrollLocked = request.IsPayrollLocked,
                PayrollPeriod = request.PayrollPeriod,
                CreatedAt = request.CreatedAt,
                ReconciledAt = request.ReconciledAt,
                Segments = request.Segments
                    .OrderBy(s => s.SegmentStartAt)
                    .Select(s => new OvertimeSegmentDto
                    {
                        SegmentStartAt = s.SegmentStartAt,
                        SegmentEndAt = s.SegmentEndAt,
                        Minutes = s.Minutes,
                        PolicyCode = s.PolicyCode,
                        RateMultiplierSnapshot = s.RateMultiplierSnapshot
                    })
                    .ToList()
            };
        }

        private static bool IsManager(string role) => string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase);
        private static bool IsHr(string role) => string.Equals(role, "HR", StringComparison.OrdinalIgnoreCase);
        private static bool IsDirector(string role) => string.Equals(role, "Director", StringComparison.OrdinalIgnoreCase);
        private static bool IsAdmin(string role) => string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
    }
}
