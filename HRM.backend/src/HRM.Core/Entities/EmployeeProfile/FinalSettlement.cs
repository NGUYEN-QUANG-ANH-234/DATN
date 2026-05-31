using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.EmployeeProfile
{
    [Table("final_settlements")]
    public class FinalSettlement
    {
        [Key] public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")] public virtual Employee Employee { get; set; } = null!;

        public int? TerminationRequestId { get; set; }
        [ForeignKey("TerminationRequestId")] public virtual TerminationRequest? TerminationRequest { get; set; }

        public TerminationType TerminationType { get; set; }
        public DateTime LastWorkingDate { get; set; }

        [Column(TypeName = "decimal(15,2)")] public decimal UnpaidSalaryAmount { get; set; }
        [Column(TypeName = "decimal(5,2)")] public decimal UnusedAnnualLeaveDays { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal UnusedAnnualLeaveAmount { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal SeveranceAllowanceAmount { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal JobLossAllowanceAmount { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal HalfMonthSalaryCompensation { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal InsufficientNoticeCompensation { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal TrainingCostCompensation { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal AssetCompensation { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal OtherDeductions { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal TaxAmount { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal InsuranceAmount { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal FinalNetAmount { get; set; }

        public FinalSettlementStatus Status { get; set; } = FinalSettlementStatus.Draft;
        public string? CalculationSnapshotJson { get; set; }

        public int? ApprovedByAccountId { get; set; }
        [ForeignKey("ApprovedByAccountId")] public virtual Account? ApprovedByAccount { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? LockedByAccountId { get; set; }
        [ForeignKey("LockedByAccountId")] public virtual Account? LockedByAccount { get; set; }
        public DateTime? LockedAt { get; set; }
        public DateTime? PaidAt { get; set; }

        [StringLength(1000)] public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
