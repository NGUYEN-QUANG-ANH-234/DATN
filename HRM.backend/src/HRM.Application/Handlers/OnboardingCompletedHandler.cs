using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using MediatR;

namespace HRM.backend.src.HRM.Application.Handlers
{
    public class OnboardingCompletedHandler : INotificationHandler<OnboardingCompletedEvent>
    {
        private readonly IEmailService _emailService;

        public OnboardingCompletedHandler(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task Handle(OnboardingCompletedEvent notification, CancellationToken ct)
        {
            string subject = "Chào mừng gia nhập HICAS!";
            string body = $@"
                <h3>Xin chào {notification.FullName},</h3>
                <p>Hồ sơ Onboarding của bạn đã được nhân sự xác minh thành công.</p>
                <p>Dưới đây là thông tin đăng nhập hệ thống nội bộ của bạn:</p>
                <ul>
                    <li><b>Mã nhân viên:</b> {notification.EmpCode}</li>
                    <li><b>Tài khoản:</b> {notification.Email}</li>
                </ul>
                <p>Vui lòng đăng nhập và đổi mật khẩu trong lần đầu truy cập.</p>";

            await _emailService.SendEmailAsync(notification.Email, subject, body);
        }
    }
}
