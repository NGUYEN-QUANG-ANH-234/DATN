using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Recruitment;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using MediatR;

namespace HRM.backend.src.HRM.Application.Handlers
{
    public class CandidateLevelChangedHandler : INotificationHandler<ApprovalLevelChangedEvent>
    {
        private readonly ICandidateRepository _candidateRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;

        public CandidateLevelChangedHandler(
            ICandidateRepository candidateRepo,
            IAccountRepository accountRepo,
            IEmailService emailService,
            IUnitOfWork unitOfWork)
        {
            _candidateRepo = candidateRepo;
            _accountRepo = accountRepo;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(ApprovalLevelChangedEvent notification, CancellationToken ct)
        {
            if (notification.ModuleCode != "CANDIDATE") return;

            var candidate = await _candidateRepo.GetByIdAsync(notification.ReferenceId, ct);
            if (candidate != null && notification.NewLevel == 2)
            {
                candidate.Status = CandidateStatus.Interview_Passed;
                await _candidateRepo.UpdateAsync(candidate, ct);
                await _unitOfWork.CommitAsync(ct);

                // Gửi email thông báo cho Giám đốc
                var directorAccountIds = await _accountRepo.GetAccountIdsByRoleAsync("Director", ct);
                int directorId = directorAccountIds.FirstOrDefault();
                if (directorId != 0)
                {
                    var directorAccount = await _accountRepo.GetByIdAsync(directorId, ct);
                    if (directorAccount != null && !string.IsNullOrEmpty(directorAccount.Email))
                    {
                        string subject = $"[HRM] Yêu cầu phê duyệt tuyển dụng: {candidate.FullName}";
                        string body = $@"
                            <h3>Kính gửi Giám đốc,</h3>
                            <p>Hồ sơ ứng viên <b>{candidate.FullName}</b> đã được Trưởng phòng phê duyệt đạt vòng phỏng vấn chuyên môn (Trạng thái: <b>Đạt phỏng vấn</b>).</p>
                            <p>Yêu cầu phê duyệt hiện đã chuyển lên cấp Giám đốc để đưa ra quyết định tuyển dụng cuối cùng.</p>
                            <p>Vui lòng đăng nhập hệ thống HRM và truy cập <b>Hộp thư phê duyệt</b> để hoàn tất xử lý.</p>
                            <br/>
                            <p>Trân trọng,<br/>Bộ phận Tuyển dụng HICAS</p>";
                        await _emailService.SendEmailAsync(directorAccount.Email, subject, body);
                        await _unitOfWork.CommitAsync(ct);
                    }
                }
            }
        }
    }
}
