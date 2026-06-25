using HRM.backend.src.HRM.Application.DTOs.PersonnelChanges;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.Services;
using HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.UseCases;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.PersonnelChanges;
using HRM.backend.src.HRM.Core.Entities.WorkflowRequests;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PersonnelChanges;

namespace HRM.backend.src.HRM.Application.UseCases.PersonnelChanges
{
    public class InternalTransferUseCase : IInternalTransferUseCase
    {
        private readonly IPersonnelChangeRepository _personnelChangeRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IBaseRepository<EmploymentHistory> _historyRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;
        private readonly PersonnelChangeRiskSummaryBuilder _riskSummaryBuilder;
        private readonly IPersonnelChangeAccessGuard _accessGuard;
        private readonly IPersonnelChangeContractFlowService _contractFlowService;
        private readonly IPersonnelChangeUseCase _personnelChangeUseCase;

        public InternalTransferUseCase(
            IPersonnelChangeRepository personnelChangeRepo,
            IEmployeeRepository employeeRepo,
            IBaseRepository<EmploymentHistory> historyRepo,
            IUnitOfWork unitOfWork,
            ILockService lockService,
            PersonnelChangeRiskSummaryBuilder riskSummaryBuilder,
            IPersonnelChangeAccessGuard accessGuard,
            IPersonnelChangeContractFlowService contractFlowService,
            IPersonnelChangeUseCase personnelChangeUseCase)
        {
            _personnelChangeRepo = personnelChangeRepo;
            _employeeRepo = employeeRepo;
            _historyRepo = historyRepo;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
            _riskSummaryBuilder = riskSummaryBuilder;
            _accessGuard = accessGuard;
            _contractFlowService = contractFlowService;
            _personnelChangeUseCase = personnelChangeUseCase;
        }

        public async Task<PersonnelChangeDetailDto> CreateInternalTransferDemandAsync(
            InternalTransferDemandDto dto,
            int actorAccountId,
            CancellationToken ct)
        {
            await _accessGuard.EnsureActorHasRoleAsync(actorAccountId, ct, "HR", "Manager", "Admin");

            if (dto.RequestedDepartmentId <= 0)
                throw new ArgumentException("Requested department is required.");

            await _accessGuard.EnsurePlacementReferencesAsync(
                dto.RequestedDepartmentId,
                dto.RequestedPositionId,
                dto.RequestedManagerId,
                null,
                actorAccountId,
                ct);

            var request = new PersonnelChangeRequest
            {
                ChangeType = PersonnelChangeType.InternalTransfer,
                Status = PersonnelChangeStatus.PendingHRReview,
                RequestedByAccountId = actorAccountId,
                RequestedAt = DateTime.UtcNow,
                Reason = BuildDemandReason(dto),
                EffectiveDate = dto.ExpectedEffectiveDate,
                NewDepartmentId = dto.RequestedDepartmentId,
                NewPositionId = dto.RequestedPositionId,
                NewManagerId = dto.RequestedManagerId,
                RequiresEmployeeConsent = true,
                EmployeeConsentStatus = PersonnelChangeConsentStatus.Pending,
                RequiresDirectorApproval = true,
                RequiresHRProcessing = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                await _personnelChangeRepo.AddAsync(request, ct);
                await _unitOfWork.CommitAsync(ct);
            }, ct);

            await AddHistoryAndSnapshotAsync(
                request.Id,
                "InternalTransferDemandCreated",
                null,
                request.Status,
                actorAccountId,
                request.Reason,
                ct);

            return await _personnelChangeUseCase.GetDetailAsync(request.Id, ct);
        }

        public Task<PersonnelChangeDetailDto> HrSelectEmployeeAsync(
            int id,
            int actorAccountId,
            HrSelectEmployeeDto dto,
            CancellationToken ct)
        {
            if (dto.EmployeeId <= 0)
                throw new ArgumentException("Employee is required.");

            return MutateInternalTransferAsync(id, actorAccountId, async (request, innerCt) =>
            {
                await _accessGuard.EnsureActorHasRoleAsync(actorAccountId, innerCt, "HR", "Admin");
                EnsureStatus(request, PersonnelChangeStatus.PendingHRReview);

                var employee = await _accessGuard.EnsureCanAccessEmployeeAsync(dto.EmployeeId, actorAccountId, innerCt);
                await _accessGuard.EnsurePlacementReferencesAsync(
                    dto.NewDepartmentId ?? request.NewDepartmentId,
                    dto.NewPositionId ?? request.NewPositionId,
                    dto.NewManagerId ?? request.NewManagerId,
                    dto.NewJobLevelId ?? request.NewJobLevelId,
                    actorAccountId,
                    innerCt);

                var oldStatus = request.Status;
                request.EmployeeId = employee.Id;
                request.CurrentDepartmentId = employee.DeptId;
                request.CurrentPositionId = employee.PositionId;
                request.CurrentManagerId = employee.ManagerId;
                request.CurrentJobLevelId = employee.JobLevelId;
                request.CurrentEmployeeType = employee.Type;

                request.NewDepartmentId = dto.NewDepartmentId ?? request.NewDepartmentId;
                request.NewPositionId = dto.NewPositionId ?? request.NewPositionId;
                request.NewManagerId = dto.NewManagerId ?? request.NewManagerId;
                request.NewJobLevelId = dto.NewJobLevelId ?? request.NewJobLevelId;
                request.RequiresContractFlow = dto.RequiresContractAddendum;
                request.ContractFlowType = dto.RequiresContractAddendum
                    ? PersonnelChangeContractFlowType.ContractAddendum
                    : PersonnelChangeContractFlowType.None;
                request.ContractFlowStatus = dto.RequiresContractAddendum ? "Pending" : null;
                request.HRAssignedAccountId = actorAccountId;
                request.HRNote = dto.Note;
                request.HRProcessedAt = DateTime.UtcNow;
                request.Status = PersonnelChangeStatus.PendingCurrentManagerOpinion;

                await AddApprovalAsync(request.Id, "HRSelectEmployee", "HR", actorAccountId, true, dto.Note, innerCt);
                await AddHistoryAsync(request.Id, "HRSelectedEmployee", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
            }, ct);
        }

        public Task<PersonnelChangeDetailDto> SubmitCurrentManagerOpinionAsync(
            int id,
            int actorAccountId,
            CurrentManagerOpinionDto dto,
            CancellationToken ct)
        {
            return MutateInternalTransferAsync(id, actorAccountId, async (request, innerCt) =>
            {
                EnsureStatus(request, PersonnelChangeStatus.PendingCurrentManagerOpinion);
                await _accessGuard.EnsureCurrentManagerCanActAsync(
                    request.CurrentManagerId,
                    actorAccountId,
                    "submit this opinion",
                    innerCt);

                var oldStatus = request.Status;
                request.Status = dto.IsApproved
                    ? PersonnelChangeStatus.PendingEmployeeConsent
                    : PersonnelChangeStatus.Rejected;
                if (!dto.IsApproved)
                    request.RejectedReason = dto.Opinion;

                await AddApprovalAsync(request.Id, "CurrentManagerOpinion", "Manager", actorAccountId, dto.IsApproved, dto.Opinion, innerCt);
                await AddHistoryAsync(request.Id, "CurrentManagerOpinionSubmitted", oldStatus, request.Status, actorAccountId, dto.Opinion, innerCt);
            }, ct);
        }

        public Task<PersonnelChangeDetailDto> SubmitEmployeeConsentAsync(
            int id,
            int actorAccountId,
            EmployeeConsentDto dto,
            CancellationToken ct)
        {
            return MutateInternalTransferAsync(id, actorAccountId, async (request, innerCt) =>
            {
                EnsureStatus(request, PersonnelChangeStatus.PendingEmployeeConsent);
                if (!request.EmployeeId.HasValue)
                    throw new InvalidOperationException("Internal transfer has no selected employee.");
                await _accessGuard.EnsureEmployeeAccountCanActAsync(
                    request.EmployeeId.Value,
                    actorAccountId,
                    "submit consent",
                    innerCt);

                var oldStatus = request.Status;
                request.EmployeeConsentAt = DateTime.UtcNow;
                request.EmployeeConsentNote = dto.Note;
                request.EmployeeConsentStatus = dto.IsAccepted
                    ? PersonnelChangeConsentStatus.Accepted
                    : PersonnelChangeConsentStatus.Declined;
                request.Status = dto.IsAccepted
                    ? PersonnelChangeStatus.PendingDirectorApproval
                    : PersonnelChangeStatus.EmployeeDeclined;

                if (!dto.IsAccepted)
                    request.RejectedReason = dto.Note;

                await AddHistoryAsync(request.Id, "InternalTransferEmployeeConsentSubmitted", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
            }, ct);
        }

        public Task<PersonnelChangeDetailDto> DirectorApproveTransferAsync(
            int id,
            int actorAccountId,
            DirectorApproveTransferDto dto,
            CancellationToken ct)
        {
            return MutateInternalTransferAsync(id, actorAccountId, async (request, innerCt) =>
            {
                await _accessGuard.EnsureActorHasRoleAsync(actorAccountId, innerCt, "Director", "Admin");
                EnsureStatus(request, PersonnelChangeStatus.PendingDirectorApproval);

                var oldStatus = request.Status;
                request.DirectorApprovedByAccountId = dto.IsApproved ? actorAccountId : null;
                request.DirectorApprovedAt = dto.IsApproved ? DateTime.UtcNow : null;
                request.DirectorNote = dto.Note;

                if (dto.IsApproved)
                {
                    request.Status = request.RequiresContractFlow
                        ? PersonnelChangeStatus.PendingContractFlow
                        : PersonnelChangeStatus.ApprovedByDirector;
                }
                else
                {
                    request.Status = PersonnelChangeStatus.Rejected;
                    request.RejectedReason = dto.Note;
                }

                await AddApprovalAsync(request.Id, "DirectorApproveTransfer", "Director", actorAccountId, dto.IsApproved, dto.Note, innerCt);
                await AddHistoryAsync(request.Id, "DirectorApprovedTransfer", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
            }, ct);
        }

        public Task<PersonnelChangeDetailDto> IssueTransferDecisionAsync(
            int id,
            int actorAccountId,
            IssueTransferDecisionDto dto,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.DecisionNumber))
                throw new ArgumentException("Decision number is required.");

            return MutateInternalTransferAsync(id, actorAccountId, async (request, innerCt) =>
            {
                await _accessGuard.EnsureActorHasRoleAsync(actorAccountId, innerCt, "HR", "Admin");
                EnsureStatus(
                    request,
                    PersonnelChangeStatus.ApprovedByDirector,
                    PersonnelChangeStatus.PendingDecisionIssuance,
                    PersonnelChangeStatus.ReadyToExecute);
                _contractFlowService.EnsureCanExecute(request);

                var oldStatus = request.Status;
                request.DecisionNumber = dto.DecisionNumber.Trim();
                request.DecisionFilePath = dto.DecisionFilePath;
                request.DecisionIssuedAt = dto.DecisionIssuedAt ?? DateTime.UtcNow;
                request.Status = PersonnelChangeStatus.ReadyToExecute;

                await AddHistoryAsync(request.Id, "TransferDecisionIssued", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
            }, ct);
        }

        public Task<PersonnelChangeDetailDto> ExecuteInternalTransferAsync(
            int id,
            int actorAccountId,
            ExecutePersonnelChangeDto dto,
            CancellationToken ct)
        {
            return MutateInternalTransferAsync(id, actorAccountId, async (request, innerCt) =>
            {
                await _accessGuard.EnsureActorHasRoleAsync(actorAccountId, innerCt, "HR", "Admin");
                EnsureStatus(request, PersonnelChangeStatus.ReadyToExecute);
                _contractFlowService.EnsureCanExecute(request);

                if (!request.EmployeeId.HasValue)
                    throw new InvalidOperationException("Internal transfer requires a selected employee before execution.");

                var employee = await _employeeRepo.GetByIdAsync(request.EmployeeId.Value, innerCt)
                    ?? throw new KeyNotFoundException("Employee was not found.");

                await ApplyTransferAsync(request, employee, actorAccountId, innerCt);

                var oldStatus = request.Status;
                request.Status = PersonnelChangeStatus.Completed;
                request.CompletedAt = dto.CompletedAt ?? DateTime.UtcNow;

                await AddHistoryAsync(request.Id, "InternalTransferExecuted", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
            }, ct);
        }

        private async Task<PersonnelChangeDetailDto> MutateInternalTransferAsync(
            int id,
            int actorAccountId,
            Func<PersonnelChangeRequest, CancellationToken, Task> mutation,
            CancellationToken ct)
        {
            await _lockService.GetWithLockAsync($"personnel_change_{id}", async innerCt =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var request = await _personnelChangeRepo.GetDetailAsync(id, innerCt)
                        ?? throw new KeyNotFoundException("Personnel change request was not found.");

                    EnsureInternalTransfer(request);
                    await mutation(request, innerCt);

                    if (request.Status == PersonnelChangeStatus.PendingContractFlow &&
                        request.RequiresContractFlow &&
                        !request.ContractLinks.Any())
                    {
                        await _contractFlowService.CreateContractFlowAsync(request, innerCt);
                    }

                    request.UpdatedAt = DateTime.UtcNow;
                    _personnelChangeRepo.Update(request);
                    await _unitOfWork.CommitAsync(innerCt);
                }, innerCt);

                return true;
            }, cancellationToken: ct);

            await SaveSnapshotAsync(id, actorAccountId, ct);
            return await _personnelChangeUseCase.GetDetailAsync(id, ct);
        }

        private async Task ApplyTransferAsync(
            PersonnelChangeRequest request,
            Employee employee,
            int actorAccountId,
            CancellationToken ct)
        {
            var effectiveDate = request.EffectiveDate ?? DateTime.UtcNow.Date;

            if (request.NewDepartmentId.HasValue && request.NewDepartmentId != employee.DeptId)
            {
                await AddEmploymentHistoryAsync(
                    employee.Id,
                    HistoryType.Transfer,
                    $"DeptId: {employee.DeptId?.ToString() ?? "null"}",
                    $"DeptId: {request.NewDepartmentId.Value}",
                    effectiveDate,
                    ct);
                employee.DeptId = request.NewDepartmentId.Value;
            }

            if (request.NewPositionId.HasValue && request.NewPositionId != employee.PositionId)
            {
                await AddEmploymentHistoryAsync(
                    employee.Id,
                    HistoryType.Appointment,
                    $"PositionId: {employee.PositionId?.ToString() ?? "null"}",
                    $"PositionId: {request.NewPositionId.Value}",
                    effectiveDate,
                    ct);
                employee.PositionId = request.NewPositionId.Value;
            }

            if (request.NewJobLevelId.HasValue && request.NewJobLevelId != employee.JobLevelId)
            {
                await AddEmploymentHistoryAsync(
                    employee.Id,
                    HistoryType.Appointment,
                    $"JobLevelId: {employee.JobLevelId?.ToString() ?? "null"}",
                    $"JobLevelId: {request.NewJobLevelId.Value}",
                    effectiveDate,
                    ct);
                employee.JobLevelId = request.NewJobLevelId.Value;
            }

            if (request.NewManagerId.HasValue && request.NewManagerId != request.CurrentManagerId)
            {
                await AddEmploymentHistoryAsync(
                    employee.Id,
                    HistoryType.Transfer,
                    $"ManagerId: {request.CurrentManagerId?.ToString() ?? "null"}",
                    $"ManagerId: {request.NewManagerId.Value}",
                    effectiveDate,
                    ct);
                employee.ManagerId = request.NewManagerId.Value;
            }

            _employeeRepo.Update(employee);
        }

        private Task AddEmploymentHistoryAsync(
            int employeeId,
            HistoryType type,
            string oldValue,
            string newValue,
            DateTime effectiveDate,
            CancellationToken ct)
        {
            return _historyRepo.AddAsync(new EmploymentHistory
            {
                EmployeeId = employeeId,
                Type = type,
                OldValue = oldValue,
                NewValue = newValue,
                EffectiveDate = effectiveDate,
                ChangeDate = DateTime.UtcNow
            }, ct);
        }

        private async Task EnsureCurrentManagerCanReviewAsync(PersonnelChangeRequest request, int actorAccountId, CancellationToken ct)
        {
            if (!request.CurrentManagerId.HasValue)
                return;

            var manager = await _employeeRepo.GetByIdAsync(request.CurrentManagerId.Value, ct)
                ?? throw new KeyNotFoundException("Current manager was not found.");

            if (manager.AccountId.HasValue && manager.AccountId.Value != actorAccountId)
                throw new UnauthorizedAccessException("Only the current manager can submit this opinion.");
        }

        private async Task EnsureSelectedEmployeeCanConsentAsync(PersonnelChangeRequest request, int actorAccountId, CancellationToken ct)
        {
            if (!request.EmployeeId.HasValue)
                throw new InvalidOperationException("Internal transfer has no selected employee.");

            var employee = await _employeeRepo.GetByIdAsync(request.EmployeeId.Value, ct)
                ?? throw new KeyNotFoundException("Employee was not found.");

            if (employee.AccountId.HasValue && employee.AccountId.Value != actorAccountId)
                throw new UnauthorizedAccessException("Only the selected employee can submit consent.");
        }

        private async Task AddHistoryAndSnapshotAsync(
            int requestId,
            string action,
            PersonnelChangeStatus? oldStatus,
            PersonnelChangeStatus? newStatus,
            int actorAccountId,
            string? note,
            CancellationToken ct)
        {
            await AddHistoryAsync(requestId, action, oldStatus, newStatus, actorAccountId, note, ct);
            await _unitOfWork.CommitAsync(ct);
            await SaveSnapshotAsync(requestId, actorAccountId, ct);
        }

        private async Task SaveSnapshotAsync(int requestId, int actorAccountId, CancellationToken ct)
        {
            var snapshotJson = await _riskSummaryBuilder.BuildSnapshotJsonAsync(requestId, ct);
            await _personnelChangeRepo.AddRiskSnapshotAsync(new PersonnelChangeRiskSnapshot
            {
                RequestId = requestId,
                SnapshotJson = snapshotJson,
                CreatedByAccountId = actorAccountId,
                CreatedAt = DateTime.UtcNow
            }, ct);
            await _unitOfWork.CommitAsync(ct);
        }

        private Task AddApprovalAsync(
            int requestId,
            string stepName,
            string approverRole,
            int actorAccountId,
            bool isApproved,
            string? note,
            CancellationToken ct)
        {
            return _personnelChangeRepo.AddApprovalAsync(new PersonnelChangeApproval
            {
                RequestId = requestId,
                StepName = stepName,
                ApproverRole = approverRole,
                ApproverAccountId = actorAccountId,
                Decision = isApproved ? PersonnelChangeApprovalDecision.Approved : PersonnelChangeApprovalDecision.Rejected,
                Note = note,
                DecidedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }, ct);
        }

        private Task AddHistoryAsync(
            int requestId,
            string action,
            PersonnelChangeStatus? oldStatus,
            PersonnelChangeStatus? newStatus,
            int? actorAccountId,
            string? note,
            CancellationToken ct)
        {
            return _personnelChangeRepo.AddHistoryAsync(new PersonnelChangeHistory
            {
                RequestId = requestId,
                Action = action,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ActorAccountId = actorAccountId,
                Note = note,
                CreatedAt = DateTime.UtcNow
            }, ct);
        }

        private static void EnsureInternalTransfer(PersonnelChangeRequest request)
        {
            if (request.ChangeType != PersonnelChangeType.InternalTransfer)
                throw new InvalidOperationException("Request is not an internal transfer request.");
        }

        private static void EnsureStatus(PersonnelChangeRequest request, params PersonnelChangeStatus[] allowedStatuses)
        {
            if (!PersonnelChangeStatusGuard.IsAllowed(request, allowedStatuses))
                throw new InvalidOperationException($"Request is in status {request.Status}, expected one of: {PersonnelChangeStatusGuard.DescribeAllowed(allowedStatuses)}.");
        }

        private static string BuildDemandReason(InternalTransferDemandDto dto)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(dto.Reason))
                parts.Add(dto.Reason.Trim());
            if (!string.IsNullOrWhiteSpace(dto.UrgencyLevel))
                parts.Add($"Urgency: {dto.UrgencyLevel.Trim()}");
            if (!string.IsNullOrWhiteSpace(dto.RequiredSkills))
                parts.Add($"Required skills: {dto.RequiredSkills.Trim()}");

            return parts.Count == 0 ? "Internal transfer demand." : string.Join("\n", parts);
        }
    }
}
