using HRM.backend.src.HRM.Application.DTOs.PersonnelChanges;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.Services;
using HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.UseCases;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.PersonnelChanges;
using HRM.backend.src.HRM.Core.Entities.RequestHandover;
using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PersonnelChanges;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;

namespace HRM.backend.src.HRM.Application.UseCases.PersonnelChanges
{
    public class DismissalDisciplinaryUseCase : IDismissalDisciplinaryUseCase
    {
        private readonly IPersonnelChangeRepository _personnelChangeRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IPenaltyRecordRepository _penaltyRecordRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly IBaseRepository<EmploymentHistory> _historyRepo;
        private readonly IBaseRepository<FinalSettlement> _finalSettlementRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;
        private readonly PersonnelChangeRiskSummaryBuilder _riskSummaryBuilder;
        private readonly IPersonnelChangeAccessGuard _accessGuard;
        private readonly IPersonnelChangeContractFlowService _contractFlowService;
        private readonly IPersonnelChangeUseCase _personnelChangeUseCase;

        public DismissalDisciplinaryUseCase(
            IPersonnelChangeRepository personnelChangeRepo,
            IEmployeeRepository employeeRepo,
            IPenaltyRecordRepository penaltyRecordRepo,
            IAccountRepository accountRepo,
            IBaseRepository<EmploymentHistory> historyRepo,
            IBaseRepository<FinalSettlement> finalSettlementRepo,
            IUnitOfWork unitOfWork,
            ILockService lockService,
            PersonnelChangeRiskSummaryBuilder riskSummaryBuilder,
            IPersonnelChangeAccessGuard accessGuard,
            IPersonnelChangeContractFlowService contractFlowService,
            IPersonnelChangeUseCase personnelChangeUseCase)
        {
            _personnelChangeRepo = personnelChangeRepo;
            _employeeRepo = employeeRepo;
            _penaltyRecordRepo = penaltyRecordRepo;
            _accountRepo = accountRepo;
            _historyRepo = historyRepo;
            _finalSettlementRepo = finalSettlementRepo;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
            _riskSummaryBuilder = riskSummaryBuilder;
            _accessGuard = accessGuard;
            _contractFlowService = contractFlowService;
            _personnelChangeUseCase = personnelChangeUseCase;
        }

        public async Task<PersonnelChangeDetailDto> CreateDismissalAsync(
            CreateDismissalDto dto,
            int actorAccountId,
            CancellationToken ct)
        {
            if (dto.EmployeeId <= 0)
                throw new ArgumentException("Employee is required.");
            if (dto.SourcePenaltyRecordId <= 0)
                throw new ArgumentException("Penalty record is required.");

            _accessGuard.EnsurePersonnelChangeEvidencePath(dto.EvidenceFilePath);

            var employee = await _accessGuard.EnsureCanAccessEmployeeAsync(dto.EmployeeId, actorAccountId, ct);
            var penalty = await _penaltyRecordRepo.GetByIdAsync(dto.SourcePenaltyRecordId, ct)
                ?? throw new KeyNotFoundException("Penalty record was not found.");

            if (penalty.EmployeeId != employee.Id)
                throw new InvalidOperationException("Penalty record does not belong to the selected employee.");
            await _accessGuard.EnsureContractBelongsToEmployeeAsync(dto.RelatedContractId, employee.Id, ct);

            var reason = FirstNonEmpty(dto.Reason, penalty.Reason);
            var evidenceFilePath = FirstNonEmpty(dto.EvidenceFilePath, penalty.EvidenceFilePath);
            var managerNote = FirstNonEmpty(dto.ManagerNote, penalty.ManagerNote);
            var hrNote = FirstNonEmpty(dto.HRNote, penalty.HRNote);

            EnsureRequiredDismissalData(reason, evidenceFilePath, hrNote, managerNote);

            var request = new PersonnelChangeRequest
            {
                EmployeeId = employee.Id,
                ChangeType = PersonnelChangeType.Dismissal,
                Status = PersonnelChangeStatus.PendingHRReview,
                RequestedByAccountId = actorAccountId,
                RequestedAt = DateTime.UtcNow,
                Reason = reason,
                EffectiveDate = dto.EffectiveDate,

                CurrentDepartmentId = employee.DeptId,
                CurrentPositionId = employee.PositionId,
                CurrentManagerId = employee.ManagerId,
                CurrentJobLevelId = employee.JobLevelId,
                CurrentEmployeeType = employee.Type,

                RequiresEmployeeConsent = false,
                EmployeeConsentStatus = PersonnelChangeConsentStatus.NotRequired,
                RequiresContractFlow = true,
                ContractFlowType = PersonnelChangeContractFlowType.ContractTermination,
                RelatedContractId = dto.RelatedContractId,
                ContractFlowStatus = "NotStarted",
                RequiresDirectorApproval = true,
                RequiresHRProcessing = true,
                HRAssignedAccountId = actorAccountId,
                HRNote = hrNote,
                ManagerNote = managerNote,
                EvidenceFilePath = evidenceFilePath,
                ResponseDeadlineAt = dto.ResponseDeadlineAt,
                SourcePenaltyRecordId = penalty.Id,
                LockAccountOnExecution = dto.LockAccountOnExecution,
                RequiresFinalSettlement = dto.RequiresFinalSettlement,
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
                "DismissalCreated",
                null,
                request.Status,
                actorAccountId,
                request.Reason,
                ct);

            return await _personnelChangeUseCase.GetDetailAsync(request.Id, ct);
        }

        public Task<PersonnelChangeDetailDto> NotifyEmployeeAsync(
            int id,
            int actorAccountId,
            NotifyEmployeeDismissalDto dto,
            CancellationToken ct)
        {
            return MutateDismissalAsync(id, actorAccountId, async (request, innerCt) =>
            {
                EnsureStatus(request, PersonnelChangeStatus.PendingHRReview, PersonnelChangeStatus.PendingEmployeeNotification);

                _accessGuard.EnsurePersonnelChangeEvidencePath(dto.EvidenceFilePath);
                request.HRNote = FirstNonEmpty(dto.HRNote, request.HRNote);
                request.EvidenceFilePath = FirstNonEmpty(dto.EvidenceFilePath, request.EvidenceFilePath);
                request.ResponseDeadlineAt = dto.ResponseDeadlineAt ?? request.ResponseDeadlineAt;

                EnsureRequiredDismissalData(request.Reason, request.EvidenceFilePath, request.HRNote, request.ManagerNote);
                if (!request.ResponseDeadlineAt.HasValue)
                    throw new ArgumentException("Response deadline is required before notifying employee.");

                var oldStatus = request.Status;
                request.Status = PersonnelChangeStatus.PendingEmployeeNotification;
                request.HRProcessedAt = DateTime.UtcNow;

                await AddHistoryAsync(request.Id, "DismissalReadyForEmployeeNotification", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);

                oldStatus = request.Status;
                request.EmployeeNotifiedAt = dto.EmployeeNotifiedAt ?? DateTime.UtcNow;
                request.Status = PersonnelChangeStatus.PendingEmployeeExplanation;

                await AddApprovalAsync(request.Id, "DismissalEmployeeNotification", "HR", actorAccountId, true, dto.Note, innerCt);
                await AddHistoryAsync(request.Id, "DismissalEmployeeNotified", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
            }, ct);
        }

        public Task<PersonnelChangeDetailDto> SubmitDismissalExplanationAsync(
            int id,
            int actorAccountId,
            DismissalEmployeeExplanationDto dto,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Explanation))
                throw new ArgumentException("Explanation is required.");

            return MutateDismissalAsync(id, actorAccountId, async (request, innerCt) =>
            {
                EnsureStatus(request, PersonnelChangeStatus.PendingEmployeeExplanation);
                await EnsureSelectedEmployeeCanExplainAsync(request, actorAccountId, innerCt);

                _accessGuard.EnsurePersonnelChangeEvidencePath(dto.EvidenceFilePath);
                var oldStatus = request.Status;
                request.EmployeeExplanation = dto.Explanation.Trim();
                request.EmployeeExplanationAt = DateTime.UtcNow;
                request.EvidenceFilePath = FirstNonEmpty(dto.EvidenceFilePath, request.EvidenceFilePath);
                request.Status = PersonnelChangeStatus.PendingDirectorApproval;

                if (request.SourcePenaltyRecordId.HasValue)
                {
                    var penalty = await _penaltyRecordRepo.GetByIdAsync(request.SourcePenaltyRecordId.Value, innerCt);
                    if (penalty != null)
                    {
                        penalty.EmployeeExplanation = request.EmployeeExplanation;
                        _penaltyRecordRepo.Update(penalty);
                    }
                }

                await AddHistoryAsync(request.Id, "DismissalEmployeeExplanationSubmitted", oldStatus, request.Status, actorAccountId, request.EmployeeExplanation, innerCt);
            }, ct);
        }

        public Task<PersonnelChangeDetailDto> DirectorApproveDismissalAsync(
            int id,
            int actorAccountId,
            DirectorApproveDismissalDto dto,
            CancellationToken ct)
        {
            return MutateDismissalAsync(id, actorAccountId, async (request, innerCt) =>
            {
                EnsureStatus(request, PersonnelChangeStatus.PendingEmployeeExplanation, PersonnelChangeStatus.PendingDirectorApproval);
                EnsureDirectorCanReview(request);

                var oldStatus = request.Status;
                request.DirectorApprovedByAccountId = dto.IsApproved ? actorAccountId : null;
                request.DirectorApprovedAt = dto.IsApproved ? DateTime.UtcNow : null;
                request.DirectorNote = dto.Note;

                if (dto.IsApproved)
                {
                    request.Status = PersonnelChangeStatus.ApprovedByDirector;
                    await AddApprovalAsync(request.Id, "DirectorApproveDismissal", "Director", actorAccountId, true, dto.Note, innerCt);
                    await AddHistoryAsync(request.Id, "DirectorReviewedDismissal", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);

                    oldStatus = request.Status;
                    request.Status = PersonnelChangeStatus.PendingContractFlow;
                    request.ContractFlowStatus = "Pending";
                    await AddHistoryAsync(request.Id, "DismissalPendingContractFlow", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
                    await _contractFlowService.CreateContractFlowAsync(request, innerCt);
                }
                else
                {
                    request.Status = PersonnelChangeStatus.Rejected;
                    request.RejectedReason = dto.Note;
                    await AddApprovalAsync(request.Id, "DirectorApproveDismissal", "Director", actorAccountId, false, dto.Note, innerCt);
                    await AddHistoryAsync(request.Id, "DirectorReviewedDismissal", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
                }
            }, ct);
        }

        public Task<PersonnelChangeDetailDto> ExecuteDismissalAsync(
            int id,
            int actorAccountId,
            ExecutePersonnelChangeDto dto,
            CancellationToken ct)
        {
            return MutateDismissalAsync(id, actorAccountId, async (request, innerCt) =>
            {
                EnsureStatus(request, PersonnelChangeStatus.ContractAccepted, PersonnelChangeStatus.ReadyToExecute);
                _contractFlowService.EnsureCanExecute(request);

                if (!request.EmployeeId.HasValue)
                    throw new InvalidOperationException("Dismissal requires an employee before execution.");

                var employee = await _employeeRepo.GetByIdAsync(request.EmployeeId.Value, innerCt)
                    ?? throw new KeyNotFoundException("Employee was not found.");

                if (request.Status == PersonnelChangeStatus.ContractAccepted)
                {
                    var acceptedStatus = request.Status;
                    request.Status = PersonnelChangeStatus.ReadyToExecute;
                    await AddHistoryAsync(request.Id, "DismissalReadyToExecute", acceptedStatus, request.Status, actorAccountId, dto.Note, innerCt);
                }

                await ApplyDismissalAsync(request, employee, actorAccountId, dto, innerCt);

                var oldStatus = request.Status;
                request.Status = PersonnelChangeStatus.Completed;
                request.CompletedAt = dto.CompletedAt ?? DateTime.UtcNow;

                await AddHistoryAsync(request.Id, "DismissalExecuted", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
            }, ct);
        }

        private async Task<PersonnelChangeDetailDto> MutateDismissalAsync(
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

                    EnsureDismissal(request);
                    await mutation(request, innerCt);

                    request.UpdatedAt = DateTime.UtcNow;
                    _personnelChangeRepo.Update(request);
                    await _unitOfWork.CommitAsync(innerCt);
                }, innerCt);

                return true;
            }, cancellationToken: ct);

            await SaveSnapshotAsync(id, actorAccountId, ct);
            return await _personnelChangeUseCase.GetDetailAsync(id, ct);
        }

        private async Task ApplyDismissalAsync(
            PersonnelChangeRequest request,
            Employee employee,
            int actorAccountId,
            ExecutePersonnelChangeDto dto,
            CancellationToken ct)
        {
            var effectiveDate = request.EffectiveDate ?? dto.CompletedAt ?? DateTime.UtcNow.Date;

            if (employee.Status != EmployeeStatus.Dismissed)
            {
                await AddEmploymentHistoryAsync(
                    employee.Id,
                    HistoryType.Termination,
                    $"EmployeeStatus: {employee.Status}",
                    $"EmployeeStatus: {EmployeeStatus.Dismissed}",
                    effectiveDate,
                    ct);
                employee.Status = EmployeeStatus.Dismissed;
                _employeeRepo.Update(employee);
            }

            if (request.LockAccountOnExecution && employee.AccountId.HasValue)
            {
                var account = await _accountRepo.GetByIdAsync(employee.AccountId.Value, ct);
                if (account != null && account.Status != AccountStatus.Locked)
                {
                    account.Status = AccountStatus.Locked;
                    account.RefreshToken = null;
                    account.RefreshTokenExpiryTime = null;
                    request.AccountLockedAt = DateTime.UtcNow;
                    _accountRepo.Update(account);
                }
            }

            if (request.RequiresFinalSettlement && !request.RelatedFinalSettlementId.HasValue)
            {
                var settlement = new FinalSettlement
                {
                    EmployeeId = employee.Id,
                    TerminationType = TerminationType.Dismissal,
                    LastWorkingDate = effectiveDate.Date,
                    Status = FinalSettlementStatus.Draft,
                    Note = string.IsNullOrWhiteSpace(dto.Note)
                        ? $"Created from dismissal personnel change request #{request.Id}."
                        : dto.Note.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                await _finalSettlementRepo.AddAsync(settlement, ct);
                request.RelatedFinalSettlement = settlement;
            }
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

        private async Task EnsureSelectedEmployeeCanExplainAsync(PersonnelChangeRequest request, int actorAccountId, CancellationToken ct)
        {
            if (!request.EmployeeId.HasValue)
                throw new InvalidOperationException("Dismissal has no selected employee.");

            var employee = await _employeeRepo.GetByIdAsync(request.EmployeeId.Value, ct)
                ?? throw new KeyNotFoundException("Employee was not found.");

            if (employee.AccountId.HasValue && employee.AccountId.Value != actorAccountId)
                throw new UnauthorizedAccessException("Only the employee can submit dismissal explanation.");
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

        private static void EnsureDismissal(PersonnelChangeRequest request)
        {
            if (request.ChangeType != PersonnelChangeType.Dismissal)
                throw new InvalidOperationException("Request is not a dismissal request.");
        }

        private static void EnsureStatus(PersonnelChangeRequest request, params PersonnelChangeStatus[] allowedStatuses)
        {
            if (!PersonnelChangeStatusGuard.IsAllowed(request, allowedStatuses))
                throw new InvalidOperationException($"Request is in status {request.Status}, expected one of: {PersonnelChangeStatusGuard.DescribeAllowed(allowedStatuses)}.");
        }

        private static void EnsureDirectorCanReview(PersonnelChangeRequest request)
        {
            if (!request.SourcePenaltyRecordId.HasValue)
                throw new InvalidOperationException("Penalty record is required before Director approval.");

            EnsureRequiredDismissalData(request.Reason, request.EvidenceFilePath, request.HRNote, request.ManagerNote);

            var now = DateTime.UtcNow;
            if (!request.EmployeeNotifiedAt.HasValue &&
                (!request.ResponseDeadlineAt.HasValue || request.ResponseDeadlineAt.Value > now))
            {
                throw new InvalidOperationException("Director cannot approve before employee notification or response deadline expiry.");
            }

            if (string.IsNullOrWhiteSpace(request.EmployeeExplanation) &&
                request.ResponseDeadlineAt.HasValue &&
                request.ResponseDeadlineAt.Value > now)
            {
                throw new InvalidOperationException("Director cannot approve while employee response deadline is still open.");
            }
        }

        private static void EnsureRequiredDismissalData(string? reason, string? evidenceFilePath, string? hrNote, string? managerNote)
        {
            if (string.IsNullOrWhiteSpace(reason) && string.IsNullOrWhiteSpace(evidenceFilePath))
                throw new ArgumentException("Dismissal requires either reason or evidence file path.");

            if (string.IsNullOrWhiteSpace(hrNote) && string.IsNullOrWhiteSpace(managerNote))
                throw new ArgumentException("Dismissal requires HR note or manager note.");
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
        }
    }
}
