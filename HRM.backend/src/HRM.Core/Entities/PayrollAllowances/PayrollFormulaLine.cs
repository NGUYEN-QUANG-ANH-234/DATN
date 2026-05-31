using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.PayrollAllowances
{
    [Table("payroll_formula_lines")]
    public class PayrollFormulaLine
    {
        [Key] public int Id { get; set; }

        public int PayrollFormulaId { get; set; }
        [ForeignKey("PayrollFormulaId")] public virtual PayrollFormula PayrollFormula { get; set; } = null!;

        public int? SalaryComponentTypeId { get; set; }
        [ForeignKey("SalaryComponentTypeId")] public virtual SalaryComponentType? SalaryComponentType { get; set; }

        [StringLength(80)] public required string ComponentCode { get; set; }
        public required string Expression { get; set; }

        public int CalculationOrder { get; set; }
        public bool IsGrossComponent { get; set; }
        public bool IsTaxable { get; set; }
        public bool IsInsuranceBased { get; set; }
        public bool IsDeduction { get; set; }
        public bool IsSnapshotRequired { get; set; } = true;

        [StringLength(1000)] public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
