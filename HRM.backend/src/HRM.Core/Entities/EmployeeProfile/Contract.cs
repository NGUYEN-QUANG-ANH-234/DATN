using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.EmployeeProfile
{
    [Table("contracts")]
    public class Contract
    {
        [Key] public int Id { get; set; }
        public int Version { get; set; } = 1; // Quản lý bản Draft v1, v2...
        public int? EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        [StringLength(50)] public required string ContractNumber { get; set; }

        public ContractType ContractType { get; set; } = ContractType.Probation;
        public PayBasis PayBasis { get; set; } = PayBasis.Monthly;
        public TaxMethod? TaxMethodOverride { get; set; }
        public bool IsInsuranceEligible { get; set; } = true;

        [Column(TypeName = "decimal(15,2)")] public decimal BasicSalary { get; set; }

        [Column(TypeName = "decimal(5,2)")] public decimal SalaryPercentage { get; set; } = 100.0m;
        [Column(TypeName = "decimal(15,2)")] public decimal? HourlyRate { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? DailyRate { get; set; }
        [Column(TypeName = "decimal(5,2)")] public decimal StandardHoursPerDaySnapshot { get; set; } = 8m;
        [Column(TypeName = "decimal(5,2)")] public decimal StandardWorkdaysSnapshot { get; set; } = 22m;
        public int? PayrollFormulaId { get; set; }

        [ForeignKey("PayrollFormulaId")] public virtual PayrollFormula? PayrollFormula { get; set; }

        [Column(TypeName = "decimal(15,2)")] 
        public decimal InsuranceSalary { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        [StringLength(1000)]
        public string? NegotiationNote { get; set; } // Ý kiến thương lượng của nhân viên

        // Các mốc SLA để Worker chạy ngầm quét
        public DateTime? EmployeeDeadline { get; set; }
        public DateTime? DirectorDeadline { get; set; }

        public ContractStatus Status { get; set; } = ContractStatus.Draft;

        public virtual ICollection<ContractAddendum> Addendums { get; set; } = new List<ContractAddendum>();
    }
}
