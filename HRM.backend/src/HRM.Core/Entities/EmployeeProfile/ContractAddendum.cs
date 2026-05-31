using HRM.backend.src.HRM.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.EmployeeProfile
{
    [Table("contract_addendums")]
    public class ContractAddendum
    {
        [Key]
        public int Id { get; set; }

        public int ContractId { get; set; }

        [ForeignKey(nameof(ContractId))]
        public virtual Contract? Contract { get; set; }

        [StringLength(50)]
        public required string AddendumNumber { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal? NewBasicSalary { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal? NewInsuranceSalary { get; set; }

        public DateTime? NewEndDate { get; set; }

        [Column(TypeName = "json")]
        public string? OtherChangesJson { get; set; }

        [StringLength(1000)]
        public string? Content { get; set; }

        public DateTime EffectiveDate { get; set; }

        public AddendumStatus Status { get; set; } = AddendumStatus.Draft;

        [StringLength(500)]
        public string? RejectReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<ContractAddendumDetail> Details { get; set; } = new List<ContractAddendumDetail>();
    }
}
