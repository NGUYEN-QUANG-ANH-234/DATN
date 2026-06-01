using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.PersonnelChanges
{
    [Table("personnel_change_contract_links")]
    public class PersonnelChangeContractLink
    {
        [Key] public int Id { get; set; }

        public int PersonnelChangeRequestId { get; set; }
        [ForeignKey(nameof(PersonnelChangeRequestId))] public virtual PersonnelChangeRequest PersonnelChangeRequest { get; set; } = null!;

        public int? ContractId { get; set; }
        [ForeignKey(nameof(ContractId))] public virtual Contract? Contract { get; set; }

        public int? ContractRequestId { get; set; }

        public int? ContractAddendumId { get; set; }
        [ForeignKey(nameof(ContractAddendumId))] public virtual ContractAddendum? ContractAddendum { get; set; }

        public PersonnelChangeContractFlowType ContractFlowType { get; set; } = PersonnelChangeContractFlowType.None;

        [StringLength(50)]
        public string? Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}
