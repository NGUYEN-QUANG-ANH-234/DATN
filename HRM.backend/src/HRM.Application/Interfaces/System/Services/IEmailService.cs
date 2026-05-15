namespace HRM.backend.src.HRM.Application.Interfaces.System.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
