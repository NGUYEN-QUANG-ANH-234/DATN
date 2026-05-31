using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.PayrollAllowances
{
    [Table("payroll_adjustments")]
    public class PayrollAdjustment
    {
        [Key] public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")] public virtual Employee Employee { get; set; } = null!;

        public int? RelatedPayrollId { get; set; }
        [ForeignKey("RelatedPayrollId")] public virtual Payroll? RelatedPayroll { get; set; }

        public PayrollAdjustmentType AdjustmentType { get; set; } = PayrollAdjustmentType.ManualCorrection;

        public byte RecognizedMonth { get; set; }
        public short RecognizedYear { get; set; }
        [StringLength(7)] public required string RecognizedPayrollPeriod { get; set; }

        [StringLength(7)] public string? EffectiveFromMonth { get; set; }
        [StringLength(7)] public string? EffectiveToMonth { get; set; }

        [Column(TypeName = "decimal(15,2)")] public decimal Amount { get; set; }
        public bool IsTaxable { get; set; } = true;
        public bool IsInsuranceBased { get; set; }
        public bool IsDeduction { get; set; }

        public PayrollAdjustmentStatus Status { get; set; } = PayrollAdjustmentStatus.Draft;
        [StringLength(1000)] public required string Reason { get; set; }

        public int? ApprovedByAccountId { get; set; }
        [ForeignKey("ApprovedByAccountId")] public virtual Account? ApprovedByAccount { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public int? AppliedPayrollId { get; set; }
        [ForeignKey("AppliedPayrollId")] public virtual Payroll? AppliedPayroll { get; set; }
        public DateTime? AppliedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
