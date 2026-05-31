using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.PayrollAllowances
{
    [Table("payroll_contract_segments")]
    public class PayrollContractSegment
    {
        [Key] public int Id { get; set; }

        public int? PayrollId { get; set; }
        [ForeignKey("PayrollId")] public virtual Payroll? Payroll { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")] public virtual Employee Employee { get; set; } = null!;

        public int ContractId { get; set; }
        [ForeignKey("ContractId")] public virtual Contract Contract { get; set; } = null!;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ContractType ContractType { get; set; }
        public PayBasis PayBasis { get; set; }
        public TaxMethod TaxMethod { get; set; }
        public bool IsInsuranceEligible { get; set; }
        public PayrollContractSegmentType SegmentType { get; set; } = PayrollContractSegmentType.Contract;

        [Column(TypeName = "decimal(15,2)")] public decimal BaseSalary { get; set; }
        [Column(TypeName = "decimal(5,2)")] public decimal SalaryPercentage { get; set; }
        [Column(TypeName = "decimal(5,2)")] public decimal StandardWorkdays { get; set; }
        [Column(TypeName = "decimal(5,2)")] public decimal ActualWorkdays { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal SalaryAmount { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal TaxableAmount { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal InsuranceBaseAmount { get; set; }

        public string? SnapshotJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
