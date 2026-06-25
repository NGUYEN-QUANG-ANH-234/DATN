using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.Recruitment;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Recruitment;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace HRM.backend.src.HRM.Application.Handlers
{
    public class CandidateApprovalCompletedHandler : INotificationHandler<ApprovalCompletedEvent>
    {
        private readonly ICandidateRepository _candidateRepo;
        private readonly IRecruitmentRequestRepository _recruitmentRequestRepo;
        private readonly ISlaTrackingService _slaTrackingService;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IBaseRepository<OnboardingRequest> _onboardingRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public CandidateApprovalCompletedHandler(
            ICandidateRepository candidateRepo,
            IRecruitmentRequestRepository recruitmentRequestRepo,
            ISlaTrackingService slaTrackingService,
            IAuditLogRepository auditLogRepo,
            IBaseRepository<OnboardingRequest> onboardingRepo,
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _candidateRepo = candidateRepo;
            _recruitmentRequestRepo = recruitmentRequestRepo;
            _slaTrackingService = slaTrackingService;
            _auditLogRepo = auditLogRepo;
            _onboardingRepo = onboardingRepo;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _configuration = configuration;
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
                await EnsureOnboardingInvitationAsync(candidate, ct);

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
                    var profileSetupLink = BuildProfileSetupLink(candidate.Email, candidate.TrackingCode);
                    var safeName = WebUtility.HtmlEncode(candidate.FullName);
                    var safeTrackingCode = WebUtility.HtmlEncode(candidate.TrackingCode ?? "-");
                    var safeProfileSetupLink = WebUtility.HtmlEncode(profileSetupLink);

                    string candidateSubject = "[HICAS] Hoàn thiện hồ sơ tiếp nhận";
                    string candidateBody = $"""
                        <h2>Chúc mừng {safeName}!</h2>
                        <p>Bạn đã vượt qua vòng phê duyệt tuyển dụng của HICAS.</p>
                        <p>Vui lòng hoàn thiện hồ sơ tiếp nhận để HR xác minh và kích hoạt tài khoản nội bộ.</p>
                        <p><a href="{safeProfileSetupLink}" target="_blank">Hoàn thiện hồ sơ tiếp nhận</a></p>
                        <p>Mã hồ sơ: <b>{safeTrackingCode}</b></p>
                        """;

                    string hrSubject = $"[NHÂN SỰ MỚI] Theo dõi hồ sơ tiếp nhận của {candidate.FullName}";
                    string hrBody = $"""
                        Giám đốc đã chốt Offer cho ứng viên {candidate.FullName}.
                        Vui lòng theo dõi hồ sơ tiếp nhận sau khi ứng viên hoàn thiện thông tin.
                        Link hoàn thiện hồ sơ: {profileSetupLink}
                        """;

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

        private string BuildProfileSetupLink(string email, string? trackingCode)
        {
            var allowedOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
            var baseUrl = _configuration["ClientApp:BaseUrl"]
                ?? allowedOrigins?.FirstOrDefault(origin => origin.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                ?? allowedOrigins?.FirstOrDefault()
                ?? "https://localhost:5173";

            return $"{baseUrl.TrimEnd('/')}/employee-contract/profile-setup?trackingCode={Uri.EscapeDataString(trackingCode ?? string.Empty)}&email={Uri.EscapeDataString(email)}";
        }

        private async Task EnsureOnboardingInvitationAsync(Candidate candidate, CancellationToken ct)
        {
            var existing = (await _onboardingRepo.FindAsync(r => r.CandidateId == candidate.Id, ct))
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault();

            if (existing != null)
            {
                if (existing.Status == OnboardingStatus.Rejected)
                {
                    existing.Status = OnboardingStatus.PendingCandidateProfile;
                    existing.RejectReason = null;
                    await _onboardingRepo.UpdateAsync(existing, ct);
                }

                return;
            }

            await _onboardingRepo.AddAsync(new OnboardingRequest
            {
                CandidateId = candidate.Id,
                RequestedDataJson = "{}",
                Status = OnboardingStatus.PendingCandidateProfile
            }, ct);
        }
    }
}
