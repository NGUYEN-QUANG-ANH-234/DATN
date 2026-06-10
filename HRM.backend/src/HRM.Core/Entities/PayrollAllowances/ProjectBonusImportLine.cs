using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.PayrollAllowances
{
    [Table("project_bonus_import_lines")]
    public class ProjectBonusImportLine
    {
        [Key] public int Id { get; set; }

        public int BatchId { get; set; }
        [ForeignKey(nameof(BatchId))] public virtual ProjectBonusImportBatch Batch { get; set; } = null!;

        public int RowNumber { get; set; }

        public int? EmployeeId { get; set; }
        [ForeignKey(nameof(EmployeeId))] public virtual Employee? Employee { get; set; }

        [StringLength(50)] public required string EmployeeCodeSnapshot { get; set; }
        [StringLength(200)] public string? EmployeeNameSnapshot { get; set; }

        [StringLength(80)] public required string ProjectCode { get; set; }
        [StringLength(200)] public required string ProjectName { get; set; }

        [Column(TypeName = "decimal(15,2)")] public decimal BonusAmount { get; set; }
        public bool Taxable { get; set; } = true;
        public bool InsuranceContributable { get; set; }

        [StringLength(500)] public string? Reason { get; set; }
        [StringLength(1000)] public string? Note { get; set; }

        public ProjectBonusLineValidationStatus ValidationStatus { get; set; } = ProjectBonusLineValidationStatus.Pending;
        [StringLength(1000)] public string? ErrorMessage { get; set; }
    }
}
