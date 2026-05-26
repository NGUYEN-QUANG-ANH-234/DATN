using HRM.backend.src.HRM.Application.DTOs.TimeAttendance;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Usecases;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System.HRM.backend.src.HRM.Infrastructure.Repositories.Interfaces.System;
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

        public OvertimeRequestUseCase(
            IOvertimeRequestRepository overtimeRepo,
            IEmployeeRepository employeeRepo,
            IAttendanceRepository attendanceRepo,
            IAuditLogRepository auditLogRepo,
            IApprovalConflictGuard approvalConflictGuard,
            IUnitOfWork unitOfWork,
            ILockService lockService,
            IAppCache cache,
            IIdempotencyService idempotencyService)
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
        }

        public async Task<int> CreateAsync(CreateOvertimeRequestDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default, string? idempotencyKey = null)
        {
            var existingResourceId = string.IsNullOrWhiteSpace(idempotencyKey)
                ? null
                : await _idempotencyService.FindResourceIdAsync("OVERTIME_CREATE", idempotencyKey, ct);
            if (existingResourceId.HasValue)
                return existingResourceId.Value;

            ValidateTimeRange(dto.WorkDate, dto.StartTime, dto.EndTime);

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
                    await EnsureNoOverlapAsync(targetEmployee.Id, dto.WorkDate, dto.StartTime, dto.EndTime, innerCt);

                    var request = new OvertimeRequest
                    {
                        EmployeeId = targetEmployee.Id,
                        RequestedByAccountId = actorAccountId,
                        WorkDate = dto.WorkDate.Date,
                        StartTime = dto.StartTime,
                        EndTime = dto.EndTime,
                        Reason = dto.Reason.Trim(),
                        ProjectCode = string.IsNullOrWhiteSpace(dto.ProjectCode) ? null : dto.ProjectCode.Trim(),
                        Status = requiresDirectorApproval
                            ? OvertimeRequestStatus.PendingDirector
                            : isManagerCreatedForOther ? OvertimeRequestStatus.PendingHR : OvertimeRequestStatus.PendingManager,
                        ManagerReviewerAccountId = isManagerCreatedForOther && !requiresDirectorApproval ? actorAccountId : null,
                        ManagerReviewedAt = isManagerCreatedForOther && !requiresDirectorApproval ? DateTime.UtcNow : null,
                        ManagerNote = isManagerCreatedForOther && !requiresDirectorApproval
                            ? IsAdmin(actorRoleName)
                                ? "Admin tao yeu cau OT cho nhan vien."
                                : "Manager tao yeu cau OT cho nhan vien trong phong."
                            : null,
                        ApprovedMinutes = CalculateApprovedMinutes(dto.StartTime, dto.EndTime)
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
                throw new UnauthorizedAccessException("Chi Manager hoac Admin duoc tao OT hang loat cho nhan vien.");

            ValidateTimeRange(dto.WorkDate, dto.StartTime, dto.EndTime);

            var employeeIds = dto.EmployeeIds.Distinct().ToList();
            if (employeeIds.Count == 0)
                throw new InvalidOperationException("Danh sach nhan vien OT khong duoc de trong.");

            var actorEmployee = IsAdmin(actorRoleName)
                ? await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct)
                : await GetEmployeeByAccountAsync(actorAccountId, ct);
            if (IsManager(actorRoleName) && !IsAdmin(actorRoleName) && !actorEmployee!.DeptId.HasValue)
                throw new UnauthorizedAccessException("Tai khoan Manager chua duoc gan phong ban.");

            var employees = (await _employeeRepo.FindAsync(e => employeeIds.Contains(e.Id), ct)).ToList();
            var missingIds = employeeIds.Except(employees.Select(e => e.Id)).ToList();
            if (missingIds.Count > 0)
                throw new InvalidOperationException($"Khong tim thay nhan vien: {string.Join(", ", missingIds)}.");

            if (IsManager(actorRoleName) && !IsAdmin(actorRoleName))
            {
                var outsideDept = employees.Where(e => e.DeptId != actorEmployee!.DeptId).Select(e => e.Id).ToList();
                if (outsideDept.Count > 0)
                    throw new UnauthorizedAccessException($"Manager chi duoc tao OT cho nhan vien thuoc phong ban cua minh. Ngoai pham vi: {string.Join(", ", outsideDept)}.");
            }

            var lockKey = $"overtime_create_bulk_{dto.WorkDate:yyyyMMdd}_{string.Join("_", employeeIds.OrderBy(id => id))}";
            return await _lockService.GetWithLockAsync(
                lockKey,
                async (innerCt) =>
                {
                    foreach (var employee in employees)
                    {
                        await EnsureNoOverlapAsync(employee.Id, dto.WorkDate, dto.StartTime, dto.EndTime, innerCt);
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
                        Reason = dto.Reason.Trim(),
                        ProjectCode = string.IsNullOrWhiteSpace(dto.ProjectCode) ? null : dto.ProjectCode.Trim(),
                        Status = directorApprovalEmployeeIds.Contains(employee.Id) ? OvertimeRequestStatus.PendingDirector : OvertimeRequestStatus.PendingHR,
                        ManagerReviewerAccountId = directorApprovalEmployeeIds.Contains(employee.Id) ? null : actorAccountId,
                        ManagerReviewedAt = directorApprovalEmployeeIds.Contains(employee.Id) ? null : now,
                        ManagerNote = directorApprovalEmployeeIds.Contains(employee.Id)
                            ? null
                            : IsAdmin(actorRoleName)
                                ? "Admin tao yeu cau OT hang loat cho nhan vien."
                                : "Manager tao yeu cau OT hang loat cho nhan vien trong phong.",
                        ApprovedMinutes = CalculateApprovedMinutes(dto.StartTime, dto.EndTime)
                    }).ToList();

                    await _overtimeRepo.AddRangeAsync(requests, innerCt);
                    var actorLabel = IsAdmin(actorRoleName) ? "Admin" : "Manager";
                    await _auditLogRepo.LogSystemEventAsync("OT_MANAGER_CREATE_BULK", actorAccountId, "overtime_requests", $"{actorLabel} tao {requests.Count} yeu cau OT hang loat.");
                    await _unitOfWork.CommitAsync(innerCt);

                    return requests.Select(r => r.Id).ToList();
                },
                cancellationToken: ct);
        }

        public async Task<IEnumerable<OvertimeEmployeeOptionDto>> GetAssignableEmployeesAsync(int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            if (!IsManager(actorRoleName) && !IsHr(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Ban khong co quyen chon nhan vien de tao OT.");

            if (IsManager(actorRoleName) && !IsAdmin(actorRoleName))
            {
                var manager = await GetEmployeeByAccountAsync(actorAccountId, ct);
                if (!manager.DeptId.HasValue)
                    throw new UnauthorizedAccessException("Tai khoan Manager chua duoc gan phong ban.");

                return await _cache.GetOrSetWithLockAsync(
                    $"employee_options_dept_{manager.DeptId.Value}",
                    async (innerCt) =>
                    {
                        var employees = await _employeeRepo.GetActiveByDeptWithDepartmentAsync(manager.DeptId.Value, manager.Id, innerCt);
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
                throw new UnauthorizedAccessException("Chi Manager hoac Admin duoc xem danh sach OT cho truong phong duyet.");

            if (IsAdmin(actorRoleName))
                return (await _overtimeRepo.GetByStatusAsync(OvertimeRequestStatus.PendingManager, ct)).Select(MapToResponse);

            var manager = await GetEmployeeByAccountAsync(actorAccountId, ct);
            if (!manager.DeptId.HasValue)
                throw new UnauthorizedAccessException("Tai khoan Manager chua duoc gan phong ban.");

            return (await _overtimeRepo.GetPendingManagerByDeptAsync(manager.DeptId.Value, ct)).Select(MapToResponse);
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
                throw new UnauthorizedAccessException("Chi Giam doc hoac Admin duoc xem danh sach OT can duyet truc tiep.");

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
                    throw new InvalidOperationException("Yeu cau OT khong o trang thai cho Manager duyet.");

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
                    throw new InvalidOperationException("Yeu cau OT khong o trang thai cho HR xac nhan.");

                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(request.EmployeeId, actorAccountId, innerCt);

                request.HrReviewerAccountId = actorAccountId;
                request.HrReviewedAt = DateTime.UtcNow;
                request.HrNote = dto.Note;
                request.Status = dto.IsApproved ? OvertimeRequestStatus.Approved : OvertimeRequestStatus.Rejected;

                if (dto.IsApproved)
                    ApplyActualOvertime(request, await _attendanceRepo.GetTodayLogAsync(request.EmployeeId, request.WorkDate, innerCt));

                await _overtimeRepo.UpdateAsync(request, innerCt);
                await _auditLogRepo.LogSystemEventAsync("OT_HR_CONFIRM", actorAccountId, "overtime_requests", $"HR {(dto.IsApproved ? "confirmed" : "rejected")} OT #{request.Id}");
                await _unitOfWork.CommitAsync(innerCt);
                return true;
            }, cancellationToken: ct);
        }

        public async Task<bool> ReviewByDirectorAsync(int id, ReviewOvertimeRequestDto dto, int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            if (!IsDirector(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chi Giam doc hoac Admin duoc duyet OT truc tiep.");

            return await _lockService.GetWithLockAsync($"overtime_{id}", async (innerCt) =>
            {
                var request = await GetRequestOrThrowAsync(id, innerCt);
                if (request.Status != OvertimeRequestStatus.PendingDirector)
                    throw new InvalidOperationException("Yeu cau OT khong o trang thai cho Giam doc duyet.");

                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(request.EmployeeId, actorAccountId, innerCt);

                request.HrReviewerAccountId = actorAccountId;
                request.HrReviewedAt = DateTime.UtcNow;
                request.HrNote = dto.Note;
                request.Status = dto.IsApproved ? OvertimeRequestStatus.Approved : OvertimeRequestStatus.Rejected;

                if (dto.IsApproved)
                    ApplyActualOvertime(request, await _attendanceRepo.GetTodayLogAsync(request.EmployeeId, request.WorkDate, innerCt));

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
                if (request.Status != OvertimeRequestStatus.Approved)
                    throw new InvalidOperationException("Chi yeu cau OT da duoc duyet moi duoc doi chieu.");

                if (request.IsPayrollLocked)
                    throw new InvalidOperationException("Yeu cau OT da duoc khoa trong ky luong, khong the doi chieu lai.");

                var attendanceLog = await _attendanceRepo.GetTodayLogAsync(request.EmployeeId, request.WorkDate, innerCt);
                ApplyActualOvertime(request, attendanceLog);

                await _overtimeRepo.UpdateAsync(request, innerCt);
                await _auditLogRepo.LogSystemEventAsync("OT_RECONCILE", actorAccountId, "overtime_requests", $"Doi chieu OT #{request.Id}: {request.ActualOtMinutes} phut");
                await _unitOfWork.CommitAsync(innerCt);

                return MapToResponse(request);
            }, cancellationToken: ct);
        }

        private async Task<OvertimeRequest> GetRequestOrThrowAsync(int id, CancellationToken ct)
        {
            return await _overtimeRepo.GetDetailAsync(id, ct)
                ?? throw new InvalidOperationException("Yeu cau OT khong ton tai.");
        }

        private async Task<Employee> GetEmployeeByAccountAsync(int accountId, CancellationToken ct)
        {
            return await _employeeRepo.GetByAccountIdAsync(accountId, ct)
                ?? throw new UnauthorizedAccessException("Tai khoan chua lien ket ho so nhan su.");
        }

        private async Task EnsureNoOverlapAsync(int employeeId, DateTime workDate, TimeSpan startTime, TimeSpan endTime, CancellationToken ct)
        {
            var hasOverlap = await _overtimeRepo.HasOverlappingRequestAsync(employeeId, workDate, startTime, endTime, null, ct);
            if (hasOverlap)
                throw new InvalidOperationException("Nhan vien da co yeu cau OT trung khung gio trong ngay nay.");
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

                throw new UnauthorizedAccessException("Tai khoan Admin can chon nhan vien khi tao yeu cau OT.");
            }

            if (actorEmployee != null && employeeId.Value == actorEmployee.Id)
                return actorEmployee;

            if (!IsManager(actorRoleName) && !IsHr(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Ban khong duoc tao yeu cau OT cho nhan vien khac.");

            var target = await _employeeRepo.GetByIdAsync(employeeId.Value, ct)
                ?? throw new InvalidOperationException("Nhan vien duoc chon khong ton tai.");

            if (IsManager(actorRoleName) && !IsAdmin(actorRoleName) && target.DeptId != actorEmployee?.DeptId)
                throw new UnauthorizedAccessException("Manager chi duoc tao OT cho nhan vien thuoc phong ban cua minh.");

            return target;
        }

        private async Task EnsureManagerCanAccessAsync(Employee targetEmployee, int actorAccountId, string actorRoleName, CancellationToken ct)
        {
            if (IsAdmin(actorRoleName))
                return;

            if (!IsManager(actorRoleName))
                throw new UnauthorizedAccessException("Chi Manager duoc duyet buoc nghiep vu OT.");

            var manager = await GetEmployeeByAccountAsync(actorAccountId, ct);
            if (!manager.DeptId.HasValue || targetEmployee.DeptId != manager.DeptId)
                throw new UnauthorizedAccessException("Manager chi duoc duyet OT cua nhan vien trong phong ban minh.");
        }

        private static void ValidateTimeRange(DateTime workDate, TimeSpan startTime, TimeSpan endTime)
        {
            if (workDate.Date < DateTime.UtcNow.Date.AddDays(-7))
                throw new InvalidOperationException("Khong the dang ky OT cho ngay qua xa trong qua khu.");

            if (endTime <= startTime)
                throw new InvalidOperationException("Gio ket thuc OT phai lon hon gio bat dau OT trong cung ngay.");
        }

        private static void ApplyActualOvertime(OvertimeRequest request, AttendanceLog? attendanceLog)
        {
            if (attendanceLog?.CheckIn == null || attendanceLog.CheckOut == null)
            {
                request.ActualOtMinutes = 0;
                request.ReconciledAt = DateTime.UtcNow;
                return;
            }

            var approvedStart = request.WorkDate.Date.Add(request.StartTime);
            var approvedEnd = request.WorkDate.Date.Add(request.EndTime);
            var actualStart = attendanceLog.CheckIn.Value > approvedStart ? attendanceLog.CheckIn.Value : approvedStart;
            var actualEnd = attendanceLog.CheckOut.Value < approvedEnd ? attendanceLog.CheckOut.Value : approvedEnd;

            request.ActualOtMinutes = actualEnd > actualStart
                ? (int)Math.Floor((actualEnd - actualStart).TotalMinutes)
                : 0;
            request.ReconciledAt = DateTime.UtcNow;
        }

        private static int CalculateApprovedMinutes(TimeSpan startTime, TimeSpan endTime)
        {
            return (int)Math.Floor((endTime - startTime).TotalMinutes);
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
                StartTime = request.StartTime.ToString(@"hh\:mm"),
                EndTime = request.EndTime.ToString(@"hh\:mm"),
                Reason = request.Reason,
                ProjectCode = request.ProjectCode,
                Status = request.Status.ToString(),
                ManagerNote = request.ManagerNote,
                HrNote = request.HrNote,
                ApprovedMinutes = request.ApprovedMinutes,
                ActualOtMinutes = request.ActualOtMinutes,
                IsPayrollLocked = request.IsPayrollLocked,
                PayrollPeriod = request.PayrollPeriod,
                CreatedAt = request.CreatedAt,
                ReconciledAt = request.ReconciledAt
            };
        }

        private static bool IsManager(string role) => string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase);
        private static bool IsHr(string role) => string.Equals(role, "HR", StringComparison.OrdinalIgnoreCase);
        private static bool IsDirector(string role) => string.Equals(role, "Director", StringComparison.OrdinalIgnoreCase);
        private static bool IsAdmin(string role) => string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
    }
}
