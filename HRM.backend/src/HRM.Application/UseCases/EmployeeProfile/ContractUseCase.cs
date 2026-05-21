using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;
using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases;
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
        private readonly IAccountRepository _accountRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;

        public ContractUseCase(
            IContractRepository contractRepo,
            IEmployeeRepository employeeRepo,
            IAuditLogRepository auditLogRepo,
            ISlaTrackingService slaTrackingService,
            IApprovalWorkflowService approvalService,
            IAccountRepository accountRepo,
            IUnitOfWork unitOfWork,
            IMediator mediator)
        {
            _contractRepo = contractRepo;
            _employeeRepo = employeeRepo;
            _auditLogRepo = auditLogRepo;
            _slaTrackingService = slaTrackingService;
            _approvalService = approvalService;
            _accountRepo = accountRepo;
            _unitOfWork = unitOfWork;
            _mediator = mediator;
        }

        public async Task CreateRequestAsync(int accountId, ContractRequestDto dto, CancellationToken ct)
        {
            int contractId = 0;
            int managerAccountId = 0;

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var emp = await _employeeRepo.GetByAccountIdAsync(accountId, ct);
                if (emp == null) throw new ArgumentException("Không tìm thấy hồ sơ nhân viên.");
                if (!emp.DeptId.HasValue) throw new ArgumentException("Nhân viên chưa được phân phòng ban, không thể yêu cầu.");

                var managerAccountIds = await _accountRepo.GetAccountIdsByRoleAsync("Manager", ct);
                var managerEmployee = (await _employeeRepo.FindAsync(
                    e => e.DeptId == emp.DeptId.Value &&
                         e.AccountId.HasValue &&
                         managerAccountIds.Contains(e.AccountId.Value), ct)).FirstOrDefault();

                if (managerEmployee?.AccountId == null)
                    throw new ArgumentException("Phòng ban của bạn hiện chưa có Trưởng phòng để duyệt hợp đồng.");

                var contract = new Core.Entities.EmployeeProfile.Contract
                {
                    EmployeeId = emp.Id,
                    ContractNumber = $"TEMP-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    Status = ContractStatus.PendingDept,
                    NegotiationNote = dto.Note
                };

                await _contractRepo.AddAsync(contract, ct);
                await _auditLogRepo.LogSystemEventAsync("CONTRACT_REQUESTED", accountId, "contract", "Gửi yêu cầu hợp đồng mới");
                await _unitOfWork.CommitAsync(ct);

                contractId = contract.Id;
                managerAccountId = managerEmployee.AccountId.Value;
            }, ct);

            await _approvalService.CreateWorkflowAsync("CONTRACT_DEPT", contractId, new List<int> { managerAccountId }, ct);
        }

        public async Task DeptReviewAsync(int contractId, ReviewContractDto dto, CancellationToken ct)
        {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var contract = await _contractRepo.GetByIdAsync(contractId, ct);
                if (contract == null || contract.Status != ContractStatus.PendingDept)
                    throw new InvalidOperationException("Hợp đồng không hợp lệ hoặc không ở trạng thái chờ Trưởng phòng.");

                contract.Status = dto.IsApproved ? ContractStatus.PendingHR : ContractStatus.Rejected;
                if (!dto.IsApproved)
                {
                    contract.NegotiationNote = string.IsNullOrWhiteSpace(dto.RejectReason)
                        ? "Trưởng phòng từ chối yêu cầu."
                        : dto.RejectReason;
                }

                await _contractRepo.UpdateAsync(contract, ct);
                await _auditLogRepo.LogSystemEventAsync(
                    dto.IsApproved ? "CONTRACT_DEPT_APPROVED" : "CONTRACT_DEPT_REJECTED",
                    0,
                    "contract",
                    dto.IsApproved
                        ? $"Trưởng phòng chuyển hợp đồng ID {contractId} sang HR."
                        : $"Trưởng phòng từ chối hợp đồng ID {contractId}: {contract.NegotiationNote}");
                await _unitOfWork.CommitAsync(ct);
            }, ct);
        }

        public async Task HrCreateDraftAsync(int contractId, CreateDraftDto dto, CancellationToken ct)
        {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var contract = await _contractRepo.GetByIdAsync(contractId, ct);
                if (contract == null ||
                    (contract.Status != ContractStatus.PendingHR && contract.Status != ContractStatus.Negotiating))
                    throw new InvalidOperationException("Hợp đồng không hợp lệ hoặc không ở trạng thái HR có thể soạn thảo.");

                bool isNegotiationUpdate = contract.Status == ContractStatus.Negotiating;
                ApplyDraft(contract, dto, isNegotiationUpdate);

                await _contractRepo.UpdateAsync(contract, ct);
                await _slaTrackingService.CreateTaskAsync(SlaModuleType.ContractRenewal, contract.Id, ct);
                await _auditLogRepo.LogSystemEventAsync(
                    isNegotiationUpdate ? "CONTRACT_DRAFT_UPDATED" : "CONTRACT_DRAFT_CREATED",
                    0,
                    "contract",
                    $"HR {(isNegotiationUpdate ? "cập nhật" : "tạo")} bản nháp hợp đồng ID {contractId}, phiên bản v{contract.Version}");
                await _unitOfWork.CommitAsync(ct);
            }, ct);
        }

        public async Task HrRejectAsync(int contractId, string reason, CancellationToken ct)
        {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var contract = await _contractRepo.GetByIdAsync(contractId, ct);
                if (contract == null ||
                    (contract.Status != ContractStatus.PendingHR && contract.Status != ContractStatus.Negotiating))
                    throw new InvalidOperationException("Hợp đồng không hợp lệ hoặc không ở trạng thái chờ HR.");

                contract.Status = ContractStatus.Rejected;
                contract.NegotiationNote = reason;

                await _contractRepo.UpdateAsync(contract, ct);
                await _slaTrackingService.ResolveTaskAsync(SlaModuleType.ContractRenewal, contractId, ct);
                await _auditLogRepo.LogSystemEventAsync("CONTRACT_HR_REJECTED", 0, "contract", $"HR từ chối hợp đồng ID {contractId}: {reason}");
                await _unitOfWork.CommitAsync(ct);
            }, ct);
        }

        public async Task NegotiateAsync(int contractId, NegotiateDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.NegotiationNote))
                throw new ArgumentException("Nội dung thương lượng không được để trống.");

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var contract = await _contractRepo.GetByIdAsync(contractId, ct);
                if (contract == null || contract.Status != ContractStatus.Draft)
                    throw new InvalidOperationException("Không thể thương lượng lúc này.");

                contract.Status = ContractStatus.Negotiating;
                contract.NegotiationNote = dto.NegotiationNote.Trim();

                await _contractRepo.UpdateAsync(contract, ct);
                await _slaTrackingService.ResolveTaskAsync(SlaModuleType.ContractRenewal, contractId, ct);
                await _auditLogRepo.LogSystemEventAsync("CONTRACT_NEGOTIATED", 0, "contract", $"Nhân viên yêu cầu điều chỉnh hợp đồng ID {contractId}");
                await _unitOfWork.CommitAsync(ct);
            }, ct);
        }

        public async Task EmployeeAcceptAsync(int contractId, CancellationToken ct)
        {
            int directorId = 0;

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var contract = await _contractRepo.GetByIdAsync(contractId, ct);
                if (contract == null || contract.Status != ContractStatus.Draft)
                    throw new InvalidOperationException("Hợp đồng không hợp lệ.");

                var directorIds = await _accountRepo.GetAccountIdsByRoleAsync("Director", ct);
                directorId = directorIds.FirstOrDefault();
                if (directorId == 0) throw new InvalidOperationException("Hệ thống chưa có Giám đốc để duyệt hợp đồng.");

                contract.Status = ContractStatus.PendingDirector;

                await _contractRepo.UpdateAsync(contract, ct);
                await _slaTrackingService.ResolveTaskAsync(SlaModuleType.ContractRenewal, contractId, ct);
                await _slaTrackingService.CreateTaskAsync(SlaModuleType.DirectorContractApproval, contract.Id, ct);
                await _auditLogRepo.LogSystemEventAsync("CONTRACT_ACCEPTED", 0, "contract", $"Nhân viên đồng ý bản nháp hợp đồng ID {contractId}");
                await _unitOfWork.CommitAsync(ct);
            }, ct);

            await _approvalService.CreateWorkflowAsync("CONTRACT_DIRECTOR", contractId, new List<int> { directorId }, ct);
        }

        public async Task DirectorReviewAsync(int contractId, ReviewContractDto dto, CancellationToken ct)
        {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var contract = await _contractRepo.GetByIdAsync(contractId, ct);
                if (contract == null || contract.Status != ContractStatus.PendingDirector)
                    throw new InvalidOperationException("Hợp đồng không hợp lệ hoặc không ở trạng thái chờ Giám đốc.");
                if (!contract.EmployeeId.HasValue)
                    throw new InvalidOperationException("Hợp đồng chưa gắn với nhân viên.");

                if (dto.IsApproved)
                {
                    contract.Status = ContractStatus.Active;
                    await _contractRepo.UpdateAsync(contract, ct);
                    await _mediator.Publish(new ContractActivatedEvent
                    {
                        ContractId = contract.Id,
                        EmployeeId = contract.EmployeeId.Value,
                        BasicSalary = contract.BasicSalary,
                        StartDate = contract.StartDate
                    }, ct);
                }
                else
                {
                    contract.Status = ContractStatus.Rejected;
                    contract.NegotiationNote = string.IsNullOrWhiteSpace(dto.RejectReason)
                        ? "Giám đốc từ chối phê duyệt."
                        : dto.RejectReason;
                    await _contractRepo.UpdateAsync(contract, ct);
                }

                await _slaTrackingService.ResolveTaskAsync(SlaModuleType.DirectorContractApproval, contractId, ct);
                await _auditLogRepo.LogSystemEventAsync(
                    dto.IsApproved ? "CONTRACT_DIRECTOR_APPROVED" : "CONTRACT_DIRECTOR_REJECTED",
                    0,
                    "contract",
                    dto.IsApproved
                        ? $"Giám đốc phê duyệt hợp đồng ID {contractId}."
                        : $"Giám đốc từ chối hợp đồng ID {contractId}: {contract.NegotiationNote}");
                await _unitOfWork.CommitAsync(ct);
            }, ct);
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
