using HRM.backend.src.HRM.Core.Enums;
using MediatR;

namespace HRM.backend.src.HRM.Application.DTOs.Events
{
    public class ContractFlowRequiredEvent : INotification
    {
        public int PersonnelChangeRequestId { get; set; }
        public int EmployeeId { get; set; }
        public PersonnelChangeContractFlowType ContractFlowType { get; set; }
        public int? ContractId { get; set; }
        public int? ContractRequestId { get; set; }
        public int? ContractAddendumId { get; set; }
    }

    public class ContractFlowCompletedEvent : INotification
    {
        public int? ContractId { get; set; }
        public int? ContractAddendumId { get; set; }
        public PersonnelChangeContractFlowType? ContractFlowType { get; set; }
        public string Status { get; set; } = "Completed";
        public string? Note { get; set; }
    }
}
