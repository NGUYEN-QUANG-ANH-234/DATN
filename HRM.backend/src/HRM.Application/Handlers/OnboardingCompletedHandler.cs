using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using MediatR;

namespace HRM.backend.src.HRM.Application.Handlers
{
    public class OnboardingCompletedHandler : INotificationHandler<OnboardingCompletedEvent>
    {
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;

        public OnboardingCompletedHandler(IEmailService emailService, IUnitOfWork unitOfWork)
        {
            _emailService = emailService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(OnboardingCompletedEvent notification, CancellationToken ct)
        {
            var subject = "Chao mung gia nhap HICAS!";
            var body = $@"
                <h3>Xin chao {notification.FullName},</h3>
                <p>Hồ sơ Onboarding của bạn đã được nhân sự xác minh thành công.</p>
                <p>Duoi day la thong tin dang nhap he thong noi bo cua ban:</p>
                <ul>
                    <li><b>Mã nhân viên:</b> {notification.EmpCode}</li>
                    <li><b>Tài khoản:</b> {notification.Email}</li>
                </ul>
                <p>Vui lòng đăng nhập và đổi mật khẩu trong lần đầu truy cập.</p>";

            await _emailService.SendEmailAsync(notification.Email, subject, body);
            await _unitOfWork.CommitAsync(ct);
        }
    }
}
