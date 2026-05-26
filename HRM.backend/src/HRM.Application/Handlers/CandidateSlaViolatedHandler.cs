using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Core.Entities.Recruitment;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using MediatR;

namespace HRM.backend.src.HRM.Application.Handlers
{
    public class CandidateSlaViolatedHandler : INotificationHandler<SlaViolatedEvent>
    {
        private readonly IBaseRepository<Candidate> _candidateRepo;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;

        public CandidateSlaViolatedHandler(
            IBaseRepository<Candidate> candidateRepo,
            IEmailService emailService,
            IUnitOfWork unitOfWork)
        {
            _candidateRepo = candidateRepo;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(SlaViolatedEvent notification, CancellationToken ct)
        {
            // 1. Chỉ xử lý nếu SLA bị vi phạm thuộc module Tuyển dụng (CandidateApproval)
            if (notification.ModuleType != SlaModuleType.CandidateApproval) return;

            var candidate = await _candidateRepo.GetByIdAsync(notification.ReferenceId, ct);

            // 2. Chỉ đổi trạng thái nếu nó đang kẹt ở bước chờ Giám đốc duyệt (Interview_Passed)
            if (candidate != null && candidate.Status == CandidateStatus.Interview_Passed)
            {
                candidate.Status = CandidateStatus.SLA_Expired;
                await _candidateRepo.UpdateAsync(candidate, ct);
                await _unitOfWork.CommitAsync(ct);

                // 3. Gửi Email cảnh báo cho bộ phận HR
                string hrEmail = "hr@hicas.vn"; // TODO: Có thể lấy động từ bảng Cấu hình hoặc User
                string subject = $"[CẢNH BÁO SLA] Hồ sơ ứng viên {candidate.FullName} đã quá hạn";
                string body = $@"
                    <h3>Hệ thống HRM HICAS Cảnh báo:</h3>
                    <p>Hồ sơ ứng viên <b>{candidate.FullName}</b> đã nằm chờ Giám đốc duyệt quá thời hạn quy định.</p>
                    <p>Vui lòng đăng nhập vào hệ thống, kiểm tra lại luồng phê duyệt và tiến hành xử lý thủ công.</p>";

                await _emailService.SendEmailAsync(hrEmail, subject, body);
                await _unitOfWork.CommitAsync(ct);
            }
        }
    }
}
