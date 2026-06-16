using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Recruitment;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using MediatR;

namespace HRM.backend.src.HRM.Application.Handlers
{
    public class RecruitmentApprovalCompletedHandler : INotificationHandler<ApprovalCompletedEvent>
    {
        private readonly IRecruitmentRequestRepository _requestRepo;
        private readonly ISlaTrackingService _slaTrackingService;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IUnitOfWork _unitOfWork;

        public RecruitmentApprovalCompletedHandler(
            IRecruitmentRequestRepository requestRepo,
            ISlaTrackingService slaTrackingService,
            IAuditLogRepository auditLogRepo,
            IUnitOfWork unitOfWork)
        {
            _requestRepo = requestRepo;
            _slaTrackingService = slaTrackingService;
            _auditLogRepo = auditLogRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(ApprovalCompletedEvent notification, CancellationToken ct)
        {
            if (notification.ModuleCode != "RECRUITMENT") return;

            var request = await _requestRepo.GetByIdAsync(notification.ReferenceId, ct);
            if (request == null) return;

            if (notification.FinalStatus == ApprovalStatus.Approved)
            {
                request.Status = RecruitmentRequestStatus.Approved;
                await _requestRepo.UpdateAsync(request, ct);
                await _slaTrackingService.ResolveTaskAsync(SlaModuleType.Recruitment, request.Id, ct);
                await _auditLogRepo.LogSystemEventAsync(
                    "RECRUITMENT_APPROVED",
                    0,
                    "recruitment",
                    $"Nhu cầu tuyển dụng #{request.Id} đã được duyệt.");
            }
            else if (notification.FinalStatus == ApprovalStatus.Rejected)
            {
                request.Status = RecruitmentRequestStatus.Rejected;
                await _requestRepo.UpdateAsync(request, ct);
                await _slaTrackingService.ResolveTaskAsync(SlaModuleType.Recruitment, request.Id, ct);
                await _auditLogRepo.LogSystemEventAsync(
                    "RECRUITMENT_REJECTED",
                    0,
                    "recruitment",
                    $"Nhu cầu tuyển dụng #{request.Id} đã bị từ chối.");
            }
            else if (notification.FinalStatus == ApprovalStatus.NeedMoreInfo)
            {
                await _auditLogRepo.LogSystemEventAsync(
                    "RECRUITMENT_NEED_MORE_INFO",
                    0,
                    "recruitment",
                    $"Nhu cầu tuyển dụng #{request.Id} cần bổ sung thông tin. Ghi chú: {notification.Note}");
            }

            await _unitOfWork.CommitAsync(ct);
        }
    }
}
