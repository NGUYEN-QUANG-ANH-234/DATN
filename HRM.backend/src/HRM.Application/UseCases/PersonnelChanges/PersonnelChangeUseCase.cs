using HRM.backend.src.HRM.Application.DTOs.PersonnelChanges;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.UseCases;
using HRM.backend.src.HRM.Core.Entities.PersonnelChanges;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PersonnelChanges;

namespace HRM.backend.src.HRM.Application.UseCases.PersonnelChanges
{
    public class PersonnelChangeUseCase : IPersonnelChangeUseCase
    {
        private readonly IPersonnelChangeRepository _personnelChangeRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;
        private readonly PersonnelChangeRiskSummaryBuilder _riskSummaryBuilder;

        public PersonnelChangeUseCase(
            IPersonnelChangeRepository personnelChangeRepo,
            IUnitOfWork unitOfWork,
            ILockService lockService,
            PersonnelChangeRiskSummaryBuilder riskSummaryBuilder)
        {
            _personnelChangeRepo = personnelChangeRepo;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
            _riskSummaryBuilder = riskSummaryBuilder;
        }

        public async Task<List<PersonnelChangeListItemDto>> GetListAsync(
            PersonnelChangeType? changeType,
            PersonnelChangeStatus? status,
            int? employeeId,
            DateTime? requestedFrom,
            DateTime? requestedTo,
            CancellationToken ct)
        {
            var requests = await _personnelChangeRepo.GetByFilterAsync(
                changeType,
                status,
                employeeId,
                requestedFrom,
                requestedTo,
                ct);

            return requests.Select(MapListItem).ToList();
        }

        public async Task<PersonnelChangeDetailDto> GetDetailAsync(int id, CancellationToken ct)
        {
            var request = await _personnelChangeRepo.GetDetailAsync(id, ct)
                ?? throw new KeyNotFoundException("Personnel change request was not found.");

            return MapDetail(request);
        }

        public Task<PersonnelChangeRiskSummaryDto> GetRiskSummaryAsync(int id, CancellationToken ct)
        {
            return _riskSummaryBuilder.BuildAsync(id, ct);
        }

        public async Task<List<PersonnelChangeTimelineDto>> GetTimelineAsync(int id, CancellationToken ct)
        {
            var histories = await _personnelChangeRepo.GetTimelineAsync(id, ct);
            return histories.Select(MapTimeline).ToList();
        }

        public Task<PersonnelChangeDetailDto> CancelAsync(int id, int actorAccountId, CancelPersonnelChangeDto dto, CancellationToken ct)
        {
            return MutateAsync(id, actorAccountId, async (request, innerCt) =>
            {
                EnsureNotClosed(request);

                var oldStatus = request.Status;
                request.Status = PersonnelChangeStatus.Cancelled;
                request.RejectedReason = string.IsNullOrWhiteSpace(request.RejectedReason) ? dto.Reason : request.RejectedReason;

                await AddHistoryAsync(request.Id, "Cancelled", oldStatus, request.Status, actorAccountId, dto.Reason, innerCt);
            }, snapshotAfterMutation: true, ct);
        }

        private async Task<PersonnelChangeDetailDto> MutateAsync(
            int id,
            int actorAccountId,
            Func<PersonnelChangeRequest, CancellationToken, Task> mutation,
            bool snapshotAfterMutation,
            CancellationToken ct)
        {
            await _lockService.GetWithLockAsync($"personnel_change_{id}", async innerCt =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var request = await _personnelChangeRepo.GetDetailAsync(id, innerCt)
                        ?? throw new KeyNotFoundException("Personnel change request was not found.");

                    await mutation(request, innerCt);

                    request.UpdatedAt = DateTime.UtcNow;

                    _personnelChangeRepo.Update(request);
                    await _unitOfWork.CommitAsync(innerCt);
                }, innerCt);

                return true;
            }, cancellationToken: ct);

            if (snapshotAfterMutation)
                await SaveSnapshotAsync(id, actorAccountId, ct);

            return await GetDetailAsync(id, ct);
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

        private static void EnsureNotClosed(PersonnelChangeRequest request)
        {
            if (request.Status is PersonnelChangeStatus.Completed or PersonnelChangeStatus.Rejected or PersonnelChangeStatus.Cancelled)
                throw new InvalidOperationException("Closed personnel change requests cannot be modified.");
        }

        private static PersonnelChangeListItemDto MapListItem(PersonnelChangeRequest request)
        {
            return new PersonnelChangeListItemDto
            {
                Id = request.Id,
                EmployeeId = request.EmployeeId,
                EmployeeCode = request.Employee?.EmployeeCode,
                EmployeeName = request.Employee?.FullName,
                ChangeType = request.ChangeType,
                PromotionType = request.PromotionType,
                Status = request.Status,
                RequestedAt = request.RequestedAt,
                RequestedByAccountId = request.RequestedByAccountId,
                RequestedByName = FormatAccountName(request.RequestedByAccount),
                EffectiveDate = request.EffectiveDate,
                Reason = request.Reason,
                RequiresEmployeeConsent = request.RequiresEmployeeConsent,
                EmployeeConsentStatus = request.EmployeeConsentStatus,
                RequiresContractFlow = request.RequiresContractFlow,
                ContractFlowType = request.ContractFlowType,
                RequiresDirectorApproval = request.RequiresDirectorApproval,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt
            };
        }

        private static PersonnelChangeDetailDto MapDetail(PersonnelChangeRequest request)
        {
            var dto = new PersonnelChangeDetailDto
            {
                Id = request.Id,
                EmployeeId = request.EmployeeId,
                EmployeeCode = request.Employee?.EmployeeCode,
                EmployeeName = request.Employee?.FullName,
                ChangeType = request.ChangeType,
                PromotionType = request.PromotionType,
                Status = request.Status,
                RequestedAt = request.RequestedAt,
                RequestedByAccountId = request.RequestedByAccountId,
                RequestedByName = FormatAccountName(request.RequestedByAccount),
                EffectiveDate = request.EffectiveDate,
                Reason = request.Reason,
                RequiresEmployeeConsent = request.RequiresEmployeeConsent,
                EmployeeConsentStatus = request.EmployeeConsentStatus,
                RequiresContractFlow = request.RequiresContractFlow,
                ContractFlowType = request.ContractFlowType,
                RequiresDirectorApproval = request.RequiresDirectorApproval,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt,

                CurrentDepartmentId = request.CurrentDepartmentId,
                CurrentDepartmentName = request.CurrentDepartment?.DeptName,
                CurrentPositionId = request.CurrentPositionId,
                CurrentPositionName = request.CurrentPosition?.Title,
                CurrentManagerId = request.CurrentManagerId,
                CurrentManagerName = request.CurrentManager?.FullName,
                CurrentJobLevelId = request.CurrentJobLevelId,
                CurrentJobLevelName = request.CurrentJobLevel?.Name,
                CurrentEmployeeType = request.CurrentEmployeeType,

                NewDepartmentId = request.NewDepartmentId,
                NewDepartmentName = request.NewDepartment?.DeptName,
                NewPositionId = request.NewPositionId,
                NewPositionName = request.NewPosition?.Title,
                NewManagerId = request.NewManagerId,
                NewManagerName = request.NewManager?.FullName,
                NewJobLevelId = request.NewJobLevelId,
                NewJobLevelName = request.NewJobLevel?.Name,
                NewEmployeeType = request.NewEmployeeType,

                EmployeeConsentAt = request.EmployeeConsentAt,
                EmployeeConsentNote = request.EmployeeConsentNote,
                RelatedContractId = request.RelatedContractId,
                RelatedContractRequestId = request.RelatedContractRequestId,
                RelatedContractAddendumId = request.RelatedContractAddendumId,
                ContractFlowStatus = request.ContractFlowStatus,
                DirectorApprovedByAccountId = request.DirectorApprovedByAccountId,
                DirectorApprovedByName = FormatAccountName(request.DirectorApprovedByAccount),
                DirectorApprovedAt = request.DirectorApprovedAt,
                DirectorNote = request.DirectorNote,
                RequiresHRProcessing = request.RequiresHRProcessing,
                HRAssignedAccountId = request.HRAssignedAccountId,
                HRAssignedName = FormatAccountName(request.HRAssignedAccount),
                HRNote = request.HRNote,
                HRProcessedAt = request.HRProcessedAt,
                EmployeeNotifiedAt = request.EmployeeNotifiedAt,
                ResponseDeadlineAt = request.ResponseDeadlineAt,
                EvidenceFilePath = request.EvidenceFilePath,
                ManagerNote = request.ManagerNote,
                EmployeeExplanation = request.EmployeeExplanation,
                EmployeeExplanationAt = request.EmployeeExplanationAt,
                LockAccountOnExecution = request.LockAccountOnExecution,
                AccountLockedAt = request.AccountLockedAt,
                RequiresFinalSettlement = request.RequiresFinalSettlement,
                RelatedFinalSettlementId = request.RelatedFinalSettlementId,
                SourcePenaltyRecordId = request.SourcePenaltyRecordId,
                SourcePerformanceReviewId = request.SourcePerformanceReviewId,
                DecisionNumber = request.DecisionNumber,
                DecisionFilePath = request.DecisionFilePath,
                DecisionIssuedAt = request.DecisionIssuedAt,
                CompletedAt = request.CompletedAt,
                RejectedReason = request.RejectedReason
            };

            dto.Approvals = request.Approvals
                .OrderBy(a => a.CreatedAt)
                .Select(a => new PersonnelChangeApprovalDto
                {
                    Id = a.Id,
                    RequestId = a.RequestId,
                    StepName = a.StepName,
                    ApproverRole = a.ApproverRole,
                    ApproverAccountId = a.ApproverAccountId,
                    ApproverName = FormatAccountName(a.ApproverAccount),
                    Decision = a.Decision,
                    Note = a.Note,
                    DecidedAt = a.DecidedAt,
                    CreatedAt = a.CreatedAt
                })
                .ToList();

            dto.ContractLinks = request.ContractLinks
                .OrderBy(l => l.CreatedAt)
                .Select(l => new PersonnelChangeContractFlowDto
                {
                    Id = l.Id,
                    PersonnelChangeRequestId = l.PersonnelChangeRequestId,
                    ContractId = l.ContractId,
                    ContractRequestId = l.ContractRequestId,
                    ContractAddendumId = l.ContractAddendumId,
                    ContractFlowType = l.ContractFlowType,
                    Status = l.Status,
                    CreatedAt = l.CreatedAt,
                    CompletedAt = l.CompletedAt
                })
                .ToList();

            dto.Histories = request.Histories
                .OrderBy(h => h.CreatedAt)
                .Select(MapTimeline)
                .ToList();

            return dto;
        }

        private static PersonnelChangeTimelineDto MapTimeline(PersonnelChangeHistory history)
        {
            return new PersonnelChangeTimelineDto
            {
                Id = history.Id,
                RequestId = history.RequestId,
                Action = history.Action,
                OldStatus = history.OldStatus,
                NewStatus = history.NewStatus,
                ActorAccountId = history.ActorAccountId,
                ActorName = FormatAccountName(history.ActorAccount),
                Note = history.Note,
                CreatedAt = history.CreatedAt
            };
        }

        private static string? FormatAccountName(Core.Entities.System.Account? account)
        {
            if (account == null)
                return null;

            return string.IsNullOrWhiteSpace(account.FullName) ? account.Email : account.FullName;
        }
    }
}
