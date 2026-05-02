using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;
 using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;

namespace HRM.backend.src.HRM.Core.Entities.PayrollAllowances
{
    [Table("payrolls")]
    public class Payroll
    {
        [Key] public int Id { get; set; }

        public int? EmployeeId { get; set; }
         [ForeignKey("EmployeeId")] public virtual Employee? Employee { get; set; }

        public byte? Month { get; set; }
        public short? Year { get; set; }

        [Column(TypeName = "decimal(15,2)")] public decimal? GrossSalary { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? TotalAllowance { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? TotalBonus { get; set; }

        [Column(TypeName = "decimal(15,2)")] public decimal? InsuranceDeduction { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? TaxDeductionFamily { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? TaxableIncome { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? PitAmount { get; set; }

        [Column(TypeName = "decimal(15,2)")] public decimal? NetSalary { get; set; }

        public PayrollStatus Status { get; set; } = PayrollStatus.Draft;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}