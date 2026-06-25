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
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;

namespace HRM.backend.src.HRM.Application.UseCases.PersonnelChanges
{
    public class VoluntaryTerminationUseCase : IVoluntaryTerminationUseCase
    {
        private readonly IPersonnelChangeRepository _personnelChangeRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly IBaseRepository<EmploymentHistory> _historyRepo;
        private readonly IBaseRepository<EmploymentServicePeriod> _servicePeriodRepo;
        private readonly IBaseRepository<FinalSettlement> _finalSettlementRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;
        private readonly PersonnelChangeRiskSummaryBuilder _riskSummaryBuilder;
        private readonly IPersonnelChangeAccessGuard _accessGuard;
        private readonly IPersonnelChangeContractFlowService _contractFlowService;
        private readonly IPersonnelChangeUseCase _personnelChangeUseCase;

        public VoluntaryTerminationUseCase(
            IPersonnelChangeRepository personnelChangeRepo,
            IEmployeeRepository employeeRepo,
            IAccountRepository accountRepo,
            IBaseRepository<EmploymentHistory> historyRepo,
            IBaseRepository<EmploymentServicePeriod> servicePeriodRepo,
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
            _accountRepo = accountRepo;
            _historyRepo = historyRepo;
            _servicePeriodRepo = servicePeriodRepo;
            _finalSettlementRepo = finalSettlementRepo;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
            _riskSummaryBuilder = riskSummaryBuilder;
            _accessGuard = accessGuard;
            _contractFlowService = contractFlowService;
            _personnelChangeUseCase = personnelChangeUseCase;
        }

        public async Task<PersonnelChangeDetailDto> SubmitResignationAsync(
            SubmitResignationDto dto,
            int actorAccountId,
            CancellationToken ct)
        {
            if (dto.EmployeeId <= 0)
                throw new ArgumentException("Employee is required.");
            if (dto.ExpectedLastWorkingDate == default)
                throw new ArgumentException("Expected last working date is required.");

            var employee = await _accessGuard.EnsureCanAccessEmployeeAsync(dto.EmployeeId, actorAccountId, ct);

            var request = new PersonnelChangeRequest
            {
                EmployeeId = employee.Id,
                ChangeType = PersonnelChangeType.VoluntaryTermination,
                Status = PersonnelChangeStatus.PendingManagerReview,
                RequestedByAccountId = actorAccountId,
                RequestedAt = DateTime.UtcNow,
                Reason = TrimOrNull(dto.Reason),
                EffectiveDate = dto.ExpectedLastWorkingDate.Date,

                CurrentDepartmentId = employee.DeptId,
                CurrentPositionId = employee.PositionId,
                CurrentManagerId = employee.ManagerId,
                CurrentJobLevelId = employee.JobLevelId,
                CurrentEmployeeType = employee.Type,

                RequiresEmployeeConsent = false,
                EmployeeConsentStatus = PersonnelChangeConsentStatus.Acknowledged,
                EmployeeConsentAt = DateTime.UtcNow,
                EmployeeConsentNote = TrimOrNull(dto.EmployeeNote),
                RequiresContractFlow = true,
                ContractFlowType = PersonnelChangeContractFlowType.ContractTermination,
                ContractFlowStatus = "NotStarted",
                RequiresDirectorApproval = true,
                RequiresHRProcessing = true,
                LockAccountOnExecution = true,
                RequiresFinalSettlement = true,
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
                "ResignationSubmitted",
                null,
                request.Status,
                actorAccountId,
                request.EmployeeConsentNote ?? request.Reason,
                ct);

            return await _personnelChangeUseCase.GetDetailAsync(request.Id, ct);
        }

        public Task<PersonnelChangeDetailDto> ManagerReviewResignationAsync(
            int id,
            int actorAccountId,
            ManagerReviewResignationDto dto,
            CancellationToken ct)
        {
            return MutateResignationAsync(id, actorAccountId, async (request, innerCt) =>
            {
                EnsureStatus(request, PersonnelChangeStatus.PendingManagerReview);
                if (!request.EmployeeId.HasValue)
                    throw new InvalidOperationException("Resignation requires an employee before manager review.");
                await _accessGuard.EnsureCurrentManagerCanActAsync(
                    request.CurrentManagerId,
                    actorAccountId,
                    "review this resignation",
                    innerCt);

                var oldStatus = request.Status;
                request.ManagerNote = TrimOrNull(dto.Note);
                request.Status = dto.IsApproved
                    ? PersonnelChangeStatus.PendingHRReview
                    : PersonnelChangeStatus.Rejected;
                if (!dto.IsApproved)
                    request.RejectedReason = request.ManagerNote;

                await AddApprovalAsync(request.Id, "ResignationManagerReview", "Manager", actorAccountId, dto.IsApproved, dto.Note, innerCt);
                await AddHistoryAsync(request.Id, "ResignationManagerReviewed", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
            }, ct);
        }

        public Task<PersonnelChangeDetailDto> HrReviewResignationAsync(
            int id,
            int actorAccountId,
            HrReviewResignationDto dto,
            CancellationToken ct)
        {
            return MutateResignationAsync(id, actorAccountId, async (request, innerCt) =>
            {
                await _accessGuard.EnsureActorHasRoleAsync(actorAccountId, innerCt, "HR", "Admin");
                EnsureStatus(request, PersonnelChangeStatus.PendingHRReview);

                var oldStatus = request.Status;
                request.HRAssignedAccountId = actorAccountId;
                request.HRNote = TrimOrNull(dto.Note);
                request.HRProcessedAt = DateTime.UtcNow;
                if (request.EmployeeId.HasValue)
                    await _accessGuard.EnsureContractBelongsToEmployeeAsync(
                        dto.RelatedContractId,
                        request.EmployeeId.Value,
                        innerCt);
                request.RelatedContractId = dto.RelatedContractId ?? request.RelatedContractId;
                request.RequiresFinalSettlement = dto.RequiresFinalSettlement;
                request.LockAccountOnExecution = dto.LockAccountAfterEffectiveDate;

                request.Status = dto.IsApproved
                    ? PersonnelChangeStatus.PendingDirectorApproval
                    : PersonnelChangeStatus.Rejected;
                if (!dto.IsApproved)
                    request.RejectedReason = request.HRNote;

                await AddApprovalAsync(request.Id, "ResignationHRReview", "HR", actorAccountId, dto.IsApproved, dto.Note, innerCt);
                await AddHistoryAsync(request.Id, "ResignationHRReviewed", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
            }, ct);
        }

        public Task<PersonnelChangeDetailDto> DirectorApproveResignationAsync(
            int id,
            int actorAccountId,
            DirectorApproveResignationDto dto,
            CancellationToken ct)
        {
            return MutateResignationAsync(id, actorAccountId, async (request, innerCt) =>
            {
                await _accessGuard.EnsureActorHasRoleAsync(actorAccountId, innerCt, "Director", "Admin");
                EnsureStatus(request, PersonnelChangeStatus.PendingDirectorApproval);

                var oldStatus = request.Status;
                request.DirectorApprovedByAccountId = dto.IsApproved ? actorAccountId : null;
                request.DirectorApprovedAt = dto.IsApproved ? DateTime.UtcNow : null;
                request.DirectorNote = TrimOrNull(dto.Note);

                if (!dto.IsApproved)
                {
                    request.Status = PersonnelChangeStatus.Rejected;
                    request.RejectedReason = request.DirectorNote;
                    await AddApprovalAsync(request.Id, "ResignationDirectorApproval", "Director", actorAccountId, false, dto.Note, innerCt);
                    await AddHistoryAsync(request.Id, "ResignationDirectorRejected", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
                    return;
                }

                request.Status = PersonnelChangeStatus.ApprovedByDirector;
                await AddApprovalAsync(request.Id, "ResignationDirectorApproval", "Director", actorAccountId, true, dto.Note, innerCt);
                await AddHistoryAsync(request.Id, "ResignationDirectorApproved", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);

                oldStatus = request.Status;
                request.Status = PersonnelChangeStatus.PendingContractFlow;
                request.ContractFlowStatus = "Pending";
                await AddHistoryAsync(request.Id, "ResignationPendingContractFlow", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
                await _contractFlowService.CreateContractFlowAsync(request, innerCt);
            }, ct);
        }

        public Task<PersonnelChangeDetailDto> ExecuteResignationAsync(
            int id,
            int actorAccountId,
            ExecutePersonnelChangeDto dto,
            CancellationToken ct)
        {
            return MutateResignationAsync(id, actorAccountId, async (request, innerCt) =>
            {
                await _accessGuard.EnsureActorHasRoleAsync(actorAccountId, innerCt, "HR", "Admin");
                EnsureStatus(request, PersonnelChangeStatus.ContractAccepted, PersonnelChangeStatus.ReadyToExecute);
                _contractFlowService.EnsureCanExecute(request);

                if (!request.EmployeeId.HasValue)
                    throw new InvalidOperationException("Resignation requires an employee before execution.");

                var employee = await _employeeRepo.GetByIdAsync(request.EmployeeId.Value, innerCt)
                    ?? throw new KeyNotFoundException("Employee was not found.");

                if (request.Status == PersonnelChangeStatus.ContractAccepted)
                {
                    var acceptedStatus = request.Status;
                    request.Status = PersonnelChangeStatus.ReadyToExecute;
                    await AddHistoryAsync(request.Id, "ResignationReadyToExecute", acceptedStatus, request.Status, actorAccountId, dto.Note, innerCt);
                }

                await ApplyResignationAsync(request, employee, dto, innerCt);

                var oldStatus = request.Status;
                request.Status = PersonnelChangeStatus.Completed;
                request.CompletedAt = dto.CompletedAt ?? DateTime.UtcNow;

                await AddHistoryAsync(request.Id, "ResignationExecuted", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
            }, ct);
        }

        private async Task<PersonnelChangeDetailDto> MutateResignationAsync(
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

                    EnsureResignation(request);
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

        private async Task ApplyResignationAsync(
            PersonnelChangeRequest request,
            Employee employee,
            ExecutePersonnelChangeDto dto,
            CancellationToken ct)
        {
            var effectiveDate = (request.EffectiveDate ?? dto.CompletedAt ?? DateTime.UtcNow).Date;

            if (employee.Status != EmployeeStatus.Resigned)
            {
                await AddEmploymentHistoryAsync(
                    employee.Id,
                    HistoryType.Termination,
                    $"EmployeeStatus: {employee.Status}",
                    $"EmployeeStatus: {EmployeeStatus.Resigned}",
                    effectiveDate,
                    ct);
                employee.Status = EmployeeStatus.Resigned;
                _employeeRepo.Update(employee);
            }

            await CloseOpenServicePeriodsAsync(request, employee.Id, effectiveDate, ct);

            if (request.RequiresFinalSettlement && !request.RelatedFinalSettlementId.HasValue)
            {
                var settlement = new FinalSettlement
                {
                    EmployeeId = employee.Id,
                    TerminationType = TerminationType.Resignation,
                    LastWorkingDate = effectiveDate,
                    Status = FinalSettlementStatus.Draft,
                    Note = string.IsNullOrWhiteSpace(dto.Note)
                        ? $"Created from voluntary termination personnel change request #{request.Id}."
                        : dto.Note.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                await _finalSettlementRepo.AddAsync(settlement, ct);
                request.RelatedFinalSettlement = settlement;
            }

            if (request.LockAccountOnExecution && employee.AccountId.HasValue && effectiveDate <= DateTime.UtcNow.Date)
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
        }

        private async Task CloseOpenServicePeriodsAsync(
            PersonnelChangeRequest request,
            int employeeId,
            DateTime effectiveDate,
            CancellationToken ct)
        {
            var periods = await _servicePeriodRepo.FindAsync(
                p => p.EmployeeId == employeeId &&
                     p.IsActualWorkingTime &&
                     p.PeriodEnd >= effectiveDate,
                ct);

            foreach (var period in periods)
            {
                period.PeriodEnd = effectiveDate < period.PeriodStart.Date
                    ? period.PeriodStart.Date
                    : effectiveDate;
                period.Note = string.IsNullOrWhiteSpace(period.Note)
                    ? $"Closed by voluntary termination personnel change request #{request.Id}."
                    : $"{period.Note} Closed by voluntary termination personnel change request #{request.Id}.";
                _servicePeriodRepo.Update(period);
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
                Note = TrimOrNull(note),
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
                Note = TrimOrNull(note),
                CreatedAt = DateTime.UtcNow
            }, ct);
        }

        private static void EnsureResignation(PersonnelChangeRequest request)
        {
            if (request.ChangeType != PersonnelChangeType.VoluntaryTermination)
                throw new InvalidOperationException("Request is not a voluntary termination request.");
        }

        private static void EnsureStatus(PersonnelChangeRequest request, params PersonnelChangeStatus[] allowedStatuses)
        {
            if (!PersonnelChangeStatusGuard.IsAllowed(request, allowedStatuses))
                throw new InvalidOperationException($"Request is in status {request.Status}, expected one of: {PersonnelChangeStatusGuard.DescribeAllowed(allowedStatuses)}.");
        }

        private static string? TrimOrNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
