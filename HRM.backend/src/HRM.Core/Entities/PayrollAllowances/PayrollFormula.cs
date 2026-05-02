using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.PayrollAllowances
{
    [Table("payroll_formulas")]
    public class PayrollFormula
    {
        [Key] public int Id { get; set; }

        [StringLength(100)] public required string FormulaName { get; set; }
        public string? Expression { get; set; }

        public bool IsActive { get; set; } = true;

        public FormulaStatus Status { get; set; } = FormulaStatus.Pending;
        public DateTime? DeadlineAt { get; set; }
    }
}