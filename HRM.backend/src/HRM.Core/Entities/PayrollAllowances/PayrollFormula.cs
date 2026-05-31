using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.PayrollAllowances
{
    [Table("payroll_formulas")]
    public class PayrollFormula
    {
        [Key] public int Id { get; set; }

        [StringLength(80)] public string FormulaCode { get; set; } = "DEFAULT_PAYROLL";
        [StringLength(100)] public required string FormulaName { get; set; }

        // Legacy aggregate expression. New payroll calculation should prefer Lines.
        public string? Expression { get; set; }

        public bool IsActive { get; set; } = true;

        public ContractType? ContractType { get; set; }
        public PayBasis? PayBasis { get; set; }
        public EmployeeType? EmployeeType { get; set; }
        public int? DeptId { get; set; }
        public int? PositionId { get; set; }
        public int? JobLevelId { get; set; }

        public int Version { get; set; } = 1;
        public DateTime EffectiveFrom { get; set; } = new(2020, 7, 1);
        public DateTime? EffectiveTo { get; set; }

        public FormulaStatus Status { get; set; } = FormulaStatus.Pending;
        public DateTime? DeadlineAt { get; set; }
        public int? ApprovedByAccountId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        [StringLength(1000)] public string? RejectReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<PayrollFormulaLine> Lines { get; set; } = new List<PayrollFormulaLine>();
    }
}
