using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;
using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PayrollAllowances;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;
using MediatR;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace HRM.backend.src.HRM.Application.UseCases.EmployeeProfile
{
    public class ContractUseCase : IContractUseCase
    {
        private const string DefaultKpiBonusPolicyCode = "HICAS_KPI_BONUS_2026";
        private const string DefaultBonusPolicyText = "Các khoản thưởng, phụ cấp và thu nhập biến động khác áp dụng theo quy chế lương thưởng hiện hành của công ty.";
        private const string DefaultKpiScoreFormula = "Điểm KPI chính thức do trưởng phòng chốt theo công thức: tổng điểm = tổng max(0, trọng số KPI * điểm trưởng phòng / 100 - điểm trừ).";
        private const string DefaultKpiPayoutFormula = "Thưởng KPI thực nhận = mức thưởng KPI tối đa * điểm KPI / 100.";
        private const string DefaultKpiEligibilityRule = "Người lao động chỉ nhận thưởng KPI khi KPI kỳ đó đã được chốt, không thuộc trường hợp bị hủy/không áp dụng theo quy chế lương thưởng và các quyết định kỷ luật liên quan.";
        private const string DefaultKpiPaymentPeriod = "Chi trả theo kỳ lương sau khi kết quả KPI được chốt và bảng lương được phê duyệt.";
        private const string DefaultKpiApproverRole = "Trưởng phòng chốt điểm KPI; HR kiểm tra chính sách; Giám đốc phê duyệt bảng lương.";

        private readonly IContractRepository _contractRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly ISlaTrackingService _slaTrackingService;
        private readonly IApprovalWorkflowService _approvalService;
        private readonly IApprovalConflictGuard _approvalConflictGuard;
        private readonly IAccountRepository _accountRepo;
        private readonly IConfigurationRepository _configurationRepo;
        private readonly IWorkShiftRepository _workShiftRepo;
        private readonly IWorkCalendarConfigRepository _workCalendarConfigRepo;
        private readonly IPayrollRepository _payrollRepo;
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
            IConfigurationRepository configurationRepo,
            IWorkShiftRepository workShiftRepo,
            IWorkCalendarConfigRepository workCalendarConfigRepo,
            IPayrollRepository payrollRepo,
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
            _configurationRepo = configurationRepo;
            _workShiftRepo = workShiftRepo;
            _workCalendarConfigRepo = workCalendarConfigRepo;
            _payrollRepo = payrollRepo;
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
                if (contract == null ||
                    (contract.Status != ContractStatus.PendingDept &&
                     contract.Status != ContractStatus.PendingManagerContentReview))
                    throw new InvalidOperationException("Hợp đồng không hợp lệ hoặc không ở trạng thái chờ Trưởng phòng.");
                if (!contract.EmployeeId.HasValue)
                    throw new InvalidOperationException("Hợp đồng chưa gắn nhân viên.");

                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(contract.EmployeeId.Value, approverAccountId, innerCt);

                var isContentRevision = contract.Status == ContractStatus.PendingManagerContentReview && !dto.IsApproved;
                var note = dto.IsApproved
                    ? "Trưởng phòng xác nhận yêu cầu hợp đồng."
                    : string.IsNullOrWhiteSpace(dto.RejectReason)
                        ? "Trưởng phòng từ chối yêu cầu hợp đồng."
                        : dto.RejectReason;
                if (isContentRevision && string.IsNullOrWhiteSpace(dto.RejectReason))
                    note = "Truong phong yeu cau HR chinh sua noi dung hop dong.";

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
            int? departmentReviewerAccountId = null;

            await _lockService.GetWithLockAsync($"contract_{contractId}", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var contract = await _contractRepo.GetByIdAsync(contractId, innerCt);
                    if (contract == null ||
                        (contract.Status != ContractStatus.PendingHR &&
                         contract.Status != ContractStatus.PendingHRRevision &&
                         contract.Status != ContractStatus.Negotiating))
                        throw new InvalidOperationException("Hợp đồng không hợp lệ hoặc không ở trạng thái HR có thể soạn thảo.");
                    if (!contract.EmployeeId.HasValue)
                        throw new InvalidOperationException("Hợp đồng chưa gắn nhân viên.");

                    EnsureHrDirectorOrAdmin(actorRoleName);
                    if (!IsDirector(actorRoleName) && !IsAdmin(actorRoleName))
                        await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(contract.EmployeeId.Value, actorAccountId, innerCt);

                    bool isNegotiationUpdate = contract.Status == ContractStatus.PendingHRRevision ||
                                               contract.Status == ContractStatus.Negotiating;
                    ApplyDraft(contract, dto, isNegotiationUpdate);
                    departmentReviewerAccountId = await ResolveDepartmentReviewerAccountIdAsync(contract, actorAccountId, innerCt);
                    if (!departmentReviewerAccountId.HasValue)
                        throw new InvalidOperationException("Chua co Truong phong phu hop de duyet noi dung hop dong.");
                    contract.Status = ContractStatus.PendingManagerContentReview;
                    ValidateContractCoreRules(contract);
                    await SyncLegalSnapshotAsync(contract, dto, actorAccountId, innerCt);

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

            if (departmentReviewerAccountId.HasValue)
                await _approvalService.CreateWorkflowAsync("CONTRACT_DEPT", contractId, new List<int> { departmentReviewerAccountId.Value }, ct);
        }

        public async Task HrRejectAsync(int contractId, int actorAccountId, string actorRoleName, string reason, CancellationToken ct)
        {
            await _lockService.GetWithLockAsync($"contract_{contractId}", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var contract = await _contractRepo.GetByIdAsync(contractId, innerCt);
                    if (contract == null ||
                        (contract.Status != ContractStatus.PendingHR &&
                         contract.Status != ContractStatus.PendingHRRevision &&
                         contract.Status != ContractStatus.Negotiating))
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
                    if (contract == null ||
                        (contract.Status != ContractStatus.PendingEmployee &&
                         contract.Status != ContractStatus.Draft))
                        throw new InvalidOperationException("Không thể thương lượng lúc này.");
                    await EnsureEmployeeOwnsContractAsync(contract, actorAccountId, innerCt);

                    contract.Status = ContractStatus.PendingHRRevision;
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

        public async Task RequestRevisionAsync(int contractId, int actorAccountId, string actorRoleName, RequestRevisionDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new ArgumentException("Vui long nhap ly do yeu cau chinh sua.");

            var contract = await _contractRepo.GetByIdAsync(contractId, ct)
                ?? throw new InvalidOperationException("Khong tim thay hop dong.");
            var oldStatus = contract.Status;
            var version = contract.Version;
            var reason = dto.Reason.Trim();

            switch (contract.Status)
            {
                case ContractStatus.PendingManagerContentReview:
                    await DeptReviewAsync(contractId, actorAccountId, actorRoleName, new ReviewContractDto
                    {
                        IsApproved = false,
                        RejectReason = reason
                    }, ct);
                    break;

                case ContractStatus.PendingEmployee:
                case ContractStatus.Draft:
                    await NegotiateAsync(contractId, actorAccountId, new NegotiateDto
                    {
                        NegotiationNote = reason
                    }, ct);
                    break;

                case ContractStatus.PendingDirector:
                    await DirectorReviewAsync(contractId, actorAccountId, actorRoleName, new ReviewContractDto
                    {
                        IsApproved = false,
                        RejectReason = reason
                    }, ct);
                    break;

                default:
                    throw new InvalidOperationException("Hop dong khong o trang thai co the yeu cau chinh sua.");
            }

            var updated = await _contractRepo.GetByIdAsync(contractId, ct);
            await _auditLogRepo.LogSystemEventAsync(
                "CONTRACT_REVISION_REQUESTED",
                actorAccountId,
                "contract",
                $"ActorRole={actorRoleName}; ContractId={contractId}; Version={version}; Status={oldStatus}->{updated?.Status}; Reason={reason}; RequestedAt={DateTime.UtcNow:O}");
            await _unitOfWork.CommitAsync(ct);
        }

        public async Task EmployeeAcceptAsync(int contractId, int actorAccountId, CancellationToken ct)
        {
            int directorId = 0;

            await _lockService.GetWithLockAsync($"contract_{contractId}", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var contract = await _contractRepo.GetByIdAsync(contractId, innerCt);
                    if (contract == null ||
                        (contract.Status != ContractStatus.PendingEmployee &&
                         contract.Status != ContractStatus.Draft))
                        throw new InvalidOperationException("Hợp đồng không hợp lệ.");
                    var employeeId = contract.EmployeeId
                        ?? throw new InvalidOperationException("Hop dong chua gan nhan vien.");
                    await EnsureEmployeeOwnsContractAsync(contract, actorAccountId, innerCt);
                    ValidateContractReadyForApproval(contract);
                    await EnsureNoActiveContractOverlapAsync(contract, innerCt);

                    directorId = await _approvalConflictGuard.GetAlternativeDirectorApproverAsync(employeeId, innerCt);
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
                if (dto.IsApproved)
                {
                    ValidateContractReadyForApproval(contract);
                    await EnsureNoActiveContractOverlapAsync(contract, innerCt);
                }

                var note = dto.IsApproved
                    ? "Giám đốc phê duyệt hợp đồng."
                    : string.IsNullOrWhiteSpace(dto.RejectReason)
                        ? "Giám đốc từ chối phê duyệt hợp đồng."
                        : dto.RejectReason;

                if (!dto.IsApproved && string.IsNullOrWhiteSpace(dto.RejectReason))
                    note = "Giam doc yeu cau HR chinh sua hop dong.";

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
            return contracts.Select(c => MapToDto(c));
        }

        public async Task<IEnumerable<ContractResponseDto>> GetAllContractsAsync(CancellationToken ct)
        {
            var contracts = await _contractRepo.GetAllWithEmployeeAsync(ct);
            return contracts.Select(c => MapToDto(c));
        }

        public async Task<ContractResponseDto> GetDraftDefaultsAsync(int contractId, CancellationToken ct)
        {
            var contract = await _contractRepo.GetByIdAsync(contractId, ct)
                ?? throw new InvalidOperationException("Không tìm thấy hợp đồng.");

            var defaultsDto = new CreateDraftDto
            {
                ContractType = ToContractTypeDto(contract.ContractType),
                BasicSalary = contract.BasicSalary,
                SalaryPercentage = contract.SalaryPercentage == default ? 100m : contract.SalaryPercentage,
                InsuranceSalary = contract.InsuranceSalary,
                StartDate = contract.StartDate == default ? DateTime.UtcNow.Date : contract.StartDate,
                EndDate = contract.EndDate
            };

            var previewSnapshot = await BuildLegalSnapshotAsync(contract, defaultsDto, null, ct);
            return MapToDto(contract, previewSnapshot);
        }

        public async Task<ContractDocumentPreviewDto> PreviewDocumentAsync(int contractId, CancellationToken ct)
        {
            var contract = await _contractRepo.GetByIdAsync(contractId, ct)
                ?? throw new InvalidOperationException("Không tìm thấy hợp đồng.");

            return BuildContractDocumentPreview(contract);
        }

        public async Task<ContractDocumentDownloadDto> DownloadDocumentDocAsync(int contractId, CancellationToken ct)
        {
            var preview = await PreviewDocumentAsync(contractId, ct);
            return new ContractDocumentDownloadDto
            {
                FileName = preview.FileName,
                ContentType = "application/msword; charset=utf-8",
                Content = Encoding.UTF8.GetBytes(preview.Html)
            };
        }

        public async Task<ContractDocumentDownloadDto> DownloadDocumentPdfAsync(int contractId, CancellationToken ct)
        {
            var preview = await PreviewDocumentAsync(contractId, ct);
            if (string.IsNullOrWhiteSpace(preview.PdfFilePath) || !File.Exists(preview.PdfFilePath))
                throw new InvalidOperationException("Hợp đồng chưa có file PDF đã phát hành.");

            return new ContractDocumentDownloadDto
            {
                FileName = Path.ChangeExtension(preview.FileName, ".pdf"),
                ContentType = "application/pdf",
                Content = await File.ReadAllBytesAsync(preview.PdfFilePath, ct)
            };
        }

        public async Task<ContractDocumentPreviewDto> IssueDocumentAsync(int contractId, IssueContractDocumentDto dto, int actorAccountId, string actorRoleName, CancellationToken ct)
        {
            Core.Entities.EmployeeProfile.Contract? updated = null;
            bool activated = false;
            int? activatedEmployeeId = null;
            decimal activatedBasicSalary = 0m;
            DateTime activatedStartDate = default;

            await _lockService.GetWithLockAsync($"contract_{contractId}", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var contract = await _contractRepo.GetByIdAsync(contractId, innerCt)
                        ?? throw new InvalidOperationException("Không tìm thấy hợp đồng.");

                    if (contract.Status != ContractStatus.ApprovedByDirector && contract.Status != ContractStatus.Active)
                        throw new InvalidOperationException("Chỉ phát hành hợp đồng sau khi Giám đốc đã phê duyệt.");

                    EnsureHrDirectorOrAdmin(actorRoleName);
                    if (contract.EmployeeId.HasValue && !IsDirector(actorRoleName) && !IsAdmin(actorRoleName))
                        await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(contract.EmployeeId.Value, actorAccountId, innerCt);

                    var snapshot = contract.LegalSnapshots
                        .OrderByDescending(s => s.Version)
                        .ThenByDescending(s => s.CreatedAt)
                        .FirstOrDefault();

                    if (snapshot == null)
                    {
                        var defaultsDto = new CreateDraftDto
                        {
                            ContractType = ToContractTypeDto(contract.ContractType),
                            BasicSalary = contract.BasicSalary,
                            SalaryPercentage = contract.SalaryPercentage == default ? 100m : contract.SalaryPercentage,
                            InsuranceSalary = contract.InsuranceSalary,
                            StartDate = contract.StartDate == default ? DateTime.UtcNow.Date : contract.StartDate,
                            EndDate = contract.EndDate,
                            Issuance = new ContractIssuanceDraftDto
                            {
                                LegalDocumentNumber = dto.LegalDocumentNumber,
                                DocumentTemplateCode = dto.DocumentTemplateCode,
                                IssuedAt = dto.IssuedAt
                            }
                        };

                        snapshot = await BuildLegalSnapshotAsync(contract, defaultsDto, actorAccountId, innerCt);
                        contract.LegalSnapshots.Add(snapshot);
                    }

                    snapshot.LegalDocumentNumber = FirstNonBlank(dto.LegalDocumentNumber, snapshot.LegalDocumentNumber, ResolveContractNumber(contract));
                    snapshot.DocumentTemplateCode = FirstNonBlank(dto.DocumentTemplateCode, snapshot.DocumentTemplateCode, contract.DocumentTemplateCode, ResolveDocumentTemplateCode(contract.ContractType));
                    snapshot.IssuedAt = dto.IssuedAt ?? snapshot.IssuedAt ?? DateTime.UtcNow;
                    snapshot.EmployeeSignedAt = dto.EmployeeSignedAt ?? snapshot.EmployeeSignedAt;
                    snapshot.EmployerSignedAt = dto.EmployerSignedAt ?? snapshot.EmployerSignedAt ?? snapshot.IssuedAt;
                    snapshot.DocumentDocFilePath = $"/contract-documents/contracts/{contract.Id}/{SafeFileName(snapshot.LegalDocumentNumber)}.doc";

                    contract.LegalDocumentNumber = snapshot.LegalDocumentNumber;
                    contract.DocumentTemplateCode = snapshot.DocumentTemplateCode;
                    contract.IssuedAt = snapshot.IssuedAt;

                    ValidateContractReadyForApproval(contract, snapshot);
                    await EnsureNoActiveContractOverlapAsync(contract, innerCt);

                    if (contract.Status == ContractStatus.ApprovedByDirector)
                    {
                        contract.Status = ContractStatus.Active;
                        activated = true;
                        activatedEmployeeId = contract.EmployeeId;
                        activatedBasicSalary = contract.BasicSalary;
                        activatedStartDate = contract.StartDate;
                    }

                    await _contractRepo.UpdateAsync(contract, innerCt);
                    await _auditLogRepo.LogSystemEventAsync("CONTRACT_DOCUMENT_ISSUED", actorAccountId, "contract", $"Phát hành văn bản hợp đồng ID {contractId}.");
                    await _unitOfWork.CommitAsync(innerCt);
                    updated = contract;
                }, innerCt);

                return true;
            }, cancellationToken: ct);

            if (activated && activatedEmployeeId.HasValue)
            {
                await _mediator.Publish(new ContractActivatedEvent
                {
                    ContractId = contractId,
                    EmployeeId = activatedEmployeeId.Value,
                    BasicSalary = activatedBasicSalary,
                    StartDate = activatedStartDate
                }, ct);

                await _mediator.Publish(new ContractFlowCompletedEvent
                {
                    ContractId = contractId,
                    Status = "Completed"
                }, ct);
            }

            return BuildContractDocumentPreview(updated!);
        }

        public async Task<IEnumerable<ContractResponseDto>> GetPendingDeptAsync(CancellationToken ct)
        {
            var contracts = await _contractRepo.GetByStatusesAsync(
                new[] { ContractStatus.PendingDept, ContractStatus.PendingManagerContentReview }, ct);
            return contracts.Select(c => MapToDto(c));
        }

        public async Task<IEnumerable<ContractResponseDto>> GetPendingHRAsync(CancellationToken ct)
        {
            var contracts = await _contractRepo.GetByStatusesAsync(
                new[] { ContractStatus.PendingHR, ContractStatus.PendingHRRevision, ContractStatus.Negotiating }, ct);
            return contracts.Select(c => MapToDto(c));
        }

        public async Task<IEnumerable<ContractResponseDto>> GetPendingDirectorAsync(int actorAccountId, string actorRoleName, CancellationToken ct)
        {
            var contracts = await _contractRepo.GetByStatusAsync(ContractStatus.PendingDirector, ct);
            if (IsDirector(actorRoleName))
            {
                contracts = contracts
                    .Where(c => c.Employee?.AccountId != actorAccountId)
                    .ToList();
            }

            return contracts.Select(c => MapToDto(c));
        }

        private async Task<int?> ResolveDepartmentReviewerAccountIdAsync(
            Core.Entities.EmployeeProfile.Contract contract,
            int actorAccountId,
            CancellationToken ct)
        {
            if (!contract.EmployeeId.HasValue)
                throw new InvalidOperationException("Hop dong chua gan nhan vien.");

            var employee = contract.Employee
                ?? await _employeeRepo.GetByIdAsync(contract.EmployeeId.Value, ct)
                ?? throw new InvalidOperationException("Khong tim thay nhan vien cua hop dong.");

            if (!employee.DeptId.HasValue)
                return null;

            var managerAccountIds = await _accountRepo.GetAccountIdsByRoleAsync("Manager", ct);
            if (managerAccountIds.Count == 0)
                return null;

            var manager = (await _employeeRepo.FindAsync(
                e => e.DeptId == employee.DeptId.Value &&
                     e.AccountId.HasValue &&
                     managerAccountIds.Contains(e.AccountId.Value) &&
                     e.AccountId.Value != actorAccountId &&
                     e.Id != employee.Id,
                ct)).FirstOrDefault();

            return manager?.AccountId;
        }

        private async Task EnsureNoActiveContractOverlapAsync(Core.Entities.EmployeeProfile.Contract contract, CancellationToken ct)
        {
            if (!contract.EmployeeId.HasValue)
                throw new InvalidOperationException("Hợp đồng chưa gắn nhân viên.");

            var activeContracts = await _contractRepo.FindAsync(
                c => c.Id != contract.Id &&
                     c.EmployeeId == contract.EmployeeId.Value &&
                     c.Status == ContractStatus.Active,
                ct);

            var hasOverlap = activeContracts.Any(active =>
                DateRangesOverlap(
                    contract.StartDate.Date,
                    contract.EndDate?.Date,
                    active.StartDate.Date,
                    active.EndDate?.Date));

            if (hasOverlap)
                throw new InvalidOperationException("Nhân viên đã có hợp đồng đang hiệu lực trùng thời gian.");
        }

        private static void ValidateContractReadyForApproval(Core.Entities.EmployeeProfile.Contract contract)
        {
            var snapshot = GetLatestLegalSnapshot(contract);
            ValidateContractReadyForApproval(contract, snapshot);
        }

        private static void ValidateContractReadyForApproval(Core.Entities.EmployeeProfile.Contract contract, ContractLegalSnapshot snapshot)
        {
            ValidateContractCoreRules(contract);

            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(snapshot.EmployeeIdentityNumberSnapshot))
                missing.Add("CCCD/CMND");
            if (string.IsNullOrWhiteSpace(snapshot.EmployeeResidenceAddressSnapshot))
                missing.Add("địa chỉ cư trú");
            if (string.IsNullOrWhiteSpace(FirstNonBlank(snapshot.JobTitle, snapshot.EmployeePositionSnapshot)))
                missing.Add("chức danh/công việc");
            if (contract.BasicSalary <= 0)
                missing.Add("lương cơ bản");
            if (string.IsNullOrWhiteSpace(snapshot.EmployerRepresentativeName))
                missing.Add("người đại diện ký");

            if (missing.Count > 0)
                throw new InvalidOperationException($"Hợp đồng chưa đủ thông tin pháp lý: {string.Join(", ", missing)}.");

            _ = BuildContractDocumentPreview(contract);
        }

        private static void ValidateContractCoreRules(Core.Entities.EmployeeProfile.Contract contract)
        {
            if (contract.StartDate == default)
                throw new ArgumentException("Ngày bắt đầu hợp đồng không hợp lệ.");
            if ((contract.ContractType == ContractType.Probation || contract.ContractType == ContractType.Definite) && !contract.EndDate.HasValue)
                throw new ArgumentException("Hợp đồng thử việc/xác định thời hạn phải có ngày kết thúc.");
            if (contract.EndDate.HasValue && contract.StartDate.Date >= contract.EndDate.Value.Date)
                throw new ArgumentException("Ngày bắt đầu hợp đồng phải trước ngày kết thúc.");
            if (contract.BasicSalary <= 0)
                throw new ArgumentException("Lương cơ bản phải lớn hơn 0.");
            if (contract.InsuranceSalary < 0)
                throw new ArgumentException("Lương đóng bảo hiểm không được âm.");
        }

        private static ContractLegalSnapshot GetLatestLegalSnapshot(Core.Entities.EmployeeProfile.Contract contract)
        {
            return contract.LegalSnapshots
                .OrderByDescending(s => s.Version)
                .ThenByDescending(s => s.CreatedAt)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("Hợp đồng chưa có bản xem trước pháp lý.");
        }

        private static bool DateRangesOverlap(DateTime leftStart, DateTime? leftEnd, DateTime rightStart, DateTime? rightEnd)
        {
            var normalizedLeftEnd = leftEnd ?? DateTime.MaxValue.Date;
            var normalizedRightEnd = rightEnd ?? DateTime.MaxValue.Date;
            return leftStart <= normalizedRightEnd && rightStart <= normalizedLeftEnd;
        }

        private static ContractDocumentPreviewDto BuildContractDocumentPreview(Core.Entities.EmployeeProfile.Contract contract)
        {
            var snapshot = contract.LegalSnapshots
                .OrderByDescending(s => s.Version)
                .ThenByDescending(s => s.CreatedAt)
                .FirstOrDefault();

            if (snapshot == null)
                throw new InvalidOperationException("Hợp đồng chưa có snapshot pháp lý để xuất văn bản.");

            var documentNumber = FirstNonBlank(snapshot.LegalDocumentNumber, contract.LegalDocumentNumber, contract.ContractNumber) ?? $"HD-{contract.Id:D4}";
            var templateCode = FirstNonBlank(snapshot.DocumentTemplateCode, contract.DocumentTemplateCode, ResolveDocumentTemplateCode(contract.ContractType)) ?? ResolveDocumentTemplateCode(contract.ContractType);
            var fileName = $"{SafeFileName(documentNumber)}.doc";

            return new ContractDocumentPreviewDto
            {
                ReferenceId = contract.Id,
                ReferenceType = "Contract",
                TemplateCode = templateCode,
                DocumentNumber = documentNumber,
                FileName = fileName,
                DocFilePath = snapshot.DocumentDocFilePath,
                PdfFilePath = snapshot.DocumentPdfFilePath,
                Html = BuildContractDocumentHtml(contract, snapshot, templateCode, documentNumber)
            };
        }

        private static string BuildContractDocumentHtml(Core.Entities.EmployeeProfile.Contract contract, ContractLegalSnapshot snapshot, string templateCode, string documentNumber)
        {
            var title = templateCode switch
            {
                "LABOR_CONTRACT_PROBATION" => "HỢP ĐỒNG THỬ VIỆC",
                "LABOR_CONTRACT_INDEFINITE" => "HỢP ĐỒNG LAO ĐỘNG KHÔNG XÁC ĐỊNH THỜI HẠN",
                _ => "HỢP ĐỒNG LAO ĐỘNG XÁC ĐỊNH THỜI HẠN"
            };

            var termText = contract.ContractType == ContractType.Indefinite
                ? $"Từ ngày {DateText(contract.StartDate)}."
                : $"Từ ngày {DateText(contract.StartDate)} đến ngày {DateText(contract.EndDate)}.";

            var body = $@"
<h1>{Html(title)}</h1>
<p class=""doc-basis"">Căn cứ Bộ luật Lao động hiện hành; căn cứ nhu cầu sử dụng lao động của Công ty và thỏa thuận giữa các bên, hai bên thống nhất ký kết hợp đồng này.</p>
<h2>Điều 1. Các bên trong hợp đồng</h2>
<p><strong>Bên sử dụng lao động:</strong> {Html(snapshot.EmployerLegalName)}; mã số thuế: {Html(snapshot.EmployerTaxCode)}; địa chỉ: {Html(snapshot.EmployerAddress)}.</p>
<p>Người đại diện: {Html(snapshot.EmployerRepresentativeName)}; chức vụ: {Html(snapshot.EmployerRepresentativeTitle)}{InlineClause(";", snapshot.EmployerRepresentativeAuthorization)}</p>
<p><strong>Người lao động:</strong> {Html(snapshot.EmployeeFullNameSnapshot)}; ngày sinh: {DateText(snapshot.EmployeeBirthDateSnapshot)}; giới tính: {Html(snapshot.EmployeeGenderSnapshot?.ToString())}; CCCD/CMND: {Html(snapshot.EmployeeIdentityNumberSnapshot)}; ngày cấp: {DateText(snapshot.EmployeeIdentityIssueDate)}; nơi cấp: {Html(snapshot.EmployeeIdentityIssuePlace)}; địa chỉ cư trú: {Html(snapshot.EmployeeResidenceAddressSnapshot)}.</p>
<h2>Điều 2. Loại hợp đồng và thời hạn</h2>
<p>Số hợp đồng: <strong>{Html(documentNumber)}</strong>. Thời hạn hợp đồng: {Html(termText)}</p>
<h2>Điều 3. Công việc và địa điểm làm việc</h2>
<p>Chức danh/công việc: {Html(FirstNonBlank(snapshot.JobTitle, snapshot.EmployeePositionSnapshot))}. Phòng ban: {Html(snapshot.EmployeeDepartmentSnapshot)}. Cấp bậc: {Html(snapshot.EmployeeJobLevelSnapshot)}. Quản lý trực tiếp: {Html(snapshot.DirectManagerSnapshot)}.</p>
<p>{Paragraph(snapshot.JobDescription)}</p>
<p>Địa điểm làm việc: {Html(snapshot.WorkLocation)}. Hình thức làm việc: {Html(snapshot.WorkingMode)}. Thời giờ làm việc: {Html(snapshot.WorkingHours)}. Thời giờ nghỉ ngơi: {Html(snapshot.RestTime)}.</p>
<h2>Điều 4. Tiền lương và chế độ đãi ngộ</h2>
<p>Lương cơ bản: <strong>{FormatMoney(contract.BasicSalary)}</strong>. Tỷ lệ lương áp dụng: {contract.SalaryPercentage:0.##}%. Hình thức trả lương: {Html(snapshot.SalaryPaymentMethod)}. Ngày trả lương: {Html(snapshot.SalaryPaymentDate)}.</p>
<p>Phụ cấp: {Paragraph(snapshot.AllowanceDescription)} Khoản bổ sung/phúc lợi khác: {Paragraph(snapshot.AdditionalBenefits)}</p>
<p>Chính sách nâng lương: {Paragraph(snapshot.SalaryReviewPolicy)} Thưởng/KPI: {Paragraph(snapshot.BonusPolicy)}</p>
{BuildKpiBonusClause(snapshot)}
<h2>Điều 5. Bảo hiểm, bảo hộ lao động và đào tạo</h2>
<p>Bảo hiểm: {Paragraph(snapshot.InsurancePolicy)} Bảo hộ lao động: {Paragraph(snapshot.LaborProtectionPolicy)} Đào tạo/bồi dưỡng: {Paragraph(snapshot.TrainingPolicy)}</p>
<h2>Điều 6. Quyền và nghĩa vụ của các bên</h2>
<p>Nghĩa vụ của người lao động: {Paragraph(snapshot.EmployeeObligations)}</p>
<p>Nghĩa vụ của công ty: {Paragraph(snapshot.EmployerObligations)}</p>
<h2>Điều 7. Bảo mật thông tin và sở hữu trí tuệ</h2>
<p>{Paragraph(snapshot.ConfidentialityClause)}</p>
<p>{Paragraph(snapshot.IntellectualPropertyClause)}</p>
<h2>Điều 8. Chấm dứt hợp đồng và giải quyết tranh chấp</h2>
<p>{Paragraph(snapshot.TerminationClause)}</p>
<p>{Paragraph(snapshot.DisputeResolutionClause)}</p>
<p>Hợp đồng này được lập thành các bản có giá trị pháp lý như nhau; mỗi bên giữ ít nhất một bản để thực hiện.</p>";

            return WrapLegalDocument(
                title,
                documentNumber,
                snapshot.SigningLocation,
                snapshot.IssuedAt ?? contract.IssuedAt ?? DateTime.UtcNow,
                snapshot.EmployerLegalName,
                snapshot.EmployerTaxCode,
                snapshot.EmployerAddress,
                body,
                "NGƯỜI LAO ĐỘNG",
                snapshot.EmployeeFullNameSnapshot,
                $"ĐẠI DIỆN {FirstNonBlank(snapshot.EmployerLegalName, "CÔNG TY")}",
                snapshot.EmployerRepresentativeName,
                snapshot.EmployerRepresentativeTitle);
        }

        private static string BuildKpiBonusClause(ContractLegalSnapshot snapshot)
        {
            var hasAnyKpiTerm =
                snapshot.KpiBonusTargetAmount.HasValue ||
                !string.IsNullOrWhiteSpace(snapshot.KpiScoreFormula) ||
                !string.IsNullOrWhiteSpace(snapshot.KpiPayoutFormula) ||
                !string.IsNullOrWhiteSpace(snapshot.KpiBonusEligibilityRule) ||
                !string.IsNullOrWhiteSpace(snapshot.KpiBonusPaymentPeriod) ||
                !string.IsNullOrWhiteSpace(snapshot.KpiBonusApproverRole);

            if (!hasAnyKpiTerm)
                return string.Empty;

            var targetAmount = snapshot.KpiBonusTargetAmount.HasValue
                ? FormatMoney(snapshot.KpiBonusTargetAmount.Value)
                : "Theo mức thưởng KPI tối đa được ghi nhận trong hệ thống tại từng kỳ áp dụng.";

            return $@"
<p><strong>Khoản thưởng KPI:</strong> Mức thưởng KPI tối đa: {Html(targetAmount)}. Chính sách áp dụng: {Html(snapshot.KpiBonusPolicyCode)}{InlineClause("/", snapshot.KpiBonusPolicyVersionCode)}.</p>
<ul>
  <li>Cách tính điểm KPI: {Paragraph(snapshot.KpiScoreFormula)}</li>
  <li>Cách quy đổi thành tiền: {Paragraph(snapshot.KpiPayoutFormula)}</li>
  <li>Điều kiện nhận/giảm thưởng: {Paragraph(snapshot.KpiBonusEligibilityRule)}</li>
  <li>Kỳ chi trả: {Paragraph(snapshot.KpiBonusPaymentPeriod)}</li>
  <li>Người duyệt: {Paragraph(snapshot.KpiBonusApproverRole)}</li>
</ul>";
        }

        private static string WrapLegalDocument(
            string title,
            string documentNumber,
            string? issuedPlace,
            DateTime issuedAt,
            string? employerName,
            string? taxCode,
            string? address,
            string bodyHtml,
            string leftSignerTitle,
            string? leftSignerName,
            string rightSignerTitle,
            string? rightSignerName,
            string? rightSignerSubTitle)
        {
            return $@"<!doctype html>
<html>
<head>
  <meta charset=""utf-8"" />
  <style>
    body {{ font-family: 'Times New Roman', serif; color: #111827; font-size: 14px; line-height: 1.55; }}
    .doc-page {{ max-width: 780px; margin: 0 auto; padding: 28px 36px; }}
    .doc-header {{ display: grid; grid-template-columns: 1fr 1fr; gap: 24px; align-items: start; }}
    .doc-company {{ font-size: 12px; }}
    .doc-national {{ text-align: center; font-weight: 700; text-transform: uppercase; }}
    .doc-motto {{ text-align: center; font-weight: 700; }}
    .doc-number {{ margin-top: 16px; }}
    .doc-date {{ text-align: right; font-style: italic; margin-top: 16px; }}
    h1 {{ text-align: center; font-size: 20px; margin: 28px 0 12px; text-transform: uppercase; }}
    h2 {{ font-size: 15px; margin: 16px 0 6px; }}
    p {{ margin: 6px 0; }}
    .doc-basis {{ font-style: italic; }}
    .doc-signatures {{ display: grid; grid-template-columns: 1fr 1fr; gap: 48px; margin-top: 38px; text-align: center; }}
    .doc-signature-space {{ height: 76px; }}
    .doc-small {{ font-size: 12px; }}
  </style>
</head>
<body>
  <main class=""doc-page"">
    <header class=""doc-header"">
      <section class=""doc-company"">
        <strong>{Html(employerName)}</strong><br/>
        MST: {Html(taxCode)}<br/>
        {Html(address)}
        <div class=""doc-number"">Số: {Html(documentNumber)}</div>
      </section>
      <section>
        <div class=""doc-national"">CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</div>
        <div class=""doc-motto"">Độc lập - Tự do - Hạnh phúc</div>
      </section>
    </header>
    <div class=""doc-date"">{Html(FirstNonBlank(issuedPlace, "Hà Nội"))}, {VietnameseDateText(issuedAt)}</div>
    {bodyHtml}
    <section class=""doc-signatures"">
      <div>
        <strong>{Html(leftSignerTitle)}</strong><br/>
        <span class=""doc-small"">(Ký, ghi rõ họ tên)</span>
        <div class=""doc-signature-space""></div>
        <strong>{Html(leftSignerName)}</strong>
      </div>
      <div>
        <strong>{Html(rightSignerTitle)}</strong><br/>
        <span class=""doc-small"">{Html(rightSignerSubTitle)}<br/>(Ký, ghi rõ họ tên, đóng dấu nếu có)</span>
        <div class=""doc-signature-space""></div>
        <strong>{Html(rightSignerName)}</strong>
      </div>
    </section>
  </main>
</body>
</html>";
        }

        private static string Html(string? value) =>
            WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(value) ? "-" : value.Trim());

        private static string Paragraph(string? value) =>
            Html(value).Replace("\r\n", "<br/>").Replace("\n", "<br/>");

        private static string InlineClause(string separator, string? value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : $" {Html(separator)} {Html(value)}";

        private static string DateText(DateTime? value) =>
            value.HasValue ? value.Value.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")) : "-";

        private static string VietnameseDateText(DateTime value) =>
            $"ngày {value.Day:00} tháng {value.Month:00} năm {value.Year}";

        private static string SafeFileName(string? value)
        {
            var raw = string.IsNullOrWhiteSpace(value) ? $"document-{DateTime.UtcNow:yyyyMMddHHmmss}" : value.Trim();
            foreach (var invalid in Path.GetInvalidFileNameChars())
                raw = raw.Replace(invalid, '-');
            return raw.Replace("/", "-").Replace("\\", "-");
        }

        private static void ApplyDraft(Core.Entities.EmployeeProfile.Contract contract, CreateDraftDto dto, bool incrementVersion)
        {
            var contractType = ParseContractType(dto.ContractType);

            contract.ContractNumber = ResolveContractNumber(contract);
            contract.ContractType = contractType;
            contract.LegalDocumentType = ResolveLegalDocumentType(contractType);
            contract.DocumentTemplateCode = ResolveDocumentTemplateCode(contractType);
            contract.BasicSalary = dto.BasicSalary;
            contract.SalaryPercentage = dto.SalaryPercentage;
            contract.InsuranceSalary = dto.InsuranceSalary;
            contract.StartDate = dto.StartDate;
            contract.EndDate = dto.EndDate;
            contract.Version = incrementVersion ? contract.Version + 1 : Math.Max(contract.Version, 1);
        }

        private async Task SyncLegalSnapshotAsync(Core.Entities.EmployeeProfile.Contract contract, CreateDraftDto dto, int actorAccountId, CancellationToken ct)
        {
            var source = await BuildLegalSnapshotAsync(contract, dto, actorAccountId, ct);
            var snapshot = contract.LegalSnapshots.FirstOrDefault(s => s.Version == contract.Version);
            if (snapshot == null)
            {
                snapshot = new ContractLegalSnapshot
                {
                    ContractId = contract.Id,
                    Version = contract.Version,
                    CreatedAt = source.CreatedAt,
                    CreatedByAccountId = source.CreatedByAccountId
                };
                contract.LegalSnapshots.Add(snapshot);
            }

            CopyLegalSnapshot(source, snapshot);
            contract.LegalDocumentNumber = snapshot.LegalDocumentNumber;
            contract.DocumentTemplateCode = snapshot.DocumentTemplateCode;
            contract.IssuedAt = snapshot.IssuedAt;
        }

        private async Task<ContractLegalSnapshot> BuildLegalSnapshotAsync(
            Core.Entities.EmployeeProfile.Contract contract,
            CreateDraftDto dto,
            int? actorAccountId,
            CancellationToken ct)
        {
            if (!contract.EmployeeId.HasValue)
                throw new InvalidOperationException("Hợp đồng chưa gắn nhân viên.");

            var employee = await _employeeRepo.GetDocumentProfileByIdAsync(contract.EmployeeId.Value, ct)
                ?? throw new InvalidOperationException("Không tìm thấy hồ sơ nhân viên của hợp đồng.");

            var configs = await LoadContractConfigAsync(ct);
            var effectiveDate = dto.StartDate == default ? DateTime.UtcNow.Date : dto.StartDate.Date;

            var shift = await ResolveWorkShiftAsync(employee, ct);
            var calendar = employee.DeptId.HasValue
                ? await _workCalendarConfigRepo.GetByDeptPeriodAsync(employee.DeptId.Value, (byte)effectiveDate.Month, (short)effectiveDate.Year, ct)
                : null;
            var insuranceConfig = await _payrollRepo.GetActiveInsuranceConfigAsync(effectiveDate, ct);
            var allowancePolicies = await _payrollRepo.GetActivePayrollPoliciesAsync(PayrollPolicyType.Allowance, effectiveDate, ct);
            var kpiBonusPolicy = (await _payrollRepo.GetActivePayrollPoliciesAsync(PayrollPolicyType.KpiBonus, effectiveDate, ct))
                .OrderByDescending(p => p.EffectiveFrom)
                .ThenByDescending(p => p.Version)
                .FirstOrDefault();
            var kpiBonusTerms = ResolveKpiBonusTerms(kpiBonusPolicy);
            var kpiBonusTargetAmount = await ResolveKpiBonusTargetAmountAsync(employee.Id, effectiveDate, ct);
            var positionPolicy = await ResolvePositionPolicyAsync(employee, effectiveDate, ct);

            var resolvedContractNumber = ResolveContractNumber(contract);
            var legalDocumentType = contract.LegalDocumentType ?? ResolveLegalDocumentType(ParseContractType(dto.ContractType));
            var templateCode = FirstNonBlank(
                dto.Issuance?.DocumentTemplateCode,
                contract.DocumentTemplateCode,
                ResolveDocumentTemplateCode(ParseContractType(dto.ContractType)));

            return new ContractLegalSnapshot
            {
                ContractId = contract.Id,
                Version = contract.Version,
                LegalDocumentType = legalDocumentType,
                LegalDocumentNumber = FirstNonBlank(dto.Issuance?.LegalDocumentNumber, contract.LegalDocumentNumber, resolvedContractNumber),
                DocumentTemplateCode = templateCode,

                EmployerLegalName = FirstNonBlank(dto.Employer?.LegalName, GetConfig(configs, "COMPANY_LEGAL_NAME", "COMPANY_NAME", "LEGAL_NAME"), "Công ty Cổ phần Công nghệ HICAS"),
                EmployerTaxCode = FirstNonBlank(dto.Employer?.TaxCode, GetConfig(configs, "COMPANY_TAX_CODE", "TAX_CODE")),
                EmployerAddress = FirstNonBlank(dto.Employer?.Address, GetConfig(configs, "COMPANY_ADDRESS", "ADDRESS")),
                EmployerRepresentativeName = FirstNonBlank(dto.Employer?.RepresentativeName, GetConfig(configs, "COMPANY_REPRESENTATIVE_NAME", "DIRECTOR_NAME", "REPRESENTATIVE_NAME")),
                EmployerRepresentativeTitle = FirstNonBlank(dto.Employer?.RepresentativeTitle, GetConfig(configs, "COMPANY_REPRESENTATIVE_TITLE", "DIRECTOR_TITLE", "REPRESENTATIVE_TITLE"), "Giám đốc"),
                EmployerRepresentativeAuthorization = FirstNonBlank(dto.Employer?.RepresentativeAuthorization, GetConfig(configs, "COMPANY_REPRESENTATIVE_AUTHORIZATION", "REPRESENTATIVE_AUTHORIZATION")),
                SigningLocation = FirstNonBlank(dto.Employer?.SigningLocation, GetConfig(configs, "SIGNING_LOCATION", "CONTRACT_SIGNING_LOCATION"), "Hà Nội"),

                EmployeeFullNameSnapshot = FirstNonBlank(dto.Employee?.FullName, employee.FullName),
                EmployeeBirthDateSnapshot = dto.Employee?.BirthDate ?? employee.BirthDate,
                EmployeeGenderSnapshot = ParseGender(dto.Employee?.Gender) ?? employee.Gender,
                EmployeeIdentityNumberSnapshot = FirstNonBlank(dto.Employee?.IdentityNumber, employee.IdentityNumber),
                EmployeeIdentityIssueDate = dto.Employee?.IdentityIssueDate,
                EmployeeIdentityIssuePlace = FirstNonBlank(dto.Employee?.IdentityIssuePlace, GetConfig(configs, "DEFAULT_IDENTITY_ISSUE_PLACE")),
                EmployeeResidenceAddressSnapshot = FirstNonBlank(dto.Employee?.ResidenceAddress, employee.CurrentAddress, employee.PermanentAddress),
                EmployeeDepartmentSnapshot = FirstNonBlank(dto.Employee?.Department, employee.Department?.DeptName),
                EmployeePositionSnapshot = FirstNonBlank(dto.Employee?.Position, employee.Position?.Title),
                EmployeeJobLevelSnapshot = FirstNonBlank(dto.Employee?.JobLevel, employee.JobLevel?.Name),

                JobTitle = FirstNonBlank(dto.Work?.JobTitle, employee.Position?.Title),
                JobDescription = FirstNonBlank(
                    dto.Work?.JobDescription,
                    GetConfig(configs, "CONTRACT_JOB_DESCRIPTION", "DEFAULT_JOB_DESCRIPTION"),
                    BuildJobDescription(employee)),
                WorkLocation = FirstNonBlank(dto.Work?.WorkLocation, GetConfig(configs, "CONTRACT_WORK_LOCATION", "WORK_LOCATION"), GetConfig(configs, "COMPANY_ADDRESS", "ADDRESS")),
                WorkingMode = FirstNonBlank(dto.Work?.WorkingMode, GetConfig(configs, "CONTRACT_WORKING_MODE", "WORKING_MODE"), "Làm việc toàn thời gian theo quy định của công ty."),
                WorkingHours = FirstNonBlank(dto.Work?.WorkingHours, BuildWorkingHours(shift, calendar), GetConfig(configs, "CONTRACT_WORKING_HOURS", "WORKING_HOURS")),
                RestTime = FirstNonBlank(dto.Work?.RestTime, BuildRestTime(shift), GetConfig(configs, "CONTRACT_REST_TIME", "REST_TIME"), "Theo nội quy lao động và lịch làm việc đang áp dụng."),
                DirectManagerSnapshot = FirstNonBlank(dto.Work?.DirectManager, employee.Manager?.FullName),

                SalaryPaymentMethod = FirstNonBlank(dto.Compensation?.SalaryPaymentMethod, GetConfig(configs, "SALARY_PAYMENT_METHOD", "CONTRACT_SALARY_PAYMENT_METHOD"), "Chuyển khoản"),
                SalaryPaymentDate = FirstNonBlank(dto.Compensation?.SalaryPaymentDate, GetConfig(configs, "SALARY_PAYMENT_DATE", "CONTRACT_SALARY_PAYMENT_DATE"), "Ngày 05 hằng tháng"),
                AllowanceDescription = FirstNonBlank(dto.Compensation?.AllowanceDescription, BuildAllowanceDescription(positionPolicy, allowancePolicies), GetConfig(configs, "ALLOWANCE_DESCRIPTION", "CONTRACT_ALLOWANCE_DESCRIPTION")),
                AdditionalBenefits = FirstNonBlank(dto.Compensation?.AdditionalBenefits, GetConfig(configs, "ADDITIONAL_BENEFITS", "CONTRACT_ADDITIONAL_BENEFITS"), "Theo chính sách phúc lợi hiện hành của công ty."),
                SalaryReviewPolicy = FirstNonBlank(dto.Compensation?.SalaryReviewPolicy, GetConfig(configs, "SALARY_REVIEW_POLICY", "CONTRACT_SALARY_REVIEW_POLICY"), "Xem xét điều chỉnh theo kết quả công việc, chính sách lương và quyết định của công ty."),
                BonusPolicy = FirstNonBlank(dto.Compensation?.BonusPolicy, GetConfig(configs, "BONUS_POLICY", "CONTRACT_BONUS_POLICY"), DefaultBonusPolicyText),
                KpiBonusTargetAmount = kpiBonusTargetAmount,
                KpiBonusPolicyCode = kpiBonusPolicy?.Code ?? DefaultKpiBonusPolicyCode,
                KpiBonusPolicyVersionCode = kpiBonusPolicy?.VersionCode ?? "HICAS_KPI_BONUS_2026_V1",
                KpiScoreFormula = kpiBonusTerms.ScoreFormula,
                KpiPayoutFormula = kpiBonusTerms.PayoutFormula,
                KpiBonusEligibilityRule = kpiBonusTerms.EligibilityRule,
                KpiBonusPaymentPeriod = kpiBonusTerms.PaymentPeriod,
                KpiBonusApproverRole = kpiBonusTerms.ApproverRole,
                InsurancePolicy = FirstNonBlank(dto.Compensation?.InsurancePolicy, BuildInsurancePolicy(contract, insuranceConfig), GetConfig(configs, "INSURANCE_POLICY", "CONTRACT_INSURANCE_POLICY")),
                LaborProtectionPolicy = FirstNonBlank(dto.Compensation?.LaborProtectionPolicy, GetConfig(configs, "LABOR_PROTECTION_POLICY", "CONTRACT_LABOR_PROTECTION_POLICY"), "Công ty cung cấp điều kiện làm việc, công cụ và bảo hộ lao động phù hợp với vị trí công việc."),
                TrainingPolicy = FirstNonBlank(dto.Compensation?.TrainingPolicy, GetConfig(configs, "TRAINING_POLICY", "CONTRACT_TRAINING_POLICY"), "Người lao động được tham gia đào tạo theo kế hoạch và chính sách phát triển nhân sự của công ty."),

                EmployeeObligations = FirstNonBlank(dto.Clauses?.EmployeeObligations, GetConfig(configs, "EMPLOYEE_OBLIGATIONS", "CONTRACT_EMPLOYEE_OBLIGATIONS"), "Thực hiện công việc đúng chức trách, tuân thủ nội quy lao động, bảo mật thông tin và hoàn thành nhiệm vụ được giao."),
                EmployerObligations = FirstNonBlank(dto.Clauses?.EmployerObligations, GetConfig(configs, "EMPLOYER_OBLIGATIONS", "CONTRACT_EMPLOYER_OBLIGATIONS"), "Bố trí công việc, trả lương và bảo đảm các quyền lợi của người lao động theo hợp đồng và quy định pháp luật."),
                ConfidentialityClause = FirstNonBlank(dto.Clauses?.ConfidentialityClause, GetConfig(configs, "CONFIDENTIALITY_CLAUSE", "CONTRACT_CONFIDENTIALITY_CLAUSE"), "Người lao động có trách nhiệm bảo mật thông tin kinh doanh, dữ liệu khách hàng, tài liệu nội bộ và bí mật công nghệ của công ty."),
                IntellectualPropertyClause = FirstNonBlank(dto.Clauses?.IntellectualPropertyClause, GetConfig(configs, "INTELLECTUAL_PROPERTY_CLAUSE", "CONTRACT_IP_CLAUSE"), "Sản phẩm, tài liệu, mã nguồn và kết quả công việc được tạo ra trong phạm vi công việc thuộc quyền sở hữu của công ty, trừ khi có thỏa thuận khác bằng văn bản."),
                TerminationClause = FirstNonBlank(dto.Clauses?.TerminationClause, GetConfig(configs, "TERMINATION_CLAUSE", "CONTRACT_TERMINATION_CLAUSE"), "Việc chấm dứt hợp đồng thực hiện theo hợp đồng, nội quy công ty và quy định pháp luật lao động hiện hành."),
                DisputeResolutionClause = FirstNonBlank(dto.Clauses?.DisputeResolutionClause, GetConfig(configs, "DISPUTE_RESOLUTION_CLAUSE", "CONTRACT_DISPUTE_RESOLUTION_CLAUSE"), "Tranh chấp phát sinh được ưu tiên giải quyết bằng thương lượng; nếu không đạt thỏa thuận sẽ xử lý theo quy định pháp luật Việt Nam."),

                IssuedAt = dto.Issuance?.IssuedAt ?? contract.IssuedAt,
                CreatedAt = DateTime.UtcNow,
                CreatedByAccountId = actorAccountId
            };
        }

        private async Task<Dictionary<string, string>> LoadContractConfigAsync(CancellationToken ct)
        {
            var groups = new[]
            {
                "COMPANY_PROFILE",
                "COMPANY",
                "SYSTEM_COMPANY",
                "DOCUMENT_COMPANY",
                "CONTRACT_DEFAULT",
                "CONTRACT_LEGAL_DEFAULT"
            };

            var configs = await _configurationRepo.FindAsync(c => c.IsActive && groups.Contains(c.ConfigGroup), ct);
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var config in configs)
            {
                AddConfigAlias(result, config.ParamKey, config.ParamValue);
                AddConfigAlias(result, $"{config.ConfigGroup}_{config.ParamKey}", config.ParamValue);
            }

            return result;
        }

        private async Task<WorkShift?> ResolveWorkShiftAsync(Employee employee, CancellationToken ct)
        {
            WorkShift? shift = null;
            if (employee.DeptId.HasValue)
                shift = await _workShiftRepo.GetByDeptIdAsync(employee.DeptId.Value, ct);

            if (shift != null) return shift;

            var activeShifts = await _workShiftRepo.GetAllActiveWithDepartmentAsync(ct);
            return activeShifts.FirstOrDefault(s => !s.DeptId.HasValue) ?? activeShifts.FirstOrDefault();
        }

        private async Task<PositionJobLevelPolicy?> ResolvePositionPolicyAsync(Employee employee, DateTime effectiveDate, CancellationToken ct)
        {
            if (!employee.PositionId.HasValue || !employee.JobLevelId.HasValue)
                return null;

            var policies = await _payrollRepo.GetPositionJobLevelPoliciesAsync(
                new[] { employee.PositionId.Value },
                new[] { employee.JobLevelId.Value },
                effectiveDate,
                ct);

            return policies
                .OrderByDescending(p => p.EffectiveFrom)
                .ThenByDescending(p => p.Version)
                .FirstOrDefault();
        }

        private static void CopyLegalSnapshot(ContractLegalSnapshot source, ContractLegalSnapshot target)
        {
            target.LegalDocumentType = source.LegalDocumentType;
            target.LegalDocumentNumber = source.LegalDocumentNumber;
            target.DocumentTemplateCode = source.DocumentTemplateCode;
            target.EmployerLegalName = source.EmployerLegalName;
            target.EmployerTaxCode = source.EmployerTaxCode;
            target.EmployerAddress = source.EmployerAddress;
            target.EmployerRepresentativeName = source.EmployerRepresentativeName;
            target.EmployerRepresentativeTitle = source.EmployerRepresentativeTitle;
            target.EmployerRepresentativeAuthorization = source.EmployerRepresentativeAuthorization;
            target.SigningLocation = source.SigningLocation;
            target.EmployeeFullNameSnapshot = source.EmployeeFullNameSnapshot;
            target.EmployeeBirthDateSnapshot = source.EmployeeBirthDateSnapshot;
            target.EmployeeGenderSnapshot = source.EmployeeGenderSnapshot;
            target.EmployeeIdentityNumberSnapshot = source.EmployeeIdentityNumberSnapshot;
            target.EmployeeIdentityIssueDate = source.EmployeeIdentityIssueDate;
            target.EmployeeIdentityIssuePlace = source.EmployeeIdentityIssuePlace;
            target.EmployeeResidenceAddressSnapshot = source.EmployeeResidenceAddressSnapshot;
            target.EmployeeDepartmentSnapshot = source.EmployeeDepartmentSnapshot;
            target.EmployeePositionSnapshot = source.EmployeePositionSnapshot;
            target.EmployeeJobLevelSnapshot = source.EmployeeJobLevelSnapshot;
            target.JobTitle = source.JobTitle;
            target.JobDescription = source.JobDescription;
            target.WorkLocation = source.WorkLocation;
            target.WorkingMode = source.WorkingMode;
            target.WorkingHours = source.WorkingHours;
            target.RestTime = source.RestTime;
            target.DirectManagerSnapshot = source.DirectManagerSnapshot;
            target.SalaryPaymentMethod = source.SalaryPaymentMethod;
            target.SalaryPaymentDate = source.SalaryPaymentDate;
            target.AllowanceDescription = source.AllowanceDescription;
            target.AdditionalBenefits = source.AdditionalBenefits;
            target.SalaryReviewPolicy = source.SalaryReviewPolicy;
            target.BonusPolicy = source.BonusPolicy;
            target.KpiBonusTargetAmount = source.KpiBonusTargetAmount;
            target.KpiBonusPolicyCode = source.KpiBonusPolicyCode;
            target.KpiBonusPolicyVersionCode = source.KpiBonusPolicyVersionCode;
            target.KpiScoreFormula = source.KpiScoreFormula;
            target.KpiPayoutFormula = source.KpiPayoutFormula;
            target.KpiBonusEligibilityRule = source.KpiBonusEligibilityRule;
            target.KpiBonusPaymentPeriod = source.KpiBonusPaymentPeriod;
            target.KpiBonusApproverRole = source.KpiBonusApproverRole;
            target.InsurancePolicy = source.InsurancePolicy;
            target.LaborProtectionPolicy = source.LaborProtectionPolicy;
            target.TrainingPolicy = source.TrainingPolicy;
            target.EmployeeObligations = source.EmployeeObligations;
            target.EmployerObligations = source.EmployerObligations;
            target.ConfidentialityClause = source.ConfidentialityClause;
            target.IntellectualPropertyClause = source.IntellectualPropertyClause;
            target.TerminationClause = source.TerminationClause;
            target.DisputeResolutionClause = source.DisputeResolutionClause;
            target.DocumentDocFilePath = source.DocumentDocFilePath;
            target.DocumentPdfFilePath = source.DocumentPdfFilePath;
            target.IssuedAt = source.IssuedAt;
            target.EmployeeSignedAt = source.EmployeeSignedAt;
            target.EmployerSignedAt = source.EmployerSignedAt;
        }

        private static string ResolveContractNumber(Core.Entities.EmployeeProfile.Contract contract)
        {
            return string.IsNullOrWhiteSpace(contract.ContractNumber) ||
                   contract.ContractNumber.StartsWith("TEMP-", StringComparison.OrdinalIgnoreCase)
                ? $"HD-{DateTime.UtcNow.Year}-{contract.Id:D4}"
                : contract.ContractNumber;
        }

        private static void AddConfigAlias(Dictionary<string, string> configs, string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                return;

            configs.TryAdd(NormalizeConfigKey(key), value.Trim());
        }

        private static string NormalizeConfigKey(string key)
        {
            return new string(key.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        }

        private static string? GetConfig(Dictionary<string, string> configs, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (configs.TryGetValue(NormalizeConfigKey(key), out var value) && !string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return null;
        }

        private static string? FirstNonBlank(params string?[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
        }

        private static Gender? ParseGender(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return Enum.TryParse<Gender>(value, true, out var parsed) ? parsed : null;
        }

        private static string BuildJobDescription(Employee employee)
        {
            var position = employee.Position?.Title;
            return string.IsNullOrWhiteSpace(position)
                ? "Thực hiện công việc theo phân công của quản lý trực tiếp và quy định của công ty."
                : $"Thực hiện công việc theo vị trí {position}, kế hoạch của bộ phận và phân công của quản lý trực tiếp.";
        }

        private static string? BuildWorkingHours(WorkShift? shift, WorkCalendarConfig? calendar)
        {
            var parts = new List<string>();

            if (shift?.StartTime.HasValue == true && shift.EndTime.HasValue)
                parts.Add($"{FormatTime(shift.StartTime.Value)} - {FormatTime(shift.EndTime.Value)}");

            if (calendar != null)
                parts.Add($"{calendar.StandardHoursPerDay:0.##} giờ/ngày, {calendar.StandardWorkDays:0.##} ngày công chuẩn/tháng");

            return parts.Count == 0 ? null : string.Join("; ", parts);
        }

        private static string? BuildRestTime(WorkShift? shift)
        {
            if (shift == null || !shift.BreakStartTime.HasValue || !shift.BreakEndTime.HasValue)
                return null;

            var breakStart = shift.BreakStartTime.Value;
            var breakEnd = shift.BreakEndTime.Value;
            return $"Nghỉ giữa ca {FormatTime(breakStart)} - {FormatTime(breakEnd)}.";
        }

        private static string? BuildAllowanceDescription(PositionJobLevelPolicy? positionPolicy, IReadOnlyCollection<PayrollPolicy> allowancePolicies)
        {
            var parts = new List<string>();

            if (positionPolicy != null && positionPolicy.PositionAllowance > 0)
                parts.Add($"Phụ cấp chức vụ {FormatMoney(positionPolicy.PositionAllowance)}");
            if (positionPolicy != null && positionPolicy.ResponsibilityAllowance > 0)
                parts.Add($"Phụ cấp trách nhiệm {FormatMoney(positionPolicy.ResponsibilityAllowance)}");

            foreach (var policy in allowancePolicies.Take(4))
            {
                if (policy.Amount.HasValue)
                    parts.Add($"{policy.Name} {FormatMoney(policy.Amount.Value)}");
                else
                    parts.Add(policy.Name);
            }

            return parts.Count == 0 ? null : string.Join("; ", parts);
        }

        private async Task<decimal?> ResolveKpiBonusTargetAmountAsync(int employeeId, DateTime effectiveDate, CancellationToken ct)
        {
            var components = await _payrollRepo.GetEmployeeSalaryComponentsAsync(
                new[] { employeeId },
                effectiveDate.Date,
                effectiveDate.Date,
                ct);

            return components
                .Where(component => string.Equals(component.SalaryComponentType.Code, "KPI_BONUS", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(component => component.EffectiveFrom)
                .ThenByDescending(component => component.Id)
                .FirstOrDefault()
                ?.Amount;
        }

        private static KpiBonusTerms ResolveKpiBonusTerms(PayrollPolicy? policy)
        {
            var scoreFormula = DefaultKpiScoreFormula;
            var payoutFormula = DefaultKpiPayoutFormula;
            var eligibilityRule = DefaultKpiEligibilityRule;
            var paymentPeriod = DefaultKpiPaymentPeriod;
            var approverRole = DefaultKpiApproverRole;

            if (!string.IsNullOrWhiteSpace(policy?.FormulaJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(policy.FormulaJson);
                    var root = doc.RootElement;
                    scoreFormula = FirstNonBlank(GetJsonString(root, "scoreFormula"), scoreFormula)!;
                    payoutFormula = FirstNonBlank(GetJsonString(root, "payoutFormula"), payoutFormula)!;
                    eligibilityRule = FirstNonBlank(GetJsonString(root, "eligibilityRule"), eligibilityRule)!;
                    paymentPeriod = FirstNonBlank(GetJsonString(root, "paymentPeriod"), paymentPeriod)!;
                    approverRole = FirstNonBlank(GetJsonString(root, "approverRole"), approverRole)!;
                }
                catch (JsonException)
                {
                    // Policy JSON is configuration data; keep contract drafting usable with defaults.
                }
            }

            return new KpiBonusTerms(scoreFormula, payoutFormula, eligibilityRule, paymentPeriod, approverRole);
        }

        private static string? GetJsonString(JsonElement root, string propertyName)
        {
            return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        private static string BuildInsurancePolicy(Core.Entities.EmployeeProfile.Contract contract, InsuranceConfig? insuranceConfig)
        {
            if (!contract.IsInsuranceEligible)
                return "Không thuộc diện tham gia bảo hiểm bắt buộc theo loại hợp đồng hoặc chính sách đang áp dụng.";

            if (insuranceConfig == null)
                return $"Tham gia BHXH, BHYT, BHTN theo quy định hiện hành; lương đóng bảo hiểm: {FormatMoney(contract.InsuranceSalary)}.";

            var employeeRate = insuranceConfig.SocialInsuranceEmployeeRate + insuranceConfig.HealthInsuranceEmployeeRate + insuranceConfig.UnemploymentInsuranceEmployeeRate;
            var employerRate = insuranceConfig.SocialInsuranceEmployerRate + insuranceConfig.HealthInsuranceEmployerRate + insuranceConfig.UnemploymentInsuranceEmployerRate + insuranceConfig.UnionFeeEmployerRate;

            return $"Tham gia BHXH, BHYT, BHTN theo quy định hiện hành; lương đóng bảo hiểm: {FormatMoney(contract.InsuranceSalary)}; tỷ lệ người lao động {FormatRate(employeeRate)}, công ty {FormatRate(employerRate)}.";
        }

        private static string FormatTime(TimeSpan time) => time.ToString(@"hh\:mm", CultureInfo.InvariantCulture);

        private static string FormatMoney(decimal value) =>
            string.Create(CultureInfo.GetCultureInfo("vi-VN"), $"{value:N0} VND");

        private static string FormatRate(decimal value) =>
            string.Create(CultureInfo.GetCultureInfo("vi-VN"), $"{value * 100m:0.##}%");

        private sealed record KpiBonusTerms(
            string ScoreFormula,
            string PayoutFormula,
            string EligibilityRule,
            string PaymentPeriod,
            string ApproverRole);

        private static ContractType ParseContractType(string contractType)
        {
            if (string.Equals(contractType, "FixedTerm", StringComparison.OrdinalIgnoreCase))
                return ContractType.Definite;
            if (Enum.TryParse<ContractType>(contractType, true, out var parsed))
                return parsed;
            throw new ArgumentException("Loại hợp đồng không hợp lệ.");
        }

        private static ContractLegalDocumentType ResolveLegalDocumentType(ContractType contractType) =>
            contractType switch
            {
                ContractType.Probation => ContractLegalDocumentType.ProbationContract,
                ContractType.Indefinite => ContractLegalDocumentType.IndefiniteTermLaborContract,
                _ => ContractLegalDocumentType.FixedTermLaborContract
            };

        private static string ResolveDocumentTemplateCode(ContractType contractType) =>
            contractType switch
            {
                ContractType.Probation => "LABOR_CONTRACT_PROBATION",
                ContractType.Indefinite => "LABOR_CONTRACT_INDEFINITE",
                _ => "LABOR_CONTRACT_FIXED_TERM"
            };

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

        private static ContractResponseDto MapToDto(Core.Entities.EmployeeProfile.Contract c, ContractLegalSnapshot? previewSnapshot = null)
        {
            var legalSnapshot = previewSnapshot ?? c.LegalSnapshots
                .OrderByDescending(s => s.Version)
                .ThenByDescending(s => s.CreatedAt)
                .FirstOrDefault();

            return new()
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
                EmployeeName = c.Employee?.FullName,
                LegalDocumentType = (legalSnapshot?.LegalDocumentType ?? c.LegalDocumentType)?.ToString(),
                EmployerLegalName = legalSnapshot?.EmployerLegalName,
                EmployerTaxCode = legalSnapshot?.EmployerTaxCode,
                EmployerAddress = legalSnapshot?.EmployerAddress,
                EmployerRepresentativeName = legalSnapshot?.EmployerRepresentativeName,
                EmployerRepresentativeTitle = legalSnapshot?.EmployerRepresentativeTitle,
                EmployerRepresentativeAuthorization = legalSnapshot?.EmployerRepresentativeAuthorization,
                SigningLocation = legalSnapshot?.SigningLocation,
                EmployeeFullNameSnapshot = legalSnapshot?.EmployeeFullNameSnapshot,
                EmployeeBirthDateSnapshot = legalSnapshot?.EmployeeBirthDateSnapshot,
                EmployeeGenderSnapshot = legalSnapshot?.EmployeeGenderSnapshot?.ToString(),
                EmployeeIdentityNumberSnapshot = legalSnapshot?.EmployeeIdentityNumberSnapshot,
                EmployeeIdentityIssueDate = legalSnapshot?.EmployeeIdentityIssueDate,
                EmployeeIdentityIssuePlace = legalSnapshot?.EmployeeIdentityIssuePlace,
                EmployeeResidenceAddressSnapshot = legalSnapshot?.EmployeeResidenceAddressSnapshot,
                EmployeeDepartmentSnapshot = legalSnapshot?.EmployeeDepartmentSnapshot,
                EmployeePositionSnapshot = legalSnapshot?.EmployeePositionSnapshot,
                EmployeeJobLevelSnapshot = legalSnapshot?.EmployeeJobLevelSnapshot,
                JobTitle = legalSnapshot?.JobTitle,
                JobDescription = legalSnapshot?.JobDescription,
                WorkLocation = legalSnapshot?.WorkLocation,
                WorkingMode = legalSnapshot?.WorkingMode,
                WorkingHours = legalSnapshot?.WorkingHours,
                RestTime = legalSnapshot?.RestTime,
                DirectManagerSnapshot = legalSnapshot?.DirectManagerSnapshot,
                SalaryPaymentMethod = legalSnapshot?.SalaryPaymentMethod,
                SalaryPaymentDate = legalSnapshot?.SalaryPaymentDate,
                AllowanceDescription = legalSnapshot?.AllowanceDescription,
                AdditionalBenefits = legalSnapshot?.AdditionalBenefits,
                SalaryReviewPolicy = legalSnapshot?.SalaryReviewPolicy,
                BonusPolicy = legalSnapshot?.BonusPolicy,
                KpiBonusTargetAmount = legalSnapshot?.KpiBonusTargetAmount,
                KpiBonusPolicyCode = legalSnapshot?.KpiBonusPolicyCode,
                KpiBonusPolicyVersionCode = legalSnapshot?.KpiBonusPolicyVersionCode,
                KpiScoreFormula = legalSnapshot?.KpiScoreFormula,
                KpiPayoutFormula = legalSnapshot?.KpiPayoutFormula,
                KpiBonusEligibilityRule = legalSnapshot?.KpiBonusEligibilityRule,
                KpiBonusPaymentPeriod = legalSnapshot?.KpiBonusPaymentPeriod,
                KpiBonusApproverRole = legalSnapshot?.KpiBonusApproverRole,
                InsurancePolicy = legalSnapshot?.InsurancePolicy,
                LaborProtectionPolicy = legalSnapshot?.LaborProtectionPolicy,
                TrainingPolicy = legalSnapshot?.TrainingPolicy,
                EmployeeObligations = legalSnapshot?.EmployeeObligations,
                EmployerObligations = legalSnapshot?.EmployerObligations,
                ConfidentialityClause = legalSnapshot?.ConfidentialityClause,
                IntellectualPropertyClause = legalSnapshot?.IntellectualPropertyClause,
                TerminationClause = legalSnapshot?.TerminationClause,
                DisputeResolutionClause = legalSnapshot?.DisputeResolutionClause,
                LegalDocumentNumber = legalSnapshot?.LegalDocumentNumber ?? c.LegalDocumentNumber,
                DocumentTemplateCode = legalSnapshot?.DocumentTemplateCode ?? c.DocumentTemplateCode,
                DocumentDocFilePath = legalSnapshot?.DocumentDocFilePath,
                DocumentPdfFilePath = legalSnapshot?.DocumentPdfFilePath,
                IssuedAt = legalSnapshot?.IssuedAt ?? c.IssuedAt,
                EmployeeSignedAt = legalSnapshot?.EmployeeSignedAt,
                EmployerSignedAt = legalSnapshot?.EmployerSignedAt
            };
        }
    }
}
