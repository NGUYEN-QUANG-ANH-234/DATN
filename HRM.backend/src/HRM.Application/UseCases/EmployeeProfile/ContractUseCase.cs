using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System.HRM.backend.src.HRM.Infrastructure.Repositories.Interfaces.System;
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
                throw new ArgumentException("Khong tim thay ho so nhan vien.");
            if (!emp.DeptId.HasValue)
                throw new ArgumentException("Nhan vien chua duoc phan phong ban, khong the yeu cau hop dong.");

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
                        throw new ArgumentException("Phong ban cua ban hien chua co Truong phong de duyet hop dong.");

                    var contract = new Core.Entities.EmployeeProfile.Contract
                    {
                        EmployeeId = emp.Id,
                        ContractNumber = $"TEMP-{DateTime.UtcNow:yyyyMMddHHmmss}",
                        Status = skipsDepartmentStep ? ContractStatus.PendingHR : ContractStatus.PendingDept,
                        NegotiationNote = dto.Note
                    };

                    await _contractRepo.AddAsync(contract, innerCt);
                    await _auditLogRepo.LogSystemEventAsync("CONTRACT_REQUESTED", accountId, "contract", "Gui yeu cau hop dong moi");
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
                    throw new InvalidOperationException("Hop dong khong hop le hoac khong o trang thai cho Truong phong.");
                if (!contract.EmployeeId.HasValue)
                    throw new InvalidOperationException("Hop dong chua gan nhan vien.");

                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(contract.EmployeeId.Value, approverAccountId, innerCt);

                var note = dto.IsApproved
                    ? "Truong phong xac nhan yeu cau hop dong."
                    : string.IsNullOrWhiteSpace(dto.RejectReason)
                        ? "Truong phong tu choi yeu cau hop dong."
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
                        throw new InvalidOperationException("Hop dong khong hop le hoac khong o trang thai HR co the soan thao.");
                    if (!contract.EmployeeId.HasValue)
                        throw new InvalidOperationException("Hop dong chua gan nhan vien.");

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
                        $"HR {(isNegotiationUpdate ? "cap nhat" : "tao")} ban nhap hop dong ID {contractId}, phien ban v{contract.Version}");
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
                        throw new InvalidOperationException("Hop dong khong hop le hoac khong o trang thai cho HR.");
                    if (!contract.EmployeeId.HasValue)
                        throw new InvalidOperationException("Hop dong chua gan nhan vien.");

                    EnsureHrDirectorOrAdmin(actorRoleName);
                    if (!IsDirector(actorRoleName) && !IsAdmin(actorRoleName))
                        await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(contract.EmployeeId.Value, actorAccountId, innerCt);

                    contract.Status = ContractStatus.Rejected;
                    contract.NegotiationNote = reason;

                    await _contractRepo.UpdateAsync(contract, innerCt);
                    await _slaTrackingService.ResolveTaskAsync(SlaModuleType.ContractRenewal, contractId, innerCt);
                    await _auditLogRepo.LogSystemEventAsync("CONTRACT_HR_REJECTED", actorAccountId, "contract", $"HR tu choi hop dong ID {contractId}: {reason}");
                    await _unitOfWork.CommitAsync(innerCt);
                }, innerCt);
                return true;
            }, cancellationToken: ct);
        }

        public async Task NegotiateAsync(int contractId, int actorAccountId, NegotiateDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.NegotiationNote))
                throw new ArgumentException("Noi dung thuong luong khong duoc de trong.");

            await _lockService.GetWithLockAsync($"contract_{contractId}", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var contract = await _contractRepo.GetByIdAsync(contractId, innerCt);
                    if (contract == null || contract.Status != ContractStatus.Draft)
                        throw new InvalidOperationException("Khong the thuong luong luc nay.");
                    await EnsureEmployeeOwnsContractAsync(contract, actorAccountId, innerCt);

                    contract.Status = ContractStatus.Negotiating;
                    contract.NegotiationNote = dto.NegotiationNote.Trim();

                    await _contractRepo.UpdateAsync(contract, innerCt);
                    await _slaTrackingService.ResolveTaskAsync(SlaModuleType.ContractRenewal, contractId, innerCt);
                    await _auditLogRepo.LogSystemEventAsync("CONTRACT_NEGOTIATED", actorAccountId, "contract", $"Nhan vien yeu cau dieu chinh hop dong ID {contractId}");
                    await _unitOfWork.CommitAsync(innerCt);
                }, innerCt);
                return true;
            }, cancellationToken: ct);
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
                        throw new InvalidOperationException("Hop dong khong hop le.");
                    await EnsureEmployeeOwnsContractAsync(contract, actorAccountId, innerCt);

                    var directorIds = await _accountRepo.GetAccountIdsByRoleAsync("Director", innerCt);
                    directorId = directorIds.FirstOrDefault();
                    if (directorId == 0)
                        throw new InvalidOperationException("He thong chua co Giam doc de duyet hop dong.");

                    contract.Status = ContractStatus.PendingDirector;

                    await _contractRepo.UpdateAsync(contract, innerCt);
                    await _slaTrackingService.ResolveTaskAsync(SlaModuleType.ContractRenewal, contractId, innerCt);
                    await _slaTrackingService.CreateTaskAsync(SlaModuleType.DirectorContractApproval, contract.Id, innerCt);
                    await _auditLogRepo.LogSystemEventAsync("CONTRACT_ACCEPTED", actorAccountId, "contract", $"Nhan vien dong y ban nhap hop dong ID {contractId}");
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
                    throw new InvalidOperationException("Hop dong khong hop le hoac khong o trang thai cho Giam doc.");
                if (!contract.EmployeeId.HasValue)
                    throw new InvalidOperationException("Hop dong chua gan nhan vien.");

                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(contract.EmployeeId.Value, approverAccountId, innerCt);

                var note = dto.IsApproved
                    ? "Giam doc phe duyet hop dong."
                    : string.IsNullOrWhiteSpace(dto.RejectReason)
                        ? "Giam doc tu choi phe duyet hop dong."
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
            throw new ArgumentException("Loai hop dong khong hop le.");
        }

        private static string ToContractTypeDto(ContractType contractType) =>
            contractType == ContractType.Definite ? "FixedTerm" : contractType.ToString();

        private async Task EnsureEmployeeOwnsContractAsync(Core.Entities.EmployeeProfile.Contract contract, int actorAccountId, CancellationToken ct)
        {
            if (!contract.EmployeeId.HasValue)
                throw new InvalidOperationException("Hop dong chua gan nhan vien.");

            var employee = await _employeeRepo.GetProfileByIdAsync(contract.EmployeeId.Value, ct)
                ?? throw new InvalidOperationException("Khong tim thay nhan vien cua hop dong.");

            if (!employee.AccountId.HasValue || employee.AccountId.Value != actorAccountId)
                throw new UnauthorizedAccessException("Chi nguoi lao dong cua hop dong moi duoc xac nhan dieu khoan.");
        }

        private static void EnsureHrDirectorOrAdmin(string actorRoleName)
        {
            if (!IsHr(actorRoleName) && !IsDirector(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chi HR, Giam doc hoac Admin duoc xu ly ban nhap hop dong.");
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
