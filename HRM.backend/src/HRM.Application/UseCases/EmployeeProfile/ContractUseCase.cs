using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;
using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using MediatR;

namespace HRM.backend.src.HRM.Application.UseCases.EmployeeProfile
{
    public class ContractUseCase : IContractUseCase
    {
        private readonly IContractRepository _contractRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly ISlaTrackingService _slaTrackingService;
        private readonly IApprovalWorkflowService _approvalService;
        private readonly IApprovalConflictGuard _approvalConflictGuard;
        private readonly IAccountRepository _accountRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;
        private readonly IIdempotencyService _idempotencyService;
        private readonly IMediator _mediator;

        public ContractUseCase(
            IContractRepository contractRepo,
            IEmployeeRepository employeeRepo,
            IAuditLogRepository auditLogRepo,
            ISlaTrackingService slaTrackingService,
            IApprovalWorkflowService approvalService,
            IApprovalConflictGuard approvalConflictGuard,
            IAccountRepository accountRepo,
            IUnitOfWork unitOfWork,
            IMediator mediator,
            ILockService lockService,
            IIdempotencyService idempotencyService)
        {
            _contractRepo = contractRepo;
            _employeeRepo = employeeRepo;
            _auditLogRepo = auditLogRepo;
            _slaTrackingService = slaTrackingService;
            _approvalService = approvalService;
            _approvalConflictGuard = approvalConflictGuard;
            _accountRepo = accountRepo;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
            _idempotencyService = idempotencyService;
            _mediator = mediator;
        }

        public async Task<int> CreateRequestAsync(int accountId, ContractRequestDto dto, CancellationToken ct, string? idempotencyKey = null)
        {
            var existingResourceId = string.IsNullOrWhiteSpace(idempotencyKey)
                ? null
                : await _idempotencyService.FindResourceIdAsync("CONTRACT_REQUEST_CREATE", idempotencyKey, ct);
            if (existingResourceId.HasValue)
                return existingResourceId.Value;

            var emp = await _employeeRepo.GetByAccountIdAsync(accountId, ct);
            if (emp == null)
                throw new ArgumentException("Không tìm thấy hồ sơ nhân viên.");
            if (!emp.DeptId.HasValue)
                throw new ArgumentException("Nhân viên chưa được phân phòng ban, không thể yêu cầu hợp đồng.");

            return await _lockService.GetWithLockAsync($"contract_request_create_{emp.Id}", async (innerCt) =>
            {
                int contractId = 0;
                int? reviewerAccountId = null;

                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var targetRoleName = await _approvalConflictGuard.GetEmployeeRoleNameAsync(emp.Id, innerCt);
                    var skipsDepartmentStep =
                        IsHr(targetRoleName) ||
                        IsManager(targetRoleName);

                    var managerAccountIds = skipsDepartmentStep
                        ? new List<int>()
                        : await _accountRepo.GetAccountIdsByRoleAsync("Manager", innerCt);

                    var managerEmployee = skipsDepartmentStep ? null : (await _employeeRepo.FindAsync(
                        e => e.DeptId == emp.DeptId.Value &&
                             e.AccountId.HasValue &&
                             managerAccountIds.Contains(e.AccountId.Value), innerCt)).FirstOrDefault();

                    if (!skipsDepartmentStep && managerEmployee?.AccountId == null)
                        throw new ArgumentException("Phòng ban của bạn hiện chưa có Trưởng phòng để duyệt hợp đồng.");

                    var contract = new Core.Entities.EmployeeProfile.Contract
                    {
                        EmployeeId = emp.Id,
                        ContractNumber = $"TEMP-{DateTime.UtcNow:yyyyMMddHHmmss}",
                        Status = skipsDepartmentStep ? ContractStatus.PendingHR : ContractStatus.PendingDept,
                        NegotiationNote = dto.Note
                    };

                    await _contractRepo.AddAsync(contract, innerCt);
                    await _auditLogRepo.LogSystemEventAsync("CONTRACT_REQUESTED", accountId, "contract", "Gửi yêu cầu hợp đồng mới");
                    await _unitOfWork.CommitAsync(innerCt);

                    contractId = contract.Id;
                    reviewerAccountId = skipsDepartmentStep ? null : managerEmployee!.AccountId!.Value;
                }, innerCt);

                if (reviewerAccountId.HasValue)
                    await _approvalService.CreateWorkflowAsync("CONTRACT_DEPT", contractId, new List<int> { reviewerAccountId.Value }, innerCt);
                await _idempotencyService.SaveAsync("CONTRACT_REQUEST_CREATE", idempotencyKey ?? string.Empty, "Contract", contractId, accountId, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
                return contractId;
            }, cancellationToken: ct);
        }

        public async Task DeptReviewAsync(int contractId, int approverAccountId, string actorRoleName, ReviewContractDto dto, CancellationToken ct)
        {
            await _lockService.GetWithLockAsync($"contract_{contractId}", async (innerCt) =>
            {
                var contract = await _contractRepo.GetByIdAsync(contractId, innerCt);
                if (contract == null || contract.Status != ContractStatus.PendingDept)
                    throw new InvalidOperationException("Hợp đồng không hợp lệ hoặc không ở trạng thái chờ Trưởng phòng.");
                if (!contract.EmployeeId.HasValue)
                    throw new InvalidOperationException("Hợp đồng chưa gắn nhân viên.");

                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(contract.EmployeeId.Value, approverAccountId, innerCt);

                var note = dto.IsApproved
                    ? "Trưởng phòng xác nhận yêu cầu hợp đồng."
                    : string.IsNullOrWhiteSpace(dto.RejectReason)
                        ? "Trưởng phòng từ chối yêu cầu hợp đồng."
                        : dto.RejectReason;

                await _approvalService.ProcessStepAsync(
                    "CONTRACT_DEPT",
                    contractId,
                    approverAccountId,
                    actorRoleName,
                    dto.IsApproved,
                    note,
                    innerCt);
                return true;
            }, cancellationToken: ct);
        }

        public async Task HrCreateDraftAsync(int contractId, int actorAccountId, string actorRoleName, CreateDraftDto dto, CancellationToken ct)
        {
            await _lockService.GetWithLockAsync($"contract_{contractId}", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var contract = await _contractRepo.GetByIdAsync(contractId, innerCt);
                    if (contract == null ||
                        (contract.Status != ContractStatus.PendingHR && contract.Status != ContractStatus.Negotiating))
                        throw new InvalidOperationException("Hợp đồng không hợp lệ hoặc không ở trạng thái HR có thể soạn thảo.");
                    if (!contract.EmployeeId.HasValue)
                        throw new InvalidOperationException("Hợp đồng chưa gắn nhân viên.");

                    EnsureHrDirectorOrAdmin(actorRoleName);
                    if (!IsDirector(actorRoleName) && !IsAdmin(actorRoleName))
                        await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(contract.EmployeeId.Value, actorAccountId, innerCt);

                    bool isNegotiationUpdate = contract.Status == ContractStatus.Negotiating;
                    ApplyDraft(contract, dto, isNegotiationUpdate);

                    await _contractRepo.UpdateAsync(contract, innerCt);
                    await _slaTrackingService.CreateTaskAsync(SlaModuleType.ContractRenewal, contract.Id, innerCt);
                    await _auditLogRepo.LogSystemEventAsync(
                        isNegotiationUpdate ? "CONTRACT_DRAFT_UPDATED" : "CONTRACT_DRAFT_CREATED",
                        actorAccountId,
                        "contract",
                        $"HR {(isNegotiationUpdate ? "cập nhật" : "tạo")} bản nháp hợp đồng ID {contractId}, phiên bản v{contract.Version}");
                    await _unitOfWork.CommitAsync(innerCt);
                }, innerCt);
                return true;
            }, cancellationToken: ct);

        }

        public async Task HrRejectAsync(int contractId, int actorAccountId, string actorRoleName, string reason, CancellationToken ct)
        {
            await _lockService.GetWithLockAsync($"contract_{contractId}", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var contract = await _contractRepo.GetByIdAsync(contractId, innerCt);
                    if (contract == null ||
                        (contract.Status != ContractStatus.PendingHR && contract.Status != ContractStatus.Negotiating))
                        throw new InvalidOperationException("Hợp đồng không hợp lệ hoặc không ở trạng thái chờ HR.");
                    if (!contract.EmployeeId.HasValue)
                        throw new InvalidOperationException("Hợp đồng chưa gắn nhân viên.");

                    EnsureHrDirectorOrAdmin(actorRoleName);
                    if (!IsDirector(actorRoleName) && !IsAdmin(actorRoleName))
                        await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(contract.EmployeeId.Value, actorAccountId, innerCt);

                    contract.Status = ContractStatus.Rejected;
                    contract.NegotiationNote = reason;

                    await _contractRepo.UpdateAsync(contract, innerCt);
                    await _slaTrackingService.ResolveTaskAsync(SlaModuleType.ContractRenewal, contractId, innerCt);
                    await _auditLogRepo.LogSystemEventAsync("CONTRACT_HR_REJECTED", actorAccountId, "contract", $"HR từ chối hợp đồng ID {contractId}: {reason}");
                    await _unitOfWork.CommitAsync(innerCt);
                }, innerCt);
                return true;
            }, cancellationToken: ct);

            await _mediator.Publish(new ContractFlowCompletedEvent
            {
                ContractId = contractId,
                Status = "Rejected",
                Note = reason
            }, ct);
        }

        public async Task NegotiateAsync(int contractId, int actorAccountId, NegotiateDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.NegotiationNote))
                throw new ArgumentException("Nội dung thương lượng không được để trống.");

            await _lockService.GetWithLockAsync($"contract_{contractId}", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var contract = await _contractRepo.GetByIdAsync(contractId, innerCt);
                    if (contract == null || contract.Status != ContractStatus.Draft)
                        throw new InvalidOperationException("Không thể thương lượng lúc này.");
                    await EnsureEmployeeOwnsContractAsync(contract, actorAccountId, innerCt);

                    contract.Status = ContractStatus.Negotiating;
                    contract.NegotiationNote = dto.NegotiationNote.Trim();

                    await _contractRepo.UpdateAsync(contract, innerCt);
                    await _slaTrackingService.ResolveTaskAsync(SlaModuleType.ContractRenewal, contractId, innerCt);
                    await _auditLogRepo.LogSystemEventAsync("CONTRACT_NEGOTIATED", actorAccountId, "contract", $"Nhân viên yêu cầu điều chỉnh hợp đồng ID {contractId}");
                    await _unitOfWork.CommitAsync(innerCt);
                }, innerCt);
                return true;
            }, cancellationToken: ct);

            await _mediator.Publish(new ContractFlowCompletedEvent
            {
                ContractId = contractId,
                Status = "Negotiating",
                Note = dto.NegotiationNote.Trim()
            }, ct);
        }

        public async Task EmployeeAcceptAsync(int contractId, int actorAccountId, CancellationToken ct)
        {
            int directorId = 0;

            await _lockService.GetWithLockAsync($"contract_{contractId}", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var contract = await _contractRepo.GetByIdAsync(contractId, innerCt);
                    if (contract == null || contract.Status != ContractStatus.Draft)
                        throw new InvalidOperationException("Hợp đồng không hợp lệ.");
                    await EnsureEmployeeOwnsContractAsync(contract, actorAccountId, innerCt);

                    var directorIds = await _accountRepo.GetAccountIdsByRoleAsync("Director", innerCt);
                    directorId = directorIds.FirstOrDefault();
                    if (directorId == 0)
                        throw new InvalidOperationException("Hệ thống chưa có Giám đốc để duyệt hợp đồng.");

                    contract.Status = ContractStatus.PendingDirector;

                    await _contractRepo.UpdateAsync(contract, innerCt);
                    await _slaTrackingService.ResolveTaskAsync(SlaModuleType.ContractRenewal, contractId, innerCt);
                    await _slaTrackingService.CreateTaskAsync(SlaModuleType.DirectorContractApproval, contract.Id, innerCt);
                    await _auditLogRepo.LogSystemEventAsync("CONTRACT_ACCEPTED", actorAccountId, "contract", $"Nhân viên đồng ý bản nháp hợp đồng ID {contractId}");
                    await _unitOfWork.CommitAsync(innerCt);
                }, innerCt);
                return true;
            }, cancellationToken: ct);

            await _approvalService.CreateWorkflowAsync("CONTRACT_DIRECTOR", contractId, new List<int> { directorId }, ct);
        }

        public async Task DirectorReviewAsync(int contractId, int approverAccountId, string actorRoleName, ReviewContractDto dto, CancellationToken ct)
        {
            await _lockService.GetWithLockAsync($"contract_{contractId}", async (innerCt) =>
            {
                var contract = await _contractRepo.GetByIdAsync(contractId, innerCt);
                if (contract == null || contract.Status != ContractStatus.PendingDirector)
                    throw new InvalidOperationException("Hợp đồng không hợp lệ hoặc không ở trạng thái chờ Giám đốc.");
                if (!contract.EmployeeId.HasValue)
                    throw new InvalidOperationException("Hợp đồng chưa gắn nhân viên.");

                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(contract.EmployeeId.Value, approverAccountId, innerCt);

                var note = dto.IsApproved
                    ? "Giám đốc phê duyệt hợp đồng."
                    : string.IsNullOrWhiteSpace(dto.RejectReason)
                        ? "Giám đốc từ chối phê duyệt hợp đồng."
                        : dto.RejectReason;

                await _approvalService.ProcessStepAsync(
                    "CONTRACT_DIRECTOR",
                    contractId,
                    approverAccountId,
                    actorRoleName,
                    dto.IsApproved,
                    note,
                    innerCt);
                return true;
            }, cancellationToken: ct);
        }

        public async Task<IEnumerable<ContractResponseDto>> GetMyContractsAsync(int accountId, CancellationToken ct)
        {
            var emp = await _employeeRepo.GetByAccountIdAsync(accountId, ct);
            if (emp == null) return Enumerable.Empty<ContractResponseDto>();

            var contracts = await _contractRepo.GetByEmployeeIdAsync(emp.Id, ct);
            return contracts.Select(MapToDto);
        }

        public async Task<IEnumerable<ContractResponseDto>> GetAllContractsAsync(CancellationToken ct)
        {
            var contracts = await _contractRepo.GetAllWithEmployeeAsync(ct);
            return contracts.Select(MapToDto);
        }

        public async Task<IEnumerable<ContractResponseDto>> GetPendingDeptAsync(CancellationToken ct)
        {
            var contracts = await _contractRepo.GetByStatusAsync(ContractStatus.PendingDept, ct);
            return contracts.Select(MapToDto);
        }

        public async Task<IEnumerable<ContractResponseDto>> GetPendingHRAsync(CancellationToken ct)
        {
            var contracts = await _contractRepo.GetByStatusesAsync(
                new[] { ContractStatus.PendingHR, ContractStatus.Negotiating }, ct);
            return contracts.Select(MapToDto);
        }

        public async Task<IEnumerable<ContractResponseDto>> GetPendingDirectorAsync(CancellationToken ct)
        {
            var contracts = await _contractRepo.GetByStatusAsync(ContractStatus.PendingDirector, ct);
            return contracts.Select(MapToDto);
        }

        private static void ApplyDraft(Core.Entities.EmployeeProfile.Contract contract, CreateDraftDto dto, bool incrementVersion)
        {
            contract.ContractNumber = string.IsNullOrWhiteSpace(contract.ContractNumber) ||
                                      contract.ContractNumber.StartsWith("TEMP-", StringComparison.OrdinalIgnoreCase)
                ? $"HD-{DateTime.UtcNow.Year}-{contract.Id:D4}"
                : contract.ContractNumber;
            contract.ContractType = ParseContractType(dto.ContractType);
            contract.BasicSalary = dto.BasicSalary;
            contract.SalaryPercentage = dto.SalaryPercentage;
            contract.InsuranceSalary = dto.InsuranceSalary;
            contract.StartDate = dto.StartDate;
            contract.EndDate = dto.EndDate;
            contract.Version = incrementVersion ? contract.Version + 1 : Math.Max(contract.Version, 1);
            contract.Status = ContractStatus.Draft;
        }

        private static ContractType ParseContractType(string contractType)
        {
            if (string.Equals(contractType, "FixedTerm", StringComparison.OrdinalIgnoreCase))
                return ContractType.Definite;
            if (Enum.TryParse<ContractType>(contractType, true, out var parsed))
                return parsed;
            throw new ArgumentException("Loại hợp đồng không hợp lệ.");
        }

        private static string ToContractTypeDto(ContractType contractType) =>
            contractType == ContractType.Definite ? "FixedTerm" : contractType.ToString();

        private async Task EnsureEmployeeOwnsContractAsync(Core.Entities.EmployeeProfile.Contract contract, int actorAccountId, CancellationToken ct)
        {
            if (!contract.EmployeeId.HasValue)
                throw new InvalidOperationException("Hợp đồng chưa gắn nhân viên.");

            var employee = await _employeeRepo.GetProfileByIdAsync(contract.EmployeeId.Value, ct)
                ?? throw new InvalidOperationException("Không tìm thấy nhân viên của hợp đồng.");

            if (!employee.AccountId.HasValue || employee.AccountId.Value != actorAccountId)
                throw new UnauthorizedAccessException("Chỉ người lao động của hợp đồng mới được xác nhận điều khoản.");
        }

        private static void EnsureHrDirectorOrAdmin(string actorRoleName)
        {
            if (!IsHr(actorRoleName) && !IsDirector(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chỉ HR, Giám đốc hoặc Admin được xử lý bản nháp hợp đồng.");
        }

        private static bool IsHr(string? role) => string.Equals(role, "HR", StringComparison.OrdinalIgnoreCase);
        private static bool IsManager(string? role) => string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase);
        private static bool IsDirector(string? role) => string.Equals(role, "Director", StringComparison.OrdinalIgnoreCase);
        private static bool IsAdmin(string? role) => string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

        private static ContractResponseDto MapToDto(Core.Entities.EmployeeProfile.Contract c) => new()
        {
            Id = c.Id,
            ContractNumber = c.ContractNumber,
            ContractType = ToContractTypeDto(c.ContractType),
            BasicSalary = c.BasicSalary,
            SalaryPercentage = c.SalaryPercentage,
            InsuranceSalary = c.InsuranceSalary,
            StartDate = c.StartDate == default ? null : c.StartDate,
            EndDate = c.EndDate,
            Status = c.Status.ToString(),
            Version = c.Version,
            NegotiationNote = c.NegotiationNote,
            EmployeeId = c.EmployeeId,
            EmployeeName = c.Employee?.FullName
        };
    }
}
