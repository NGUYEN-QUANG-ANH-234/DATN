using System.Text.Json;
using System.Globalization;
using System.Net;
using System.Text;
using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;
using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.WorkflowRequests;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using MediatR;

namespace HRM.backend.src.HRM.Application.UseCases.EmployeeProfile
{
    public class ContractAddendumUseCase : IContractAddendumUseCase
    {
        private static readonly HashSet<string> ContractRenewalJsonKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "startDate",
            "contractStartDate",
            "endDate",
            "newEndDate",
            "contractEndDate",
            "contractType",
            "contractTerm",
            "contractDuration",
            "renewal",
            "extension"
        };

        private static readonly HashSet<string> FullContractRewriteJsonKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "fullContract",
            "fullContractJson",
            "allTerms",
            "employerInfo",
            "employeeInfo",
            "contractTerms",
            "legalTerms",
            "workingTerms",
            "salaryAndBenefits"
        };

        private readonly IContractRepository _contractRepo;
        private readonly IContractAddendumRepository _addendumRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IBaseRepository<EmploymentHistory> _historyRepo;
        private readonly IApprovalConflictGuard _approvalConflictGuard;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;
        private readonly IIdempotencyService _idempotencyService;
        private readonly IMediator _mediator;

        public ContractAddendumUseCase(
            IContractRepository contractRepo,
            IContractAddendumRepository addendumRepo,
            IEmployeeRepository employeeRepo,
            IBaseRepository<EmploymentHistory> historyRepo,
            IApprovalConflictGuard approvalConflictGuard,
            IAuditLogRepository auditLogRepo,
            IUnitOfWork unitOfWork,
            ILockService lockService,
            IIdempotencyService idempotencyService,
            IMediator mediator)
        {
            _contractRepo = contractRepo;
            _addendumRepo = addendumRepo;
            _employeeRepo = employeeRepo;
            _historyRepo = historyRepo;
            _approvalConflictGuard = approvalConflictGuard;
            _auditLogRepo = auditLogRepo;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
            _idempotencyService = idempotencyService;
            _mediator = mediator;
        }

        public async Task<ContractAddendumResponseDto> CreateDraftAsync(int contractId, CreateContractAddendumDto dto, CancellationToken ct, string? idempotencyKey = null)
        {
            var existingResourceId = string.IsNullOrWhiteSpace(idempotencyKey)
                ? null
                : await _idempotencyService.FindResourceIdAsync("CONTRACT_ADDENDUM_CREATE", idempotencyKey, ct);
            if (existingResourceId.HasValue)
            {
                var existing = await _addendumRepo.GetByIdWithContractAsync(existingResourceId.Value, ct);
                if (existing != null)
                    return Map(existing);
            }

            ValidateDraft(dto);

            var addendumId = await _lockService.GetWithLockAsync($"addendum_create_{contractId}", async (innerCt) =>
            {
                ContractAddendum? addendum = null;
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                var contract = await _contractRepo.GetByIdAsync(contractId, innerCt);
                if (contract == null)
                    throw new InvalidOperationException("Không tìm thấy hợp đồng gốc.");
                if (contract.Status != ContractStatus.Active)
                    throw new InvalidOperationException("Chỉ có thể tạo phụ lục cho hợp đồng đang có hiệu lực.");

                ValidateDraftAgainstContract(dto, contract);

                addendum = new ContractAddendum
                {
                    ContractId = contractId,
                    AddendumNumber = GenerateAddendumNumber(contractId),
                    Status = AddendumStatus.Draft
                };
                ApplyDraftFields(addendum, dto, contract);

                await _addendumRepo.AddAsync(addendum, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
                await _idempotencyService.SaveAsync("CONTRACT_ADDENDUM_CREATE", idempotencyKey ?? string.Empty, "ContractAddendum", addendum.Id, null, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
                }, innerCt);
                return addendum!.Id;
            }, cancellationToken: ct);

            var created = await _addendumRepo.GetByIdWithContractAsync(addendumId, ct);
            return Map(created!);
        }

        public async Task<ContractAddendumResponseDto> UpdateDraftAsync(int addendumId, CreateContractAddendumDto dto, CancellationToken ct)
        {
            ValidateDraft(dto);

            await _lockService.GetWithLockAsync($"addendum_{addendumId}", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, innerCt);
                if (addendum == null)
                    throw new InvalidOperationException("Không tìm thấy phụ lục hợp đồng.");
                if (addendum.Status != AddendumStatus.Draft &&
                    addendum.Status != AddendumStatus.PendingHRRevision)
                    throw new InvalidOperationException("Chỉ có thể sửa bản nháp phụ lục.");
                if (addendum.Contract == null || addendum.Contract.Status != ContractStatus.Active)
                    throw new InvalidOperationException("Hợp đồng gốc không còn ở trạng thái có hiệu lực.");

                ValidateDraftAgainstContract(dto, addendum.Contract);
                addendum.Details.Clear();
                ApplyDraftFields(addendum, dto, addendum.Contract);
                await _addendumRepo.UpdateAsync(addendum, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
                }, innerCt);
                return true;
            }, cancellationToken: ct);

            var updated = await _addendumRepo.GetByIdWithContractAsync(addendumId, ct);
            return Map(updated!);
        }

        public async Task<ContractDocumentPreviewDto> PreviewDocumentAsync(int addendumId, CancellationToken ct)
        {
            var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, ct)
                ?? throw new InvalidOperationException("Không tìm thấy phụ lục hợp đồng.");

            return BuildAddendumDocumentPreview(addendum);
        }

        public async Task<ContractDocumentDownloadDto> DownloadDocumentDocAsync(int addendumId, CancellationToken ct)
        {
            var preview = await PreviewDocumentAsync(addendumId, ct);
            return new ContractDocumentDownloadDto
            {
                FileName = preview.FileName,
                ContentType = "application/msword; charset=utf-8",
                Content = Encoding.UTF8.GetBytes(preview.Html)
            };
        }

        public async Task<ContractDocumentDownloadDto> DownloadDocumentPdfAsync(int addendumId, CancellationToken ct)
        {
            var preview = await PreviewDocumentAsync(addendumId, ct);
            if (string.IsNullOrWhiteSpace(preview.PdfFilePath) || !File.Exists(preview.PdfFilePath))
                throw new InvalidOperationException("Phụ lục chưa có file PDF đã phát hành.");

            return new ContractDocumentDownloadDto
            {
                FileName = Path.ChangeExtension(preview.FileName, ".pdf"),
                ContentType = "application/pdf",
                Content = await File.ReadAllBytesAsync(preview.PdfFilePath, ct)
            };
        }

        public async Task<ContractDocumentPreviewDto> IssueDocumentAsync(int addendumId, IssueContractDocumentDto dto, int actorAccountId, string actorRoleName, CancellationToken ct)
        {
            ContractAddendum? updated = null;
            bool activated = false;
            int? completedContractId = null;

            await _lockService.GetWithLockAsync($"addendum_{addendumId}", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, innerCt)
                        ?? throw new InvalidOperationException("Không tìm thấy phụ lục hợp đồng.");

                    if (addendum.Status != AddendumStatus.ApprovedByDirector && addendum.Status != AddendumStatus.Active)
                        throw new InvalidOperationException("Chỉ phát hành phụ lục sau khi đã qua các bước xác nhận chính.");

                    EnsureHrDirectorOrAdmin(actorRoleName);
                    if (addendum.Contract?.EmployeeId.HasValue == true && !IsDirector(actorRoleName) && !IsAdmin(actorRoleName))
                        await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(addendum.Contract.EmployeeId.Value, actorAccountId, innerCt);

                    addendum.LegalDocumentNumber = FirstNonBlank(dto.LegalDocumentNumber, addendum.LegalDocumentNumber, addendum.AddendumNumber);
                    addendum.DocumentTemplateCode = FirstNonBlank(dto.DocumentTemplateCode, addendum.DocumentTemplateCode, "CONTRACT_ADDENDUM");
                    addendum.IssuedAt = dto.IssuedAt ?? addendum.IssuedAt ?? DateTime.UtcNow;
                    addendum.EmployeeSignedAt = dto.EmployeeSignedAt ?? addendum.EmployeeSignedAt;
                    addendum.EmployerSignedAt = dto.EmployerSignedAt ?? addendum.EmployerSignedAt ?? addendum.IssuedAt;
                    addendum.DocumentDocFilePath = $"/contract-documents/addendums/{addendum.Id}/{SafeFileName(addendum.LegalDocumentNumber)}.doc";

                    if (addendum.Status == AddendumStatus.ApprovedByDirector)
                    {
                        var contract = addendum.Contract ?? throw new InvalidOperationException("Phụ lục chưa liên kết hợp đồng gốc.");
                        if (!contract.EmployeeId.HasValue)
                            throw new InvalidOperationException("Hợp đồng gốc chưa gắn nhân viên.");

                        var employee = await _employeeRepo.GetByIdAsync(contract.EmployeeId.Value, innerCt)
                            ?? throw new InvalidOperationException("Không tìm thấy nhân viên của hợp đồng.");

                        await ApplySalaryChangesAsync(addendum, contract, employee.Id, innerCt);
                        await ApplyOtherChangesAsync(addendum, employee, innerCt);

                        addendum.Status = AddendumStatus.Active;
                        addendum.RejectReason = null;
                        completedContractId = contract.Id;
                        activated = true;

                        await _contractRepo.UpdateAsync(contract, innerCt);
                    }

                    await _addendumRepo.UpdateAsync(addendum, innerCt);
                    await _unitOfWork.CommitAsync(innerCt);
                    updated = addendum;
                }, innerCt);

                return true;
            }, cancellationToken: ct);

            if (activated)
            {
                await _mediator.Publish(new ContractFlowCompletedEvent
                {
                    ContractId = completedContractId,
                    ContractAddendumId = addendumId,
                    Status = "Completed"
                }, ct);
            }

            return BuildAddendumDocumentPreview(updated!);
        }

        public async Task SubmitAsync(int addendumId, CancellationToken ct)
        {
            await _lockService.GetWithLockAsync($"addendum_{addendumId}", async (innerCt) =>
            {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, innerCt);
                if (addendum == null)
                    throw new InvalidOperationException("Không tìm thấy phụ lục hợp đồng.");
                if (addendum.Status != AddendumStatus.Draft &&
                    addendum.Status != AddendumStatus.PendingHRRevision)
                    throw new InvalidOperationException("Chỉ bản nháp phụ lục mới có thể gửi duyệt.");

                if (addendum.Contract?.EmployeeId.HasValue != true)
                    throw new InvalidOperationException("Phụ lục chưa liên kết nhân viên.");

                addendum.Status = AddendumStatus.PendingDept;
                await _addendumRepo.UpdateAsync(addendum, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
            }, innerCt);
            return true;
            }, cancellationToken: ct);
        }

        public async Task ReviewByDeptAsync(int addendumId, int actorAccountId, string actorRoleName, ReviewContractAddendumDto dto, CancellationToken ct)
        {
            int? contractId = null;

            await _lockService.GetWithLockAsync($"addendum_{addendumId}", async (innerCt) =>
            {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, innerCt);
                if (addendum == null)
                    throw new InvalidOperationException("Không tìm thấy phụ lục hợp đồng.");
                contractId = addendum.ContractId;
                if (addendum.Status != AddendumStatus.PendingDept)
                    throw new InvalidOperationException("Phụ lục không ở trạng thái chờ Trưởng phòng xác nhận.");

                await EnsureManagerCanAccessAsync(addendum, actorAccountId, actorRoleName, innerCt);
                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(GetTargetEmployeeId(addendum), actorAccountId, innerCt);

                addendum.Status = dto.IsApproved ? AddendumStatus.PendingEmployee : AddendumStatus.PendingHRRevision;
                addendum.RejectReason = dto.IsApproved
                    ? null
                    : string.IsNullOrWhiteSpace(dto.RejectReason)
                        ? "Trưởng phòng từ chối phụ lục hợp đồng."
                        : dto.RejectReason.Trim();

                await _addendumRepo.UpdateAsync(addendum, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
            }, innerCt);
            return true;
            }, cancellationToken: ct);

            if (!dto.IsApproved)
                await PublishContractFlowNegotiatingAsync(contractId, addendumId, dto.RejectReason, ct);
        }

        public async Task ConfirmByHrAsync(int addendumId, int actorAccountId, string actorRoleName, ReviewContractAddendumDto dto, CancellationToken ct)
        {
            int? contractId = null;

            await _lockService.GetWithLockAsync($"addendum_{addendumId}", async (innerCt) =>
            {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, innerCt);
                if (addendum == null)
                    throw new InvalidOperationException("Không tìm thấy phụ lục hợp đồng.");
                contractId = addendum.ContractId;
                if (addendum.Status != AddendumStatus.PendingHR)
                    throw new InvalidOperationException("Phụ lục không ở trạng thái chờ HR xác nhận chính sách.");

                EnsureHrDirectorOrAdmin(actorRoleName);
                if (!IsDirector(actorRoleName) && !IsAdmin(actorRoleName))
                    await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(GetTargetEmployeeId(addendum), actorAccountId, innerCt);

                addendum.Status = dto.IsApproved ? AddendumStatus.PendingEmployee : AddendumStatus.PendingHRRevision;
                addendum.RejectReason = dto.IsApproved
                    ? null
                    : string.IsNullOrWhiteSpace(dto.RejectReason)
                        ? "HR từ chối phụ lục do không đáp ứng chính sách."
                        : dto.RejectReason.Trim();

                await _addendumRepo.UpdateAsync(addendum, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
            }, innerCt);
            return true;
            }, cancellationToken: ct);

            if (!dto.IsApproved)
                await PublishContractFlowNegotiatingAsync(contractId, addendumId, dto.RejectReason, ct);
        }

        public async Task EmployeeConfirmAsync(int addendumId, int actorAccountId, ReviewContractAddendumDto dto, CancellationToken ct)
        {
            int? contractId = null;

            await _lockService.GetWithLockAsync($"addendum_{addendumId}", async (innerCt) =>
            {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, innerCt);
                if (addendum == null)
                    throw new InvalidOperationException("Không tìm thấy phụ lục hợp đồng.");
                contractId = addendum.ContractId;
                if (addendum.Status != AddendumStatus.PendingEmployee)
                    throw new InvalidOperationException("Phụ lục không ở trạng thái chờ người lao động xác nhận.");

                await EnsureEmployeeOwnsAddendumAsync(addendum, actorAccountId, innerCt);

                addendum.Status = dto.IsApproved ? AddendumStatus.PendingDirector : AddendumStatus.PendingHRRevision;
                addendum.RejectReason = dto.IsApproved
                    ? null
                    : string.IsNullOrWhiteSpace(dto.RejectReason)
                        ? "Người lao động từ chối điều khoản phụ lục hợp đồng."
                        : dto.RejectReason.Trim();

                await _addendumRepo.UpdateAsync(addendum, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
            }, innerCt);
            return true;
            }, cancellationToken: ct);

            if (!dto.IsApproved)
                await PublishContractFlowNegotiatingAsync(contractId, addendumId, dto.RejectReason, ct);
        }

        public async Task ApproveAsync(int addendumId, int actorAccountId, string actorRoleName, CancellationToken ct)
        {
            await _lockService.GetWithLockAsync($"addendum_{addendumId}", async (innerCt) =>
            {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, innerCt);
                if (addendum == null)
                    throw new InvalidOperationException("Không tìm thấy phụ lục hợp đồng.");
                if (addendum.Status != AddendumStatus.PendingDirector)
                    throw new InvalidOperationException("Phụ lục không ở trạng thái chờ Giám đốc phê duyệt.");

                var contract = addendum.Contract ?? throw new InvalidOperationException("Phụ lục chưa liên kết hợp đồng gốc.");
                if (!contract.EmployeeId.HasValue)
                    throw new InvalidOperationException("Hợp đồng gốc chưa gắn nhân viên.");

                EnsureDirectorOrAdmin(actorRoleName);
                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(contract.EmployeeId.Value, actorAccountId, innerCt);

                addendum.Status = AddendumStatus.ApprovedByDirector;
                addendum.RejectReason = null;

                await _addendumRepo.UpdateAsync(addendum, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
            }, innerCt);
            return true;
            }, cancellationToken: ct);

        }

        public async Task DirectorReviewAsync(int addendumId, int actorAccountId, string actorRoleName, ReviewContractAddendumDto dto, CancellationToken ct)
        {
            if (dto.IsApproved)
            {
                await ApproveAsync(addendumId, actorAccountId, actorRoleName, ct);
                return;
            }

            int? contractId = null;
            string? reason = dto.RejectReason;

            await _lockService.GetWithLockAsync($"addendum_{addendumId}", async (innerCt) =>
            {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, innerCt);
                if (addendum == null)
                    throw new InvalidOperationException("Khong tim thay phu luc hop dong.");
                contractId = addendum.ContractId;
                if (addendum.Status != AddendumStatus.PendingDirector)
                    throw new InvalidOperationException("Phu luc khong o trang thai cho Giam doc duyet.");

                EnsureDirectorOrAdmin(actorRoleName);
                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(GetTargetEmployeeId(addendum), actorAccountId, innerCt);

                addendum.Status = AddendumStatus.PendingHRRevision;
                addendum.RejectReason = string.IsNullOrWhiteSpace(dto.RejectReason)
                    ? "Giam doc yeu cau HR chinh sua phu luc hop dong."
                    : dto.RejectReason.Trim();
                reason = addendum.RejectReason;

                await _addendumRepo.UpdateAsync(addendum, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
            }, innerCt);
            return true;
            }, cancellationToken: ct);

            await PublishContractFlowNegotiatingAsync(contractId, addendumId, reason, ct);
        }

        public async Task RequestRevisionAsync(int addendumId, int actorAccountId, string actorRoleName, RequestRevisionDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new ArgumentException("Vui long nhap ly do yeu cau chinh sua.");

            var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, ct)
                ?? throw new InvalidOperationException("Khong tim thay phu luc hop dong.");
            var oldStatus = addendum.Status;
            var reason = dto.Reason.Trim();

            switch (addendum.Status)
            {
                case AddendumStatus.PendingDept:
                    await ReviewByDeptAsync(addendumId, actorAccountId, actorRoleName, new ReviewContractAddendumDto
                    {
                        IsApproved = false,
                        RejectReason = reason
                    }, ct);
                    break;

                case AddendumStatus.PendingHR:
                    await ConfirmByHrAsync(addendumId, actorAccountId, actorRoleName, new ReviewContractAddendumDto
                    {
                        IsApproved = false,
                        RejectReason = reason
                    }, ct);
                    break;

                case AddendumStatus.PendingEmployee:
                    await EmployeeConfirmAsync(addendumId, actorAccountId, new ReviewContractAddendumDto
                    {
                        IsApproved = false,
                        RejectReason = reason
                    }, ct);
                    break;

                case AddendumStatus.PendingDirector:
                    await DirectorReviewAsync(addendumId, actorAccountId, actorRoleName, new ReviewContractAddendumDto
                    {
                        IsApproved = false,
                        RejectReason = reason
                    }, ct);
                    break;

                default:
                    throw new InvalidOperationException("Phu luc khong o trang thai co the yeu cau chinh sua.");
            }

            var updated = await _addendumRepo.GetByIdWithContractAsync(addendumId, ct);
            await _auditLogRepo.LogSystemEventAsync(
                "CONTRACT_ADDENDUM_REVISION_REQUESTED",
                actorAccountId,
                "contract_addendum",
                $"ActorRole={actorRoleName}; AddendumId={addendumId}; AddendumNumber={addendum.AddendumNumber}; ContractId={addendum.ContractId}; Status={oldStatus}->{updated?.Status}; Reason={reason}; RequestedAt={DateTime.UtcNow:O}");
            await _unitOfWork.CommitAsync(ct);
        }

        public async Task RejectAsync(int addendumId, int actorAccountId, string actorRoleName, string? reason, CancellationToken ct)
        {
            int? contractId = null;

            await _lockService.GetWithLockAsync($"addendum_{addendumId}", async (innerCt) =>
            {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, innerCt);
                if (addendum == null)
                    throw new InvalidOperationException("Không tìm thấy phụ lục hợp đồng.");
                contractId = addendum.ContractId;
                if (addendum.Status != AddendumStatus.PendingDirector)
                    throw new InvalidOperationException("Chỉ phụ lục đang chờ duyệt mới có thể bị từ chối.");

                EnsureDirectorOrAdmin(actorRoleName);
                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(GetTargetEmployeeId(addendum), actorAccountId, innerCt);

                addendum.Status = AddendumStatus.PendingHRRevision;
                addendum.RejectReason = string.IsNullOrWhiteSpace(reason)
                    ? "Giám đốc từ chối phụ lục hợp đồng."
                    : reason.Trim();

                await _addendumRepo.UpdateAsync(addendum, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
            }, innerCt);
            return true;
            }, cancellationToken: ct);

            await PublishContractFlowNegotiatingAsync(contractId, addendumId, reason, ct);
        }

        public async Task<IEnumerable<ContractAddendumResponseDto>> GetByContractAsync(int contractId, CancellationToken ct)
        {
            var addendums = await _addendumRepo.GetByContractIdAsync(contractId, ct);
            return addendums.Select(Map);
        }

        public async Task<IEnumerable<ContractAddendumResponseDto>> GetMyPendingEmployeeAsync(int actorAccountId, CancellationToken ct)
        {
            var employee = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct);
            if (employee == null) return Enumerable.Empty<ContractAddendumResponseDto>();

            var addendums = await _addendumRepo.GetByStatusAsync(AddendumStatus.PendingEmployee, ct);
            return addendums
                .Where(a => a.Contract?.EmployeeId == employee.Id)
                .Select(Map);
        }

        public async Task<IEnumerable<ContractAddendumResponseDto>> GetPendingDeptAsync(int actorAccountId, string actorRoleName, CancellationToken ct)
        {
            var addendums = await _addendumRepo.GetByStatusAsync(AddendumStatus.PendingDept, ct);

            if (IsAdmin(actorRoleName))
                return addendums.Select(Map);

            if (!IsManager(actorRoleName))
                throw new UnauthorizedAccessException("Chỉ Trưởng phòng hoặc Admin được xem phụ lục chờ xác nhận nghiệp vụ.");

            var manager = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct)
                ?? throw new UnauthorizedAccessException("Tài khoản Trưởng phòng chưa liên kết hồ sơ nhân sự.");

            return addendums
                .Where(a => a.Contract?.Employee?.Department?.Manager?.AccountId == actorAccountId)
                .Select(Map);
        }

        public async Task<IEnumerable<ContractAddendumResponseDto>> GetPendingHRAsync(CancellationToken ct)
        {
            var addendums = await _addendumRepo.GetAllWithContractAsync(ct);
            return addendums
                .Where(a => a.Status == AddendumStatus.PendingHR ||
                            a.Status == AddendumStatus.PendingHRRevision ||
                            a.Status == AddendumStatus.Draft)
                .Select(Map);
        }

        public async Task<IEnumerable<ContractAddendumResponseDto>> GetPendingDirectorAsync(int actorAccountId, string actorRoleName, CancellationToken ct)
        {
            var addendums = await _addendumRepo.GetByStatusAsync(AddendumStatus.PendingDirector, ct);
            if (IsDirector(actorRoleName))
            {
                addendums = addendums
                    .Where(a => a.Contract?.Employee?.AccountId != actorAccountId)
                    .ToList();
            }

            return addendums.Select(Map);
        }

        public async Task<IEnumerable<ContractAddendumResponseDto>> GetAllAsync(CancellationToken ct)
        {
            var addendums = await _addendumRepo.GetAllWithContractAsync(ct);
            return addendums.Select(Map);
        }

        private Task PublishContractFlowNegotiatingAsync(int? contractId, int addendumId, string? reason, CancellationToken ct)
        {
            return _mediator.Publish(new ContractFlowCompletedEvent
            {
                ContractId = contractId,
                ContractAddendumId = addendumId,
                Status = "Negotiating",
                Note = reason
            }, ct);
        }

        private static ContractDocumentPreviewDto BuildAddendumDocumentPreview(ContractAddendum addendum)
        {
            var documentNumber = FirstNonBlank(addendum.LegalDocumentNumber, addendum.AddendumNumber) ?? $"PL-{addendum.Id:D4}";
            var templateCode = FirstNonBlank(addendum.DocumentTemplateCode, "CONTRACT_ADDENDUM") ?? "CONTRACT_ADDENDUM";

            return new ContractDocumentPreviewDto
            {
                ReferenceId = addendum.Id,
                ReferenceType = "ContractAddendum",
                TemplateCode = templateCode,
                DocumentNumber = documentNumber,
                FileName = $"{SafeFileName(documentNumber)}.doc",
                DocFilePath = addendum.DocumentDocFilePath,
                PdfFilePath = addendum.DocumentPdfFilePath,
                Html = BuildAddendumDocumentHtml(addendum, documentNumber)
            };
        }

        private static string BuildAddendumDocumentHtml(ContractAddendum addendum, string documentNumber)
        {
            var contract = addendum.Contract ?? throw new InvalidOperationException("Phụ lục chưa liên kết hợp đồng gốc.");
            var snapshot = contract.LegalSnapshots
                .OrderByDescending(s => s.Version)
                .ThenByDescending(s => s.CreatedAt)
                .FirstOrDefault();

            var employerName = FirstNonBlank(snapshot?.EmployerLegalName, "CÔNG TY TNHH PHẦN MỀM HICAS");
            var employerTaxCode = snapshot?.EmployerTaxCode;
            var employerAddress = snapshot?.EmployerAddress;
            var employeeName = FirstNonBlank(snapshot?.EmployeeFullNameSnapshot, contract.Employee?.FullName);
            var representativeName = snapshot?.EmployerRepresentativeName;
            var representativeTitle = snapshot?.EmployerRepresentativeTitle;
            var issuedPlace = snapshot?.SigningLocation;
            var issuedAt = addendum.IssuedAt ?? DateTime.UtcNow;

            var detailsHtml = addendum.Details.Any()
                ? string.Join("", addendum.Details.OrderBy(d => d.Id).Select(d =>
                    $"<tr><td>{Html(DisplayFieldName(d.FieldName))}</td><td>{Html(FormatDetailValue(d.OldValue))}</td><td>{Html(FormatDetailValue(d.NewValue))}</td></tr>"))
                : "<tr><td colspan=\"3\">Theo nội dung thống nhất tại phụ lục này.</td></tr>";

            var body = $@"
<h1>PHỤ LỤC HỢP ĐỒNG LAO ĐỘNG</h1>
<p class=""doc-basis"">Căn cứ hợp đồng lao động số {Html(FirstNonBlank(addendum.BaseContractNumberSnapshot, contract.ContractNumber))} và thỏa thuận giữa các bên, hai bên thống nhất lập phụ lục này.</p>
<h2>Điều 1. Thông tin phụ lục</h2>
<p>Số phụ lục: <strong>{Html(documentNumber)}</strong>. Loại phụ lục: {Html(DisplayAddendumType(addendum.AddendumType))}. Ngày hiệu lực: {DateText(addendum.EffectiveDate)}.</p>
<p>Hợp đồng gốc: {Html(FirstNonBlank(addendum.BaseContractNumberSnapshot, contract.ContractNumber))}; thời hạn từ {DateText(addendum.BaseContractStartDateSnapshot ?? contract.StartDate)} đến {DateText(addendum.BaseContractEndDateSnapshot ?? contract.EndDate)}.</p>
<h2>Điều 2. Nội dung thay đổi</h2>
<p>{Paragraph(FirstNonBlank(addendum.ChangedContentSummary, addendum.Content))}</p>
<table border=""1"" cellspacing=""0"" cellpadding=""6"" style=""border-collapse:collapse;width:100%;margin-top:8px"">
  <thead><tr><th>Nội dung</th><th>Giá trị cũ</th><th>Giá trị mới</th></tr></thead>
  <tbody>{detailsHtml}</tbody>
</table>
<h2>Điều 3. Điều khoản giữ nguyên</h2>
<p>{Paragraph(addendum.UnchangedTerms)}</p>
<h2>Điều 4. Hiệu lực và thực hiện</h2>
<p>Phụ lục này là một phần không tách rời của hợp đồng lao động gốc. Các bên có trách nhiệm thực hiện đúng nội dung đã thỏa thuận kể từ ngày hiệu lực.</p>";

            return WrapLegalDocument(
                "PHỤ LỤC HỢP ĐỒNG LAO ĐỘNG",
                documentNumber,
                issuedPlace,
                issuedAt,
                employerName,
                employerTaxCode,
                employerAddress,
                body,
                "NGƯỜI LAO ĐỘNG",
                employeeName,
                $"ĐẠI DIỆN {FirstNonBlank(employerName, "CÔNG TY")}",
                representativeName,
                representativeTitle);
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
    table {{ font-size: 13px; }}
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

        private static string? FirstNonBlank(params string?[] values) =>
            values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

        private static string Html(string? value) =>
            WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(value) ? "-" : value.Trim());

        private static string Paragraph(string? value) =>
            Html(value).Replace("\r\n", "<br/>").Replace("\n", "<br/>");

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

        private static string DisplayAddendumType(ContractAddendumType type) =>
            type switch
            {
                ContractAddendumType.SalaryAdjustment => "Điều chỉnh lương",
                ContractAddendumType.Extension => "Gia hạn hợp đồng",
                ContractAddendumType.InternalTransfer => "Điều chuyển nội bộ",
                ContractAddendumType.SeniorAppointment => "Bổ nhiệm/chức danh",
                _ => "Nội dung khác"
            };

        private static string DisplayFieldName(string fieldName) =>
            fieldName switch
            {
                "BasicSalary" => "Lương cơ bản",
                "InsuranceSalary" => "Lương đóng bảo hiểm",
                "EndDate" => "Ngày kết thúc hợp đồng",
                "deptId" => "Phòng ban",
                "positionId" => "Chức danh/vị trí",
                "jobLevelId" => "Cấp bậc công việc",
                _ => fieldName
            };

        private static string FormatDetailValue(string? value) =>
            string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

        private static void ValidateDraft(CreateContractAddendumDto dto)
        {
            if (dto.NewEndDate.HasValue)
                throw new ArgumentException(GetContractRenewalMessage());
            if (IsContractRenewalType(dto.AddendumType))
                throw new ArgumentException(GetContractRenewalMessage());
            if (ContainsContractRenewalJsonKey(dto.OtherChangesJson))
                throw new ArgumentException(GetContractRenewalMessage());

            if (dto.EffectiveDate == default)
                throw new ArgumentException("Ngày hiệu lực phụ lục không hợp lệ.");
            if (dto.NewBasicSalary is <= 0)
                throw new ArgumentException("Lương cơ bản mới phải lớn hơn 0.");
            if (dto.NewInsuranceSalary is < 0)
                throw new ArgumentException("Lương đóng bảo hiểm mới không được âm.");
            if (dto.NewBasicSalary == null &&
                dto.NewInsuranceSalary == null &&
                string.IsNullOrWhiteSpace(dto.OtherChangesJson) &&
                string.IsNullOrWhiteSpace(dto.Content) &&
                string.IsNullOrWhiteSpace(dto.ChangedContentSummary))
                throw new ArgumentException("Phụ lục cần có ít nhất một nội dung điều chỉnh.");
        }

        private static void ValidateDraftAgainstContract(CreateContractAddendumDto dto, Contract contract)
        {
            if (contract.EndDate.HasValue && dto.EffectiveDate.Date > contract.EndDate.Value.Date)
                throw new ArgumentException("Ngay hieu luc phu luc khong duoc sau ngay ket thuc hop dong goc.");

            if (ContainsFullContractRewriteJsonKey(dto.OtherChangesJson) ||
                CountAddendumChangeItems(dto) > 8)
                throw new ArgumentException("Noi dung phu luc thay doi qua rong. Vui long tao hop dong moi hoac tai ky neu can thay doi gan nhu toan bo hop dong.");
        }

        private static string GetContractRenewalMessage() =>
            "Phu luc khong duoc dieu chinh ngay bat dau, ngay ket thuc, loai hop dong hoac thoi han/gia han hop dong. Vui long tao luong hop dong moi, gia han hoac tai ky.";

        private static bool IsContractRenewalType(string? addendumType)
        {
            if (string.IsNullOrWhiteSpace(addendumType))
                return false;

            return addendumType.Equals("Extension", StringComparison.OrdinalIgnoreCase) ||
                   addendumType.Equals("Renewal", StringComparison.OrdinalIgnoreCase) ||
                   addendumType.Equals("ContractRenewal", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsContractRenewalJsonKey(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                using var doc = JsonDocument.Parse(json);
                return ContainsContractRenewalJsonKey(doc.RootElement);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("OtherChangesJson khong hop le.", ex);
            }
        }

        private static bool ContainsContractRenewalJsonKey(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (ContractRenewalJsonKeys.Contains(property.Name) ||
                        ContainsContractRenewalJsonKey(property.Value))
                        return true;
                }
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    if (ContainsContractRenewalJsonKey(item))
                        return true;
                }
            }

            return false;
        }

        private static bool ContainsFullContractRewriteJsonKey(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                using var doc = JsonDocument.Parse(json);
                return ContainsFullContractRewriteJsonKey(doc.RootElement);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("OtherChangesJson khong hop le.", ex);
            }
        }

        private static bool ContainsFullContractRewriteJsonKey(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (FullContractRewriteJsonKeys.Contains(property.Name) ||
                        ContainsFullContractRewriteJsonKey(property.Value))
                        return true;
                }
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    if (ContainsFullContractRewriteJsonKey(item))
                        return true;
                }
            }

            return false;
        }

        private static int CountAddendumChangeItems(CreateContractAddendumDto dto)
        {
            var count = 0;
            if (dto.NewBasicSalary.HasValue) count++;
            if (dto.NewInsuranceSalary.HasValue) count++;
            if (!string.IsNullOrWhiteSpace(dto.Content)) count++;
            if (!string.IsNullOrWhiteSpace(dto.ChangedContentSummary)) count++;
            count += CountTopLevelJsonProperties(dto.OtherChangesJson);
            return count;
        }

        private static int CountTopLevelJsonProperties(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return 0;

            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.ValueKind == JsonValueKind.Object
                    ? doc.RootElement.EnumerateObject().Count()
                    : 1;
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("OtherChangesJson khong hop le.", ex);
            }
        }

        private static string? NormalizeJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement);
        }

        private static void ApplyDraftFields(ContractAddendum addendum, CreateContractAddendumDto dto, Contract? contract)
        {
            var normalizedOtherChanges = NormalizeJson(dto.OtherChangesJson);
            addendum.OtherChangesJson = normalizedOtherChanges;
            addendum.AddendumType = ResolveAddendumType(dto, addendum);
            addendum.BaseContractNumberSnapshot = contract?.ContractNumber;
            addendum.BaseContractStartDateSnapshot = contract?.StartDate;
            addendum.BaseContractEndDateSnapshot = contract?.EndDate;
            addendum.NewBasicSalary = dto.NewBasicSalary;
            addendum.NewInsuranceSalary = dto.NewInsuranceSalary;
            addendum.NewEndDate = null;
            addendum.Content = string.IsNullOrWhiteSpace(dto.Content) ? null : dto.Content.Trim();
            addendum.UnchangedTerms = string.IsNullOrWhiteSpace(dto.UnchangedTerms)
                ? "Các điều khoản khác của hợp đồng lao động gốc không thay đổi và tiếp tục có hiệu lực. Các khoản thưởng KPI, phụ cấp và thu nhập biến động khác tiếp tục áp dụng theo quy chế lương thưởng hiện hành của công ty, trừ khi phụ lục này quy định khác."
                : dto.UnchangedTerms.Trim();
            addendum.EffectiveDate = dto.EffectiveDate;
            addendum.DocumentTemplateCode = "CONTRACT_ADDENDUM";

            if (dto.NewBasicSalary.HasValue)
            {
                AddDetail(
                    addendum,
                    "BasicSalary",
                    contract?.BasicSalary.ToString(CultureInfo.InvariantCulture),
                    dto.NewBasicSalary.Value.ToString(CultureInfo.InvariantCulture),
                    ContractAddendumDetailValueType.Money);
            }

            if (dto.NewInsuranceSalary.HasValue)
            {
                AddDetail(
                    addendum,
                    "InsuranceSalary",
                    contract?.InsuranceSalary.ToString(CultureInfo.InvariantCulture),
                    dto.NewInsuranceSalary.Value.ToString(CultureInfo.InvariantCulture),
                    ContractAddendumDetailValueType.Money);
            }

            AppendOtherChangeDetails(addendum, addendum.OtherChangesJson);
            addendum.ChangedContentSummary = string.IsNullOrWhiteSpace(dto.ChangedContentSummary)
                ? BuildChangedContentSummary(addendum)
                : dto.ChangedContentSummary.Trim();
        }

        private static ContractAddendumType ResolveAddendumType(CreateContractAddendumDto dto, ContractAddendum addendum)
        {
            if (!string.IsNullOrWhiteSpace(dto.AddendumType) &&
                Enum.TryParse<ContractAddendumType>(dto.AddendumType, true, out var parsed))
                return parsed;

            if (dto.NewBasicSalary.HasValue || dto.NewInsuranceSalary.HasValue)
                return ContractAddendumType.SalaryAdjustment;

            var json = addendum.OtherChangesJson;
            if (!string.IsNullOrWhiteSpace(json))
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("deptId", out _) || doc.RootElement.TryGetProperty("positionId", out _))
                    return ContractAddendumType.InternalTransfer;
                if (doc.RootElement.TryGetProperty("jobLevelId", out _))
                    return ContractAddendumType.SeniorAppointment;
            }

            return ContractAddendumType.Other;
        }

        private static string BuildChangedContentSummary(ContractAddendum addendum)
        {
            var parts = addendum.Details
                .Select(detail => $"{DisplayFieldName(detail.FieldName)}: {FormatDetailValue(detail.OldValue)} -> {FormatDetailValue(detail.NewValue)}")
                .ToList();

            return parts.Count == 0
                ? "Các bên thống nhất điều chỉnh nội dung theo phụ lục này."
                : string.Join("; ", parts);
        }

        private static void AddDetail(
            ContractAddendum addendum,
            string fieldName,
            string? oldValue,
            string? newValue,
            ContractAddendumDetailValueType valueType,
            string? note = null)
        {
            addendum.Details.Add(new ContractAddendumDetail
            {
                FieldName = fieldName,
                OldValue = oldValue,
                NewValue = newValue,
                ValueType = valueType,
                Note = note
            });
        }

        private static void AppendOtherChangeDetails(ContractAddendum addendum, string? normalizedJson)
        {
            if (string.IsNullOrWhiteSpace(normalizedJson)) return;

            using var doc = JsonDocument.Parse(normalizedJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return;

            foreach (var property in doc.RootElement.EnumerateObject())
            {
                AddDetail(
                    addendum,
                    property.Name,
                    null,
                    property.Value.ToString(),
                    ResolveJsonValueType(property.Value),
                    "OtherChangesJson");
            }
        }

        private static ContractAddendumDetailValueType ResolveJsonValueType(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.Number => ContractAddendumDetailValueType.Number,
                JsonValueKind.True or JsonValueKind.False => ContractAddendumDetailValueType.Boolean,
                JsonValueKind.Object or JsonValueKind.Array => ContractAddendumDetailValueType.Json,
                _ => ContractAddendumDetailValueType.Text
            };
        }

        private static string GenerateAddendumNumber(int contractId)
        {
            return $"PL-{DateTime.UtcNow:yyyyMMddHHmmss}-{contractId:D4}";
        }

        private async Task ApplySalaryChangesAsync(ContractAddendum addendum, Contract contract, int employeeId, CancellationToken ct)
        {
            if (addendum.NewBasicSalary.HasValue && addendum.NewBasicSalary.Value != contract.BasicSalary)
            {
                await AddHistoryAsync(
                    employeeId,
                    HistoryType.Salary_Change,
                    $"BasicSalary: {contract.BasicSalary:N0}",
                    $"BasicSalary: {addendum.NewBasicSalary.Value:N0} (Addendum {addendum.AddendumNumber})",
                    addendum.EffectiveDate,
                    ct);
                contract.BasicSalary = addendum.NewBasicSalary.Value;
            }

            if (addendum.NewInsuranceSalary.HasValue && addendum.NewInsuranceSalary.Value != contract.InsuranceSalary)
            {
                await AddHistoryAsync(
                    employeeId,
                    HistoryType.Salary_Change,
                    $"InsuranceSalary: {contract.InsuranceSalary:N0}",
                    $"InsuranceSalary: {addendum.NewInsuranceSalary.Value:N0} (Addendum {addendum.AddendumNumber})",
                    addendum.EffectiveDate,
                    ct);
                contract.InsuranceSalary = addendum.NewInsuranceSalary.Value;
            }
        }

        private async Task ApplyOtherChangesAsync(ContractAddendum addendum, Employee employee, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(addendum.OtherChangesJson)) return;

            using var doc = JsonDocument.Parse(addendum.OtherChangesJson);
            var root = doc.RootElement;

            if (TryGetInt(root, "deptId", out var newDeptId) && newDeptId != employee.DeptId)
            {
                await AddHistoryAsync(
                    employee.Id,
                    HistoryType.Transfer,
                    $"DeptId: {employee.DeptId?.ToString() ?? "null"}",
                    $"DeptId: {newDeptId} (Addendum {addendum.AddendumNumber})",
                    addendum.EffectiveDate,
                    ct);
                employee.DeptId = newDeptId;
            }

            if (TryGetInt(root, "positionId", out var newPositionId) && newPositionId != employee.PositionId)
            {
                await AddHistoryAsync(
                    employee.Id,
                    HistoryType.Appointment,
                    $"PositionId: {employee.PositionId?.ToString() ?? "null"}",
                    $"PositionId: {newPositionId} (Addendum {addendum.AddendumNumber})",
                    addendum.EffectiveDate,
                    ct);
                employee.PositionId = newPositionId;
            }

            if (TryGetInt(root, "jobLevelId", out var newJobLevelId) && newJobLevelId != employee.JobLevelId)
            {
                await AddHistoryAsync(
                    employee.Id,
                    HistoryType.Appointment,
                    $"JobLevelId: {employee.JobLevelId?.ToString() ?? "null"}",
                    $"JobLevelId: {newJobLevelId} (Addendum {addendum.AddendumNumber})",
                    addendum.EffectiveDate,
                    ct);
                employee.JobLevelId = newJobLevelId;
            }
        }

        private async Task AddHistoryAsync(
            int employeeId,
            HistoryType type,
            string oldValue,
            string newValue,
            DateTime effectiveDate,
            CancellationToken ct)
        {
            await _historyRepo.AddAsync(new EmploymentHistory
            {
                EmployeeId = employeeId,
                Type = type,
                OldValue = oldValue,
                NewValue = newValue,
                EffectiveDate = effectiveDate,
                ChangeDate = DateTime.UtcNow
            }, ct);
        }

        private static bool TryGetInt(JsonElement root, string propertyName, out int value)
        {
            value = 0;
            if (!root.TryGetProperty(propertyName, out var property)) return false;
            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out value)) return true;
            return property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out value);
        }

        private static string FormatDate(DateTime? value) =>
            value.HasValue ? value.Value.ToString("yyyy-MM-dd") : "null";

        private async Task EnsureManagerCanAccessAsync(ContractAddendum addendum, int actorAccountId, string actorRoleName, CancellationToken ct)
        {
            if (IsAdmin(actorRoleName))
                return;

            if (!IsManager(actorRoleName))
                throw new UnauthorizedAccessException("Chỉ Trưởng phòng được xác nhận nghiệp vụ phụ lục.");

            var manager = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct)
                ?? throw new UnauthorizedAccessException("Tài khoản Trưởng phòng chưa liên kết hồ sơ nhân sự.");

            if (addendum.Contract?.Employee?.Department?.Manager?.AccountId != actorAccountId)
                throw new UnauthorizedAccessException("Trưởng phòng chỉ được xác nhận phụ lục của nhân viên trong phòng ban mình.");
        }

        private static int GetTargetEmployeeId(ContractAddendum addendum)
        {
            return addendum.Contract?.EmployeeId
                ?? throw new InvalidOperationException("Phụ lục chưa gắn với nhân viên.");
        }

        private async Task EnsureEmployeeOwnsAddendumAsync(ContractAddendum addendum, int actorAccountId, CancellationToken ct)
        {
            var employeeId = GetTargetEmployeeId(addendum);
            var employee = await _employeeRepo.GetProfileByIdAsync(employeeId, ct)
                ?? throw new InvalidOperationException("Không tìm thấy nhân viên của phụ lục.");

            if (!employee.AccountId.HasValue || employee.AccountId.Value != actorAccountId)
                throw new UnauthorizedAccessException("Chỉ người lao động của phụ lục mới được xác nhận điều khoản.");
        }

        private static void EnsureHrDirectorOrAdmin(string actorRoleName)
        {
            if (!IsHr(actorRoleName) && !IsDirector(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chỉ HR, Giám đốc hoặc Admin được xác nhận chính sách phụ lục.");
        }

        private static void EnsureDirectorOrAdmin(string actorRoleName)
        {
            if (!IsDirector(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chỉ Giám đốc hoặc Admin được phê duyệt cuối phụ lục.");
        }

        private static bool IsAdmin(string role) => string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        private static bool IsManager(string? role) => string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase);
        private static bool IsHr(string? role) => string.Equals(role, "HR", StringComparison.OrdinalIgnoreCase);
        private static bool IsDirector(string? role) => string.Equals(role, "Director", StringComparison.OrdinalIgnoreCase);

        private static ContractAddendumResponseDto Map(ContractAddendum addendum) => new()
        {
            Id = addendum.Id,
            ContractId = addendum.ContractId,
            ContractNumber = addendum.Contract?.ContractNumber ?? string.Empty,
            AddendumNumber = addendum.AddendumNumber,
            AddendumType = addendum.AddendumType.ToString(),
            BaseContractNumberSnapshot = addendum.BaseContractNumberSnapshot,
            BaseContractStartDateSnapshot = addendum.BaseContractStartDateSnapshot,
            BaseContractEndDateSnapshot = addendum.BaseContractEndDateSnapshot,
            NewBasicSalary = addendum.NewBasicSalary,
            NewInsuranceSalary = addendum.NewInsuranceSalary,
            NewEndDate = addendum.NewEndDate,
            OtherChangesJson = addendum.OtherChangesJson,
            Content = addendum.Content,
            ChangedContentSummary = addendum.ChangedContentSummary,
            UnchangedTerms = addendum.UnchangedTerms,
            LegalDocumentNumber = addendum.LegalDocumentNumber,
            DocumentTemplateCode = addendum.DocumentTemplateCode,
            DocumentDocFilePath = addendum.DocumentDocFilePath,
            DocumentPdfFilePath = addendum.DocumentPdfFilePath,
            IssuedAt = addendum.IssuedAt,
            EmployeeSignedAt = addendum.EmployeeSignedAt,
            EmployerSignedAt = addendum.EmployerSignedAt,
            EffectiveDate = addendum.EffectiveDate,
            Status = addendum.Status.ToString(),
            RejectReason = addendum.RejectReason,
            CreatedAt = addendum.CreatedAt,
            EmployeeId = addendum.Contract?.EmployeeId,
            EmployeeName = addendum.Contract?.Employee?.FullName,
            Details = addendum.Details
                .OrderBy(d => d.Id)
                .Select(d => new ContractAddendumDetailDto
                {
                    Id = d.Id,
                    FieldName = d.FieldName,
                    OldValue = d.OldValue,
                    NewValue = d.NewValue,
                    ValueType = d.ValueType.ToString(),
                    Note = d.Note
                })
                .ToList()
        };
    }
}
