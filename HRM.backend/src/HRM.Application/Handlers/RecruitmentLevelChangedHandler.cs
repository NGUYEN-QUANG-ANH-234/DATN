using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Recruitment;
using MediatR;

namespace HRM.backend.src.HRM.Application.Handlers
{
    public class RecruitmentLevelChangedHandler : INotificationHandler<ApprovalLevelChangedEvent>
    {
        private readonly IRecruitmentRequestRepository _reqRepo;
        private readonly IUnitOfWork _unitOfWork;

        public RecruitmentLevelChangedHandler(IRecruitmentRequestRepository reqRepo, IUnitOfWork unitOfWork)
        {
            _reqRepo = reqRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(ApprovalLevelChangedEvent notification, CancellationToken ct)
        {
            if (notification.ModuleCode != "RECRUITMENT") return;

            // Lấy đơn tuyển dụng ra
            var recruitmentReq = await _reqRepo.GetByIdAsync(notification.ReferenceId, ct);
            if (recruitmentReq != null && notification.NewLevel == 2)
            {
                // 🔥 Đẩy trạng thái sang chờ Giám đốc
                recruitmentReq.Status = RecruitmentRequestStatus.PendingDirector;
                await _reqRepo.UpdateAsync(recruitmentReq, ct);
                await _unitOfWork.CommitAsync(ct);
            }
        }
    }
}
