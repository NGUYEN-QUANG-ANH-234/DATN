using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;

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

        [StringLength(7)] public string? Period { get; set; }

        [Column(TypeName = "decimal(15,2)")] public decimal? BaseSalary { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? BaseSalaryActual { get; set; }

        [Column(TypeName = "decimal(15,2)")] public decimal? GrossSalary { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? GrossIncome { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? TotalAllowance { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? TotalBonus { get; set; }

        [Column(TypeName = "decimal(15,2)")] public decimal? InsuranceSalary { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? InsuranceDeduction { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? EmployeeInsuranceAmount { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? EmployerContributionAmount { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? TaxDeductionFamily { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? TaxableGrossIncome { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? TaxableIncome { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? PitAmount { get; set; }

        [Column(TypeName = "decimal(15,2)")] public decimal? OtherDeductions { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? NetSalary { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? TotalCompanyCost { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? TaxDeductionPersonal { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? AdvancePayment { get; set; }
        [Column(TypeName = "decimal(5,2)")] public decimal? ActualWorkDays { get; set; }
        [Column(TypeName = "decimal(7,2)")] public decimal? ActualWorkHours { get; set; }
        public int? ActualOtMinutes { get; set; }

        public string? FormulaSnapshotJson { get; set; }
        public string? PolicySnapshotJson { get; set; }

        public PayrollStatus Status { get; set; } = PayrollStatus.Draft;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CalculatedAt { get; set; }
        public int? CalculatedByAccountId { get; set; }
        [ForeignKey("CalculatedByAccountId")] public virtual Account? CalculatedByAccount { get; set; }

        public DateTime? SubmittedAt { get; set; }
        public int? SubmittedByAccountId { get; set; }
        [ForeignKey("SubmittedByAccountId")] public virtual Account? SubmittedByAccount { get; set; }

        public DateTime? ApprovedAt { get; set; }
        public int? ApprovedByAccountId { get; set; }
        [ForeignKey("ApprovedByAccountId")] public virtual Account? ApprovedByAccount { get; set; }

        public DateTime? LockedAt { get; set; }
        public int? LockedByAccountId { get; set; }
        [ForeignKey("LockedByAccountId")] public virtual Account? LockedByAccount { get; set; }

        [StringLength(1000)] public string? ReviewNote { get; set; }

        public virtual ICollection<PayrollDetail> Details { get; set; } = new List<PayrollDetail>();
        public virtual ICollection<PayrollContractSegment> ContractSegments { get; set; } = new List<PayrollContractSegment>();
    }
}
