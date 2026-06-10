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

        public ContractAddendumType AddendumType { get; set; } = ContractAddendumType.Other;

        [StringLength(80)]
        public string? BaseContractNumberSnapshot { get; set; }

        public DateTime? BaseContractStartDateSnapshot { get; set; }
        public DateTime? BaseContractEndDateSnapshot { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal? NewBasicSalary { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal? NewInsuranceSalary { get; set; }

        public DateTime? NewEndDate { get; set; }

        [Column(TypeName = "json")]
        public string? OtherChangesJson { get; set; }

        [StringLength(1000)]
        public string? Content { get; set; }

        [Column(TypeName = "text")]
        public string? ChangedContentSummary { get; set; }

        [Column(TypeName = "text")]
        public string? UnchangedTerms { get; set; }

        [StringLength(80)]
        public string? LegalDocumentNumber { get; set; }

        [StringLength(80)]
        public string? DocumentTemplateCode { get; set; }

        [StringLength(500)]
        public string? DocumentDocFilePath { get; set; }

        [StringLength(500)]
        public string? DocumentPdfFilePath { get; set; }

        public DateTime? IssuedAt { get; set; }
        public DateTime? EmployeeSignedAt { get; set; }
        public DateTime? EmployerSignedAt { get; set; }

        public DateTime EffectiveDate { get; set; }

        public AddendumStatus Status { get; set; } = AddendumStatus.Draft;

        [StringLength(500)]
        public string? RejectReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<ContractAddendumDetail> Details { get; set; } = new List<ContractAddendumDetail>();
    }
}
