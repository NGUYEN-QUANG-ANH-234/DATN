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

            if (string.Equals(notification.Status, "Negotiating", StringComparison.OrdinalIgnoreCase) &&
                notification.ContractId.HasValue)
            {
                return _contractFlowService.MarkContractFlowNegotiatingAsync(
                    notification.ContractId.Value,
                    notification.Note,
                    ct);
            }

            return _contractFlowService.MarkContractFlowCompletedAsync(
                notification.ContractId,
                notification.ContractAddendumId,
                ct);
        }
    }
}
