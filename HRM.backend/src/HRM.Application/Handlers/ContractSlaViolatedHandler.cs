using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using MediatR;

namespace HRM.backend.src.HRM.Application.Handlers
{
    public class ContractSlaViolatedHandler : INotificationHandler<SlaViolatedEvent>
    {
        private readonly IContractRepository _contractRepo;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;

        public ContractSlaViolatedHandler(
            IContractRepository contractRepo,
            IEmailService emailService,
            IUnitOfWork unitOfWork)
        {
            _contractRepo = contractRepo;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(SlaViolatedEvent notification, CancellationToken ct)
        {
            if (notification.ModuleType != SlaModuleType.ContractRenewal &&
                notification.ModuleType != SlaModuleType.DirectorContractApproval)
            {
                return;
            }

            var contract = await _contractRepo.GetByIdAsync(notification.ReferenceId, ct);
            if (contract == null) return;

            string employeeName = contract.Employee?.FullName ?? $"Employee ID {contract.EmployeeId}";
            const string hrEmail = "hr@hicas.vn";

            if (notification.ModuleType == SlaModuleType.ContractRenewal)
            {
                if (contract.Status != ContractStatus.Draft &&
                    contract.Status != ContractStatus.Negotiating)
                {
                    return;
                }

                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    contract.Status = ContractStatus.Draft_Cancelled;
                    contract.NegotiationNote =
                        "System cancelled the contract draft because the employee response SLA expired.";

                    await _contractRepo.UpdateAsync(contract, ct);
                    await _unitOfWork.CommitAsync(ct);
                }, ct);

                string subject = $"[SLA WARNING] Contract #{contract.ContractNumber} draft cancelled";
                string body = $@"
                    <h3>HRM contract SLA notification</h3>
                    <p>Contract <b>{contract.ContractNumber}</b> for <b>{employeeName}</b> was automatically cancelled because the employee did not respond within the configured draft response SLA.</p>
                    <p>Please follow up with the employee and create a new request if needed.</p>";

                await _emailService.SendEmailAsync(hrEmail, subject, body);
                return;
            }

            if (contract.Status != ContractStatus.PendingDirector)
            {
                return;
            }

            const string directorEmail = "director@hicas.vn";
            string alertSubject = $"[URGENT SLA WARNING] Director contract approval overdue - {employeeName}";
            string alertBody = $@"
                <h3>Contract approval is overdue</h3>
                <p>Contract <b>{contract.ContractNumber}</b> for <b>{employeeName}</b> is still waiting for director approval after the configured SLA.</p>
                <ul>
                    <li>Contract type: {contract.ContractType}</li>
                    <li>Basic salary: {contract.BasicSalary:N0} VND</li>
                    <li>Start date: {contract.StartDate:dd/MM/yyyy}</li>
                </ul>";

            await _emailService.SendEmailAsync(directorEmail, alertSubject, alertBody);
            await _emailService.SendEmailAsync(hrEmail, alertSubject, alertBody);
        }
    }
}
