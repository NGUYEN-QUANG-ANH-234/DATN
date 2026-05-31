using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.PayrollAllowances
{
    [Table("monthly_insurance_statuses")]
    public class MonthlyInsuranceStatus
    {
        [Key] public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")] public virtual Employee Employee { get; set; } = null!;

        public byte Month { get; set; }
        public short Year { get; set; }
        [StringLength(7)] public required string PayrollPeriod { get; set; }

        [Column(TypeName = "decimal(15,2)")] public decimal InsuranceSalary { get; set; }
        [Column(TypeName = "decimal(5,2)")] public decimal UnpaidLeaveWorkingDays { get; set; }
        [Column(TypeName = "decimal(5,2)")] public decimal MaternityLeaveDays { get; set; }
        [Column(TypeName = "decimal(5,2)")] public decimal SickLeaveDays { get; set; }
        [Column(TypeName = "decimal(5,2)")] public decimal OfficialContractWorkingDays { get; set; }

        public bool IsSocialInsuranceContributed { get; set; } = true;
        public bool IsUnemploymentInsuranceContributed { get; set; } = true;
        public InsuranceContributionStatus Status { get; set; } = InsuranceContributionStatus.Pending;
        [StringLength(500)] public string? NonContributionReason { get; set; }

        [Column(TypeName = "decimal(15,2)")] public decimal EmployeeInsuranceAmount { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal EmployerContributionAmount { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal UnemploymentInsuranceAmount { get; set; }
        public string? ConfigSnapshotJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
