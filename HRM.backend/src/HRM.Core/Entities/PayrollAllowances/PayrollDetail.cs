using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.PayrollAllowances
{
    [Table("payroll_details")]
    public class PayrollDetail
    {
        [Key] public int Id { get; set; }

        public int PayrollId { get; set; }
        [ForeignKey("PayrollId")] public virtual Payroll Payroll { get; set; } = null!;

        [StringLength(80)] public required string ComponentCode { get; set; }
        [StringLength(200)] public required string ComponentName { get; set; }

        [Column(TypeName = "decimal(15,2)")] public decimal Amount { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal TaxableAmount { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal InsuranceBaseAmount { get; set; }

        public bool IsIncome { get; set; }
        public bool IsDeduction { get; set; }
        public bool IsTaxable { get; set; }
        public bool IsInsuranceBased { get; set; }

        [StringLength(50)] public string? ProrationType { get; set; }
        [StringLength(50)] public string? CalculationMethod { get; set; }
        [StringLength(1000)] public string? Note { get; set; }
        public string? SnapshotJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
