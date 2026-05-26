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
                <p>Ho so Onboarding cua ban da duoc nhan su xac minh thanh cong.</p>
                <p>Duoi day la thong tin dang nhap he thong noi bo cua ban:</p>
                <ul>
                    <li><b>Ma nhan vien:</b> {notification.EmpCode}</li>
                    <li><b>Tai khoan:</b> {notification.Email}</li>
                </ul>
                <p>Vui long dang nhap va doi mat khau trong lan dau truy cap.</p>";

            await _emailService.SendEmailAsync(notification.Email, subject, body);
            await _unitOfWork.CommitAsync(ct);
        }
    }
}
