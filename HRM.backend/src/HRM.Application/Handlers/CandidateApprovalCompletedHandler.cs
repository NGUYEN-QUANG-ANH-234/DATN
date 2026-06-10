using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Recruitment;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using MediatR;

namespace HRM.backend.src.HRM.Application.Handlers
{
    public class CandidateApprovalCompletedHandler : INotificationHandler<ApprovalCompletedEvent>
    {
        private readonly ICandidateRepository _candidateRepo;
        private readonly IRecruitmentRequestRepository _recruitmentRequestRepo;
        private readonly ISlaTrackingService _slaTrackingService;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;

        public CandidateApprovalCompletedHandler(
            ICandidateRepository candidateRepo,
            IRecruitmentRequestRepository recruitmentRequestRepo,
            ISlaTrackingService slaTrackingService,
            IAuditLogRepository auditLogRepo,
            IUnitOfWork unitOfWork,
            IEmailService emailService)
        {
            _candidateRepo = candidateRepo;
            _recruitmentRequestRepo = recruitmentRequestRepo;
            _slaTrackingService = slaTrackingService;
            _auditLogRepo = auditLogRepo;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        public async Task Handle(ApprovalCompletedEvent notification, CancellationToken ct)
        {
            if (notification.ModuleCode != "CANDIDATE") return;

            var candidate = await _candidateRepo.GetByIdAsync(notification.ReferenceId, ct);
            if (candidate == null) return;

            if (notification.FinalStatus == ApprovalStatus.Approved)
            {
                candidate.Status = CandidateStatus.Offer;
                await _candidateRepo.UpdateAsync(candidate, ct);

                if (candidate.RecruitmentRequestId.HasValue)
                {
                    var request = await _recruitmentRequestRepo.GetByIdWithCandidatesAsync(candidate.RecruitmentRequestId.Value, ct);
                    if (request != null && request.Status == RecruitmentRequestStatus.Approved && request.Quantity > 0)
                    {
                        var filledSlots = request.Candidates.Count(c =>
                            c.Id == candidate.Id ||
                            c.Status == CandidateStatus.Offer ||
                            c.Status == CandidateStatus.Hired);

                        if (filledSlots >= request.Quantity)
                        {
                            request.Status = RecruitmentRequestStatus.Closed;
                            await _recruitmentRequestRepo.UpdateAsync(request, ct);
                        }
                    }
                }

                await _slaTrackingService.ResolveTaskAsync(SlaModuleType.CandidateApproval, candidate.Id, ct);

                // Tự động hủy các hồ sơ khác của ứng viên này đang ở trạng thái New hoặc Interview_Pending, Interview_Passed
                if (!string.IsNullOrEmpty(candidate.Email))
                {
                    var otherApplications = await _candidateRepo.FindAsync(c =>
                        c.Email == candidate.Email &&
                        c.Id != candidate.Id &&
                        (c.Status == CandidateStatus.New || c.Status == CandidateStatus.Interview_Pending || c.Status == CandidateStatus.Interview_Passed), ct);

                    foreach (var otherApp in otherApplications)
                    {
                        otherApp.Status = CandidateStatus.Rejected;
                        await _candidateRepo.UpdateAsync(otherApp, ct);
                        await _slaTrackingService.ResolveTaskAsync(SlaModuleType.CandidateApproval, otherApp.Id, ct);
                    }
                }

                await _auditLogRepo.LogSystemEventAsync(
                    actionType: "CANDIDATE_OFFER",
                    accountId: 0,
                    module: "recruitment",
                    message: $"Giám đốc đã duyệt trúng tuyển cho ứng viên {candidate.FullName}."
                );

                if (!string.IsNullOrEmpty(candidate.Email))
                {
                    string candidateSubject = "Thư mời nhận việc từ HRM HICAS";
                    string candidateBody = $"<h2>Chúc mừng {candidate.FullName}!</h2><p>Bạn đã vượt qua vòng phỏng vấn...</p>";

                    string hrSubject = $"[NHÂN SỰ MỚI] Yêu cầu soạn hợp đồng cho {candidate.FullName}";
                    string hrBody = $"Giám đốc đã chốt Offer cho ứng viên {candidate.FullName}. Vui lòng chuẩn bị thủ tục tiếp nhận.";

                    // Chạy song song không đợi lẫn nhau
                    var mailToCandidateTask = _emailService.SendEmailAsync(candidate.Email, candidateSubject, candidateBody);
                    var mailToHrTask = _emailService.SendEmailAsync("hr@hicas.vn", hrSubject, hrBody);

                    await Task.WhenAll(mailToCandidateTask, mailToHrTask);
                }
            }
            else if (notification.FinalStatus == ApprovalStatus.Rejected)
            {
                candidate.Status = CandidateStatus.Rejected;
                await _candidateRepo.UpdateAsync(candidate, ct);

                await _slaTrackingService.ResolveTaskAsync(SlaModuleType.CandidateApproval, candidate.Id, ct);

                await _auditLogRepo.LogSystemEventAsync(
                    actionType: "CANDIDATE_REJECTED",
                    accountId: 0,
                    module: "recruitment",
                    message: $"Ứng viên {candidate.FullName} đã bị từ chối."
                );
            }

            await _unitOfWork.CommitAsync(ct);
        }
    }
}
