using HRM.backend.src.HRM.Application.DTOs.PersonnelChanges;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.Services;
using HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.UseCases;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.PersonnelChanges;
using HRM.backend.src.HRM.Core.Entities.RequestHandover;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PersonnelChanges;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;

namespace HRM.backend.src.HRM.Application.UseCases.PersonnelChanges
{
    public class PromotionOfficialUseCase : IPromotionOfficialUseCase
    {
        private readonly IPersonnelChangeRepository _personnelChangeRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IPerformanceReviewRepository _performanceReviewRepo;
        private readonly IBaseRepository<EmploymentHistory> _historyRepo;
        private readonly IBaseRepository<EmploymentServicePeriod> _servicePeriodRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;
        private readonly PersonnelChangeRiskSummaryBuilder _riskSummaryBuilder;
        private readonly IPersonnelChangeContractFlowService _contractFlowService;
        private readonly IPersonnelChangeUseCase _personnelChangeUseCase;

        public PromotionOfficialUseCase(
            IPersonnelChangeRepository personnelChangeRepo,
            IEmployeeRepository employeeRepo,
            IPerformanceReviewRepository performanceReviewRepo,
            IBaseRepository<EmploymentHistory> historyRepo,
            IBaseRepository<EmploymentServicePeriod> servicePeriodRepo,
            IUnitOfWork unitOfWork,
            ILockService lockService,
            PersonnelChangeRiskSummaryBuilder riskSummaryBuilder,
            IPersonnelChangeContractFlowService contractFlowService,
            IPersonnelChangeUseCase personnelChangeUseCase)
        {
            _personnelChangeRepo = personnelChangeRepo;
            _employeeRepo = employeeRepo;
            _performanceReviewRepo = performanceReviewRepo;
            _historyRepo = historyRepo;
            _servicePeriodRepo = servicePeriodRepo;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
            _riskSummaryBuilder = riskSummaryBuilder;
            _contractFlowService = contractFlowService;
            _personnelChangeUseCase = personnelChangeUseCase;
        }

        public async Task<PersonnelChangeDetailDto> CreatePromotionAsync(
            CreatePromotionDto dto,
            int actorAccountId,
            CancellationToken ct)
        {
            if (dto.PromotionType == PersonnelChangePromotionType.ConvertToOfficial)
                throw new ArgumentException("Use create convert official endpoint for ConvertToOfficial.");

            if (!dto.NewPositionId.HasValue && !dto.NewJobLevelId.HasValue && !dto.NewEmployeeType.HasValue)
                throw new ArgumentException("Promotion requires a new position, job level, or employee type.");

            return await CreateRequestAsync(
                employeeId: dto.EmployeeId,
                changeType: PersonnelChangeType.Promotion,
                promotionType: dto.PromotionType,
                newPositionId: dto.NewPositionId,
                newJobLevelId: dto.NewJobLevelId,
                newEmployeeType: dto.NewEmployeeType,
                effectiveDate: dto.EffectiveDate,
                reason: dto.Reason,
                sourcePerformanceReviewId: dto.SourcePerformanceReviewId,
                requiresContractFlow: dto.RequiresContractFlow,
                contractFlowType: dto.ContractFlowType,
                relatedContractId: dto.RelatedContractId,
                actorAccountId,
                ct);
        }

        public async Task<PersonnelChangeDetailDto> CreateConvertOfficialAsync(
            CreateConvertOfficialDto dto,
            int actorAccountId,
            CancellationToken ct)
        {
            return await CreateRequestAsync(
                employeeId: dto.EmployeeId,
                changeType: PersonnelChangeType.ConvertToOfficial,
                promotionType: PersonnelChangePromotionType.ConvertToOfficial,
                newPositionId: dto.NewPositionId,
                newJobLevelId: dto.NewJobLevelId,
                newEmployeeType: dto.NewEmployeeType,
                effectiveDate: dto.EffectiveDate,
                reason: dto.Reason,
                sourcePerformanceReviewId: dto.SourcePerformanceReviewId,
                requiresContractFlow: dto.RequiresContractFlow,
                contractFlowType: dto.ContractFlowType,
                relatedContractId: dto.RelatedContractId,
                actorAccountId,
                ct);
        }

        public Task<PersonnelChangeDetailDto> HrReviewPromotionAsync(
            int id,
            int actorAccountId,
            ApprovePromotionDto dto,
            CancellationToken ct)
        {
            return MutatePromotionAsync(id, actorAccountId, async (request, innerCt) =>
            {
                EnsureStatus(request, PersonnelChangeStatus.PendingHRReview);

                var oldStatus = request.Status;
                request.HRAssignedAccountId = dto.HRAssignedAccountId ?? request.HRAssignedAccountId ?? actorAccountId;
                request.HRNote = dto.Note;
                request.HRProcessedAt = DateTime.UtcNow;
                ApplyContractFlowOverrides(request, dto);

                if (dto.IsApproved)
                {
                    request.Status = PersonnelChangeStatus.PendingDirectorApproval;
                }
                else
                {
                    request.Status = PersonnelChangeStatus.Rejected;
                    request.RejectedReason = dto.Note;
                }

                await AddApprovalAsync(request.Id, "PromotionHRReview", "HR", actorAccountId, dto.IsApproved, dto.Note, innerCt);
                await AddHistoryAsync(request.Id, "PromotionHRReviewed", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
            }, ct);
        }

        public Task<PersonnelChangeDetailDto> DirectorApprovePromotionAsync(
            int id,
            int actorAccountId,
            ApprovePromotionDto dto,
            CancellationToken ct)
        {
            return MutatePromotionAsync(id, actorAccountId, async (request, innerCt) =>
            {
                EnsureStatus(request, PersonnelChangeStatus.PendingDirectorApproval);

                var oldStatus = request.Status;
                request.DirectorApprovedByAccountId = dto.IsApproved ? actorAccountId : null;
                request.DirectorApprovedAt = dto.IsApproved ? DateTime.UtcNow : null;
                request.DirectorNote = dto.Note;
                ApplyContractFlowOverrides(request, dto);

                if (dto.IsApproved)
                {
                    request.Status = PersonnelChangeStatus.ApprovedByDirector;
                    await AddApprovalAsync(request.Id, "PromotionDirectorApproval", "Director", actorAccountId, true, dto.Note, innerCt);
                    await AddHistoryAsync(request.Id, "PromotionDirectorApproved", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);

                    oldStatus = request.Status;
                    if (request.RequiresContractFlow)
                    {
                        request.Status = PersonnelChangeStatus.PendingContractFlow;
                        request.ContractFlowStatus = "Pending";
                        await AddHistoryAsync(request.Id, "PromotionPendingContractFlow", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
                        await _contractFlowService.CreateContractFlowAsync(request, innerCt);
                    }
                    else
                    {
                        request.Status = PersonnelChangeStatus.ReadyToExecute;
                        await AddHistoryAsync(request.Id, "PromotionReadyToExecute", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
                    }
                }
                else
                {
                    request.Status = PersonnelChangeStatus.Rejected;
                    request.RejectedReason = dto.Note;
                    await AddApprovalAsync(request.Id, "PromotionDirectorApproval", "Director", actorAccountId, false, dto.Note, innerCt);
                    await AddHistoryAsync(request.Id, "PromotionDirectorRejected", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
                }
            }, ct);
        }

        public Task<PersonnelChangeDetailDto> ExecutePromotionAsync(
            int id,
            int actorAccountId,
            ExecutePersonnelChangeDto dto,
            CancellationToken ct)
        {
            return MutatePromotionAsync(id, actorAccountId, async (request, innerCt) =>
            {
                EnsureStatus(
                    request,
                    PersonnelChangeStatus.ApprovedByDirector,
                    PersonnelChangeStatus.ContractAccepted,
                    PersonnelChangeStatus.ReadyToExecute);
                _contractFlowService.EnsureCanExecute(request);

                if (!request.EmployeeId.HasValue)
                    throw new InvalidOperationException("Promotion requires an employee before execution.");

                if (request.Status != PersonnelChangeStatus.ReadyToExecute)
                {
                    var oldStatus = request.Status;
                    request.Status = PersonnelChangeStatus.ReadyToExecute;
                    await AddHistoryAsync(request.Id, "PromotionReadyToExecute", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
                }

                var employee = await _employeeRepo.GetByIdAsync(request.EmployeeId.Value, innerCt)
                    ?? throw new KeyNotFoundException("Employee was not found.");

                await ApplyPromotionAsync(request, employee, dto, innerCt);

                var completedFrom = request.Status;
                request.Status = PersonnelChangeStatus.Completed;
                request.CompletedAt = dto.CompletedAt ?? DateTime.UtcNow;
                await AddHistoryAsync(request.Id, "PromotionExecuted", completedFrom, request.Status, actorAccountId, dto.Note, innerCt);
            }, ct);
        }

        private async Task<PersonnelChangeDetailDto> CreateRequestAsync(
            int employeeId,
            PersonnelChangeType changeType,
            PersonnelChangePromotionType promotionType,
            int? newPositionId,
            int? newJobLevelId,
            EmployeeType? newEmployeeType,
            DateTime? effectiveDate,
            string? reason,
            int? sourcePerformanceReviewId,
            bool requiresContractFlow,
            PersonnelChangeContractFlowType contractFlowType,
            int? relatedContractId,
            int actorAccountId,
            CancellationToken ct)
        {
            if (employeeId <= 0)
                throw new ArgumentException("Employee is required.");

            var employee = await _employeeRepo.GetByIdAsync(employeeId, ct)
                ?? throw new KeyNotFoundException("Employee was not found.");

            if (sourcePerformanceReviewId.HasValue)
            {
                var review = await _performanceReviewRepo.GetDetailAsync(sourcePerformanceReviewId.Value, ct)
                    ?? throw new KeyNotFoundException("Performance review was not found.");
                if (review.EmployeeId != employee.Id)
                    throw new InvalidOperationException("Performance review does not belong to the selected employee.");
            }

            var request = new PersonnelChangeRequest
            {
                EmployeeId = employee.Id,
                ChangeType = changeType,
                PromotionType = promotionType,
                Status = PersonnelChangeStatus.PendingHRReview,
                RequestedByAccountId = actorAccountId,
                RequestedAt = DateTime.UtcNow,
                Reason = reason,
                EffectiveDate = effectiveDate,

                CurrentDepartmentId = employee.DeptId,
                CurrentPositionId = employee.PositionId,
                CurrentManagerId = employee.ManagerId,
                CurrentJobLevelId = employee.JobLevelId,
                CurrentEmployeeType = employee.Type,

                NewDepartmentId = employee.DeptId,
                NewPositionId = newPositionId,
                NewManagerId = employee.ManagerId,
                NewJobLevelId = newJobLevelId,
                NewEmployeeType = newEmployeeType,

                RequiresEmployeeConsent = false,
                EmployeeConsentStatus = PersonnelChangeConsentStatus.NotRequired,
                RequiresContractFlow = requiresContractFlow,
                ContractFlowType = requiresContractFlow ? contractFlowType : PersonnelChangeContractFlowType.None,
                RelatedContractId = relatedContractId,
                ContractFlowStatus = requiresContractFlow ? "NotStarted" : null,
                RequiresDirectorApproval = true,
                RequiresHRProcessing = true,
                HRAssignedAccountId = actorAccountId,
                SourcePerformanceReviewId = sourcePerformanceReviewId,
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
                changeType == PersonnelChangeType.ConvertToOfficial ? "ConvertOfficialCreated" : "PromotionCreated",
                null,
                request.Status,
                actorAccountId,
                request.Reason,
                ct);

            return await _personnelChangeUseCase.GetDetailAsync(request.Id, ct);
        }

        private async Task<PersonnelChangeDetailDto> MutatePromotionAsync(
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

                    EnsurePromotionOrConvertOfficial(request);
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

        private async Task ApplyPromotionAsync(
            PersonnelChangeRequest request,
            Employee employee,
            ExecutePersonnelChangeDto dto,
            CancellationToken ct)
        {
            var effectiveDate = request.EffectiveDate ?? dto.CompletedAt ?? DateTime.UtcNow.Date;

            if (request.NewPositionId.HasValue && request.NewPositionId != employee.PositionId)
            {
                await AddEmploymentHistoryAsync(
                    employee.Id,
                    HistoryType.Promotion,
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
                    HistoryType.Promotion,
                    $"JobLevelId: {employee.JobLevelId?.ToString() ?? "null"}",
                    $"JobLevelId: {request.NewJobLevelId.Value}",
                    effectiveDate,
                    ct);
                employee.JobLevelId = request.NewJobLevelId.Value;
            }

            if (request.NewEmployeeType.HasValue && request.NewEmployeeType != employee.Type)
            {
                await AddEmploymentHistoryAsync(
                    employee.Id,
                    request.ChangeType == PersonnelChangeType.ConvertToOfficial ? HistoryType.Onboarding : HistoryType.Promotion,
                    $"EmployeeType: {employee.Type}",
                    $"EmployeeType: {request.NewEmployeeType.Value}",
                    effectiveDate,
                    ct);
                employee.Type = request.NewEmployeeType.Value;
            }

            if (request.ChangeType == PersonnelChangeType.ConvertToOfficial)
            {
                if (employee.Status != EmployeeStatus.Official)
                {
                    await AddEmploymentHistoryAsync(
                        employee.Id,
                        HistoryType.Onboarding,
                        $"EmployeeStatus: {employee.Status}",
                        $"EmployeeStatus: {EmployeeStatus.Official}",
                        effectiveDate,
                        ct);
                    employee.Status = EmployeeStatus.Official;
                }

                await EnsureOfficialServicePeriodAsync(request, employee.Id, effectiveDate.Date, ct);
            }

            _employeeRepo.Update(employee);
        }

        private async Task EnsureOfficialServicePeriodAsync(
            PersonnelChangeRequest request,
            int employeeId,
            DateTime effectiveDate,
            CancellationToken ct)
        {
            var existing = (await _servicePeriodRepo.FindAsync(
                p => p.SourceType == "PersonnelChangeConvertOfficial" && p.SourceId == request.Id,
                ct)).FirstOrDefault();

            if (existing != null)
                return;

            await _servicePeriodRepo.AddAsync(new EmploymentServicePeriod
            {
                EmployeeId = employeeId,
                PeriodStart = effectiveDate,
                PeriodEnd = new DateTime(9999, 12, 31),
                PeriodType = EmploymentServicePeriodType.OfficialWork,
                IsActualWorkingTime = true,
                IsSocialInsuranceContributed = true,
                IsUnemploymentInsuranceContributed = true,
                IsExcludedFromSeverance = false,
                SourceType = "PersonnelChangeConvertOfficial",
                SourceId = request.Id,
                Note = $"Official service period from personnel change request #{request.Id}.",
                CreatedAt = DateTime.UtcNow
            }, ct);
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

        private static void ApplyContractFlowOverrides(PersonnelChangeRequest request, ApprovePromotionDto dto)
        {
            if (dto.RequiresContractFlow.HasValue)
            {
                request.RequiresContractFlow = dto.RequiresContractFlow.Value;
                if (!dto.RequiresContractFlow.Value)
                {
                    request.ContractFlowType = PersonnelChangeContractFlowType.None;
                    request.ContractFlowStatus = null;
                }
            }

            if (request.RequiresContractFlow)
            {
                request.ContractFlowType = dto.ContractFlowType ?? request.ContractFlowType;
                if (request.ContractFlowType == PersonnelChangeContractFlowType.None)
                    request.ContractFlowType = PersonnelChangeContractFlowType.ContractAddendum;
                request.ContractFlowStatus ??= "NotStarted";
                request.RelatedContractId = dto.RelatedContractId ?? request.RelatedContractId;
            }
        }

        private static void EnsurePromotionOrConvertOfficial(PersonnelChangeRequest request)
        {
            if (request.ChangeType is not (PersonnelChangeType.Promotion or PersonnelChangeType.ConvertToOfficial))
                throw new InvalidOperationException("Request is not a promotion or official conversion request.");
        }

        private static void EnsureStatus(PersonnelChangeRequest request, params PersonnelChangeStatus[] allowedStatuses)
        {
            if (!PersonnelChangeStatusGuard.IsAllowed(request, allowedStatuses))
                throw new InvalidOperationException($"Request is in status {request.Status}, expected one of: {PersonnelChangeStatusGuard.DescribeAllowed(allowedStatuses)}.");
        }
    }
}
