using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using MediatR;

namespace HRM.backend.src.HRM.Application.Handlers
{
    public class ContractApprovalCompletedHandler : INotificationHandler<ApprovalCompletedEvent>
    {
        private readonly IContractRepository _contractRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly ISlaTrackingService _slaTrackingService;

        public ContractApprovalCompletedHandler(
            IContractRepository contractRepo,
            IUnitOfWork unitOfWork,
            IAuditLogRepository auditLogRepo,
            ISlaTrackingService slaTrackingService,
            IMediator mediator)
        {
            _contractRepo = contractRepo;
            _unitOfWork = unitOfWork;
            _auditLogRepo = auditLogRepo;
            _slaTrackingService = slaTrackingService;
        }

        public async Task Handle(ApprovalCompletedEvent notification, CancellationToken ct)
        {
            if (notification.ModuleCode != "CONTRACT_DEPT" &&
                notification.ModuleCode != "CONTRACT_DIRECTOR")
            {
                return;
            }

            var contract = await _contractRepo.GetByIdAsync(notification.ReferenceId, ct);
            if (contract == null) return;

            if (notification.ModuleCode == "CONTRACT_DEPT")
            {
                var isApproved = notification.FinalStatus == ApprovalStatus.Approved;
                var isContentReview = contract.Status == ContractStatus.PendingManagerContentReview;
                contract.Status = isApproved
                    ? isContentReview ? ContractStatus.PendingEmployee : ContractStatus.PendingHR
                    : isContentReview ? ContractStatus.PendingHRRevision : ContractStatus.Rejected;

                if (!isApproved)
                    contract.NegotiationNote = notification.Note;

                await _contractRepo.UpdateAsync(contract, ct);
                await _auditLogRepo.LogSystemEventAsync(
                    "CONTRACT_DEPT_REVIEWED",
                    0,
                    "contract",
                    $"Dept reviewed contract ID {contract.Id}: {(isApproved ? "approved" : isContentReview ? "requested changes" : "rejected")}");
                await _unitOfWork.CommitAsync(ct);
                return;
            }

            var approvedByDirector = notification.FinalStatus == ApprovalStatus.Approved;
            contract.Status = approvedByDirector ? ContractStatus.ApprovedByDirector : ContractStatus.PendingHRRevision;
            if (!approvedByDirector)
                contract.NegotiationNote = notification.Note;

            await _contractRepo.UpdateAsync(contract, ct);
            await _slaTrackingService.ResolveTaskAsync(SlaModuleType.DirectorContractApproval, contract.Id, ct);
            await _auditLogRepo.LogSystemEventAsync(
                "CONTRACT_DIRECTOR_REVIEWED",
                0,
                "contract",
                $"Director reviewed contract ID {contract.Id}: {(approvedByDirector ? "approved" : "requested changes")}");
            await _unitOfWork.CommitAsync(ct);
        }
    }
}
