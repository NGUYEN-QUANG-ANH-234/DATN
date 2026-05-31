using System.Text.Json;
using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases;
using HRM.backend.src.HRM.Application.Interfaces.Services;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;

namespace HRM.backend.src.HRM.Application.UseCases.EmployeeProfile
{
    public class DependentUseCase : IDependentUseCase
    {
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IDependentRepository _dependentRepo;
        private readonly IDependentUpdateRequestRepository _requestRepo;
        private readonly IStorageService _storageService;
        private readonly IApprovalConflictGuard _approvalConflictGuard;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;

        public DependentUseCase(
            IEmployeeRepository employeeRepo,
            IDependentRepository dependentRepo,
            IDependentUpdateRequestRepository requestRepo,
            IStorageService storageService,
            IApprovalConflictGuard approvalConflictGuard,
            IAuditLogRepository auditLogRepo,
            IUnitOfWork unitOfWork,
            ILockService lockService)
        {
            _employeeRepo = employeeRepo;
            _dependentRepo = dependentRepo;
            _requestRepo = requestRepo;
            _storageService = storageService;
            _approvalConflictGuard = approvalConflictGuard;
            _auditLogRepo = auditLogRepo;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
        }

        public async Task<List<DependentDto>> GetMyDependentsAsync(int accountId, CancellationToken ct = default)
        {
            var employee = await _employeeRepo.GetByAccountIdAsync(accountId, ct)
                ?? throw new ArgumentException("Tài khoản chưa liên kết hồ sơ.");
            var dependents = await _dependentRepo.GetByEmployeeIdAsync(employee.Id, true, ct);
            return dependents.Select(MapToDto).ToList();
        }

        public async Task<List<DependentDto>> GetEmployeeDependentsAsync(int employeeId, CancellationToken ct = default)
        {
            var dependents = await _dependentRepo.GetByEmployeeIdAsync(employeeId, true, ct);
            return dependents.Select(MapToDto).ToList();
        }

        public async Task<int> RequestCreateDependentAsync(int accountId, DependentRequestDto dto, CancellationToken ct = default)
        {
            var employee = await _employeeRepo.GetByAccountIdAsync(accountId, ct)
                ?? throw new ArgumentException("Tài khoản chưa liên kết hồ sơ.");
            return await CreateRequestAsync(employee.Id, null, "CREATE", dto, accountId, ct);
        }

        public async Task<int> RequestUpdateDependentAsync(int accountId, int dependentId, DependentRequestDto dto, CancellationToken ct = default)
        {
            var employee = await _employeeRepo.GetByAccountIdAsync(accountId, ct)
                ?? throw new ArgumentException("Tài khoản chưa liên kết hồ sơ.");
            var dependent = await _dependentRepo.GetByIdForEmployeeAsync(dependentId, employee.Id, ct)
                ?? throw new ArgumentException("Không tìm thấy người phụ thuộc.");
            return await CreateRequestAsync(employee.Id, dependent.Id, "UPDATE", dto, accountId, ct);
        }

        public async Task<int> RequestDeactivateDependentAsync(int accountId, int dependentId, CancellationToken ct = default)
        {
            var employee = await _employeeRepo.GetByAccountIdAsync(accountId, ct)
                ?? throw new ArgumentException("Tài khoản chưa liên kết hồ sơ.");
            var dependent = await _dependentRepo.GetByIdForEmployeeAsync(dependentId, employee.Id, ct)
                ?? throw new ArgumentException("Không tìm thấy người phụ thuộc.");

            var dto = new DependentRequestDto
            {
                FullName = dependent.FullName,
                Relationship = dependent.Relationship,
                IdNumber = dependent.IdNumber,
                TaxDependentCode = dependent.TaxDependentCode,
                BirthDate = dependent.BirthDate,
                ValidFrom = dependent.ValidFrom,
                ValidTo = DateTime.UtcNow.Date,
                Note = dependent.Note
            };

            return await CreateRequestAsync(employee.Id, dependent.Id, "DEACTIVATE", dto, accountId, ct);
        }

        public async Task<List<PendingDependentRequestDto>> GetPendingRequestsAsync(int actorAccountId, string actorRoleName, CancellationToken ct = default)
        {
            var pendingStatuses = IsDirector(actorRoleName)
                ? new[] { RequestStatus.Pending_Director }
                : IsAdmin(actorRoleName)
                    ? new[] { RequestStatus.Pending_HR, RequestStatus.Pending_Director }
                    : new[] { RequestStatus.Pending_HR };

            var requests = await _requestRepo.GetPendingByStatusesAsync(pendingStatuses, ct);

            if (IsHr(actorRoleName) && !IsAdmin(actorRoleName))
            {
                requests = requests
                    .Where(r => r.Employee == null ||
                                !r.Employee.AccountId.HasValue ||
                                r.Employee.AccountId.Value != actorAccountId)
                    .ToList();
            }

            return requests.Select(r => new PendingDependentRequestDto
            {
                Id = r.Id,
                EmployeeId = r.EmployeeId,
                EmployeeName = r.Employee?.FullName ?? "Không xác định",
                EmployeeCode = r.Employee?.EmployeeCode ?? "N/A",
                DependentId = r.DependentId,
                ActionType = r.ActionType,
                RequestedDataJson = r.RequestedDataJson,
                EvidenceUrl = r.EvidenceUrl,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt
            }).ToList();
        }

        public async Task<bool> ReviewRequestAsync(int requestId, int actorAccountId, string actorRoleName, ReviewProfileUpdateDto dto, CancellationToken ct = default)
        {
            return await _lockService.GetWithLockAsync($"dependent_request_{requestId}", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var request = await _requestRepo.GetByIdForUpdateAsync(requestId, innerCt)
                        ?? throw new ArgumentException("Yêu cầu không tồn tại.");

                    if (request.Status != RequestStatus.Pending_HR && request.Status != RequestStatus.Pending_Director)
                        throw new ArgumentException("Yêu cầu đã được xử lý.");

                    if (request.Status == RequestStatus.Pending_Director)
                        EnsureDirectorOrAdmin(actorRoleName);
                    else
                        EnsureHrOrAdmin(actorRoleName);

                    await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(request.EmployeeId, actorAccountId, innerCt);

                    request.ReviewerAccountId = actorAccountId;
                    request.ReviewedAt = DateTime.UtcNow;

                    if (!dto.IsApproved)
                    {
                        request.Status = RequestStatus.Rejected;
                        request.RejectReason = dto.RejectReason;
                    }
                    else
                    {
                        await ApplyApprovedRequestAsync(request, innerCt);
                        request.Status = RequestStatus.Approved;
                    }

                    _requestRepo.Update(request);
                    await _auditLogRepo.LogSystemEventAsync(
                        dto.IsApproved ? "DEPENDENT_REQUEST_APPROVED" : "DEPENDENT_REQUEST_REJECTED",
                        actorAccountId,
                        "dependents",
                        $"Dependent request {request.Id}");
                    await _unitOfWork.CommitAsync(innerCt);
                }, innerCt);

                return true;
            }, cancellationToken: ct);
        }

        public async Task<DependentDto> HrCreateDependentAsync(int employeeId, HrDependentDto dto, int actorAccountId, CancellationToken ct = default)
        {
            Validate(dto);
            var entity = new Dependent
            {
                EmployeeId = employeeId,
                FullName = dto.FullName.Trim(),
                Relationship = dto.Relationship,
                IdNumber = dto.IdNumber,
                TaxDependentCode = dto.TaxDependentCode,
                BirthDate = dto.BirthDate,
                ValidFrom = dto.ValidFrom.Date,
                ValidTo = dto.ValidTo?.Date,
                IsActive = dto.IsActive,
                Note = dto.Note,
                CreatedAt = DateTime.UtcNow
            };

            await _dependentRepo.AddAsync(entity, ct);
            await _auditLogRepo.LogSystemEventAsync("HR_CREATE_DEPENDENT", actorAccountId, "dependents", entity.FullName);
            await _unitOfWork.CommitAsync(ct);
            return MapToDto(entity);
        }

        public async Task<DependentDto> HrUpdateDependentAsync(int employeeId, int dependentId, HrDependentDto dto, int actorAccountId, CancellationToken ct = default)
        {
            Validate(dto);
            var entity = await _dependentRepo.GetByIdForEmployeeAsync(dependentId, employeeId, ct)
                ?? throw new ArgumentException("Không tìm thấy người phụ thuộc.");
            ApplyDto(entity, dto);
            entity.UpdatedAt = DateTime.UtcNow;
            _dependentRepo.Update(entity);
            await _auditLogRepo.LogSystemEventAsync("HR_UPDATE_DEPENDENT", actorAccountId, "dependents", entity.FullName);
            await _unitOfWork.CommitAsync(ct);
            return MapToDto(entity);
        }

        public async Task<bool> HrDeactivateDependentAsync(int employeeId, int dependentId, int actorAccountId, CancellationToken ct = default)
        {
            var entity = await _dependentRepo.GetByIdForEmployeeAsync(dependentId, employeeId, ct)
                ?? throw new ArgumentException("Không tìm thấy người phụ thuộc.");
            entity.IsActive = false;
            entity.ValidTo ??= DateTime.UtcNow.Date;
            entity.UpdatedAt = DateTime.UtcNow;
            _dependentRepo.Update(entity);
            await _auditLogRepo.LogSystemEventAsync("HR_DEACTIVATE_DEPENDENT", actorAccountId, "dependents", entity.FullName);
            await _unitOfWork.CommitAsync(ct);
            return true;
        }

        private async Task<int> CreateRequestAsync(int employeeId, int? dependentId, string actionType, DependentRequestDto dto, int actorAccountId, CancellationToken ct)
        {
            Validate(dto);

            return await _lockService.GetWithLockAsync($"dependent_request_create_{employeeId}_{dependentId}_{actionType}", async (innerCt) =>
            {
                var payload = BuildPayload(dto);
                var pendingRequests = await _requestRepo.GetPendingForEmployeeAsync(employeeId, dependentId, innerCt);
                if (HasConflictingPendingRequest(pendingRequests, dependentId, actionType, payload))
                    throw new InvalidOperationException("Đang có yêu cầu người phụ thuộc chờ duyệt.");

                var isHrTarget = await _approvalConflictGuard.IsEmployeeInRoleAsync(employeeId, "HR", innerCt);
                var requiresDirectorApproval = isHrTarget &&
                    !await _approvalConflictGuard.HasAlternativeHrApproverAsync(employeeId, innerCt);

                var evidenceUrl = dto.EvidenceFile != null
                    ? await _storageService.UploadFileAsync(dto.EvidenceFile, "dependent-evidences", innerCt)
                    : null;

                var request = new DependentUpdateRequest
                {
                    EmployeeId = employeeId,
                    DependentId = dependentId,
                    ActionType = actionType,
                    RequestedDataJson = JsonSerializer.Serialize(payload),
                    EvidenceUrl = evidenceUrl,
                    Status = requiresDirectorApproval ? RequestStatus.Pending_Director : RequestStatus.Pending_HR,
                    CreatedAt = DateTime.UtcNow
                };

                await _requestRepo.AddAsync(request, innerCt);
                await _auditLogRepo.LogSystemEventAsync("REQUEST_DEPENDENT_UPDATE", actorAccountId, "dependents", actionType);
                await _unitOfWork.CommitAsync(innerCt);
                return request.Id;
            }, cancellationToken: ct);
        }

        private async Task ApplyApprovedRequestAsync(DependentUpdateRequest request, CancellationToken ct)
        {
            var data = JsonSerializer.Deserialize<DependentPayload>(request.RequestedDataJson)
                ?? throw new ArgumentException("Dữ liệu yêu cầu không hợp lệ.");

            if (request.ActionType == "CREATE")
            {
                var entity = new Dependent
                {
                    EmployeeId = request.EmployeeId,
                    FullName = data.FullName,
                    EvidenceUrl = request.EvidenceUrl,
                    CreatedAt = DateTime.UtcNow
                };
                ApplyPayload(entity, data);
                await _dependentRepo.AddAsync(entity, ct);
                return;
            }

            var dependent = request.DependentId.HasValue
                ? await _dependentRepo.GetByIdForEmployeeAsync(request.DependentId.Value, request.EmployeeId, ct)
                : null;
            if (dependent == null)
                throw new ArgumentException("Không tìm thấy người phụ thuộc.");

            if (request.ActionType == "DEACTIVATE")
            {
                dependent.IsActive = false;
                dependent.ValidTo = data.ValidTo ?? DateTime.UtcNow.Date;
                dependent.UpdatedAt = DateTime.UtcNow;
                return;
            }

            ApplyPayload(dependent, data);
            if (!string.IsNullOrEmpty(request.EvidenceUrl))
                dependent.EvidenceUrl = request.EvidenceUrl;
            dependent.UpdatedAt = DateTime.UtcNow;
        }

        private static Dictionary<string, object?> BuildPayload(DependentRequestDto dto) => new()
        {
            ["FullName"] = dto.FullName.Trim(),
            ["Relationship"] = dto.Relationship,
            ["IdNumber"] = dto.IdNumber,
            ["TaxDependentCode"] = dto.TaxDependentCode,
            ["BirthDate"] = dto.BirthDate,
            ["ValidFrom"] = dto.ValidFrom.Date,
            ["ValidTo"] = dto.ValidTo?.Date,
            ["Note"] = dto.Note
        };

        private static bool HasConflictingPendingRequest(
            IEnumerable<DependentUpdateRequest> pendingRequests,
            int? dependentId,
            string actionType,
            Dictionary<string, object?> payload)
        {
            if (dependentId.HasValue)
                return pendingRequests.Any();

            if (!string.Equals(actionType, "CREATE", StringComparison.OrdinalIgnoreCase))
                return pendingRequests.Any(r => string.Equals(r.ActionType, actionType, StringComparison.OrdinalIgnoreCase));

            return pendingRequests.Any(request => IsSameCreatePayload(request.RequestedDataJson, payload));
        }

        private static bool IsSameCreatePayload(string requestedDataJson, Dictionary<string, object?> payload)
        {
            try
            {
                var pending = JsonSerializer.Deserialize<DependentPayload>(requestedDataJson);
                if (pending == null) return false;

                var fullName = Convert.ToString(payload["FullName"])?.Trim();
                var idNumber = Convert.ToString(payload["IdNumber"])?.Trim();
                var birthDate = payload["BirthDate"] is DateTime dt ? dt.Date : (DateTime?)null;

                return string.Equals(pending.FullName.Trim(), fullName, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals((pending.IdNumber ?? string.Empty).Trim(), idNumber ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                       Nullable.Equals(pending.BirthDate?.Date, birthDate?.Date);
            }
            catch
            {
                return true;
            }
        }

        private static void ApplyPayload(Dependent entity, DependentPayload payload)
        {
            entity.FullName = payload.FullName;
            entity.Relationship = payload.Relationship;
            entity.IdNumber = payload.IdNumber;
            entity.TaxDependentCode = payload.TaxDependentCode;
            entity.BirthDate = payload.BirthDate;
            entity.ValidFrom = payload.ValidFrom.Date;
            entity.ValidTo = payload.ValidTo?.Date;
            entity.Note = payload.Note;
            entity.IsActive = true;
        }

        private static void ApplyDto(Dependent entity, HrDependentDto dto)
        {
            entity.FullName = dto.FullName.Trim();
            entity.Relationship = dto.Relationship;
            entity.IdNumber = dto.IdNumber;
            entity.TaxDependentCode = dto.TaxDependentCode;
            entity.BirthDate = dto.BirthDate;
            entity.ValidFrom = dto.ValidFrom.Date;
            entity.ValidTo = dto.ValidTo?.Date;
            entity.IsActive = dto.IsActive;
            entity.Note = dto.Note;
        }

        private static void Validate(DependentRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FullName))
                throw new ArgumentException("Họ tên người phụ thuộc không được để trống.");
            if (dto.ValidFrom == default)
                throw new ArgumentException("Ngày hiệu lực không hợp lệ.");
            if (dto.ValidTo.HasValue && dto.ValidTo.Value.Date < dto.ValidFrom.Date)
                throw new ArgumentException("Ngay ket thuc phai lon hon ngay hieu luc.");
        }

        private static void Validate(HrDependentDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FullName))
                throw new ArgumentException("Họ tên người phụ thuộc không được để trống.");
            if (dto.ValidFrom == default)
                throw new ArgumentException("Ngày hiệu lực không hợp lệ.");
            if (dto.ValidTo.HasValue && dto.ValidTo.Value.Date < dto.ValidFrom.Date)
                throw new ArgumentException("Ngay ket thuc phai lon hon ngay hieu luc.");
        }

        private static DependentDto MapToDto(Dependent entity) => new()
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId ?? 0,
            FullName = entity.FullName,
            Relationship = entity.Relationship,
            IdNumber = entity.IdNumber,
            TaxDependentCode = entity.TaxDependentCode,
            BirthDate = entity.BirthDate,
            ValidFrom = entity.ValidFrom,
            ValidTo = entity.ValidTo,
            IsActive = entity.IsActive,
            EvidenceUrl = entity.EvidenceUrl,
            Note = entity.Note
        };

        private static void EnsureHrOrAdmin(string actorRoleName)
        {
            if (!IsHr(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chỉ HR hoặc Admin được duyệt yêu cầu người phụ thuộc.");
        }

        private static void EnsureDirectorOrAdmin(string actorRoleName)
        {
            if (!IsDirector(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chỉ Giám đốc hoặc Admin được duyệt yêu cầu đặc biệt.");
        }

        private static bool IsHr(string role) => string.Equals(role, "HR", StringComparison.OrdinalIgnoreCase);
        private static bool IsAdmin(string role) => string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        private static bool IsDirector(string role) => string.Equals(role, "Director", StringComparison.OrdinalIgnoreCase);

        private class DependentPayload
        {
            public string FullName { get; set; } = string.Empty;
            public DependentRelation Relationship { get; set; }
            public string? IdNumber { get; set; }
            public string? TaxDependentCode { get; set; }
            public DateTime? BirthDate { get; set; }
            public DateTime ValidFrom { get; set; }
            public DateTime? ValidTo { get; set; }
            public string? Note { get; set; }
        }
    }
}
