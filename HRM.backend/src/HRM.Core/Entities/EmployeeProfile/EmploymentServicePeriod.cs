using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.EmployeeProfile
{
    [Table("employment_service_periods")]
    public class EmploymentServicePeriod
    {
        [Key] public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")] public virtual Employee Employee { get; set; } = null!;

        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public EmploymentServicePeriodType PeriodType { get; set; } = EmploymentServicePeriodType.OfficialWork;

        public bool IsActualWorkingTime { get; set; } = true;
        public bool IsSocialInsuranceContributed { get; set; }
        public bool IsUnemploymentInsuranceContributed { get; set; }
        public bool IsExcludedFromSeverance { get; set; }
        public bool IsSeverancePaid { get; set; }
        public bool IsJobLossPaid { get; set; }

        [StringLength(100)] public string? SourceType { get; set; }
        public int? SourceId { get; set; }
        [StringLength(1000)] public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
