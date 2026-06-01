using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.DTOs.PersonnelChanges
{
    public class PersonnelChangeContractFlowDto
    {
        public int Id { get; set; }
        public int PersonnelChangeRequestId { get; set; }
        public int? ContractId { get; set; }
        public int? ContractRequestId { get; set; }
        public int? ContractAddendumId { get; set; }
        public PersonnelChangeContractFlowType ContractFlowType { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
