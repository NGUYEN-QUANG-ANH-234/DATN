using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;

namespace HRM.backend.src.HRM.Infrastructure.ExternalServices
{
    public class EmailService : IEmailService
    {
        private readonly IBaseRepository<OutboxMessage> _outboxRepo;

        public EmailService(IBaseRepository<OutboxMessage> outboxRepo)
        {
            _outboxRepo = outboxRepo;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            await _outboxRepo.AddAsync(new OutboxMessage
            {
                Recipient = toEmail,
                Subject = subject,
                Body = body
            });
        }
    }
}
