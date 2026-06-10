using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.Services;
using MediatR;

namespace HRM.backend.src.HRM.Application.Handlers
{
    public class PersonnelChangeContractFlowCompletedHandler : INotificationHandler<ContractFlowCompletedEvent>
    {
        private readonly IPersonnelChangeContractFlowService _contractFlowService;

        public PersonnelChangeContractFlowCompletedHandler(IPersonnelChangeContractFlowService contractFlowService)
        {
            _contractFlowService = contractFlowService;
        }

        public Task Handle(ContractFlowCompletedEvent notification, CancellationToken ct)
        {
            if (string.Equals(notification.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                return _contractFlowService.MarkContractFlowRejectedAsync(
                    notification.ContractId,
                    notification.ContractAddendumId,
                    notification.Note,
                    ct);
            }

            if (IsRevisionRequested(notification.Status))
            {
                return _contractFlowService.MarkContractFlowNegotiatingAsync(
                    notification.ContractId,
                    notification.ContractAddendumId,
                    notification.Note,
                    ct);
            }

            if (IsRevisionClosed(notification.Status))
            {
                return _contractFlowService.MarkContractFlowRevisionClosedAsync(
                    notification.ContractId,
                    notification.ContractAddendumId,
                    notification.Note,
                    ct);
            }

            return _contractFlowService.MarkContractFlowCompletedAsync(
                notification.ContractId,
                notification.ContractAddendumId,
                ct);
        }

        private static bool IsRevisionRequested(string? status)
        {
            return string.Equals(status, "Negotiating", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "RevisionRequested", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "RevisionRequired", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsRevisionClosed(string? status)
        {
            return string.Equals(status, "NotAgreed", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "RevisionClosed", StringComparison.OrdinalIgnoreCase);
        }
    }
}
