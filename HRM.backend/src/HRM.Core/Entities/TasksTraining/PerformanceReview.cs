using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.TasksTraining
{
    [Table("performance_reviews")]
    public class PerformanceReview
    {
        [Key] public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")] public virtual Employee? Employee { get; set; }

        public int? DeptId { get; set; }
        [ForeignKey("DeptId")] public virtual Department? Department { get; set; }

        public int? ImportBatchId { get; set; }
        [ForeignKey("ImportBatchId")] public virtual KpiImportBatch? ImportBatch { get; set; }

        public int? CreatedByAccountId { get; set; }
        [ForeignKey("CreatedByAccountId")] public virtual Account? CreatedByAccount { get; set; }

        public int? ReviewerAccountId { get; set; }
        [ForeignKey("ReviewerAccountId")] public virtual Account? ReviewerAccount { get; set; }

        [StringLength(10)] public required string Period { get; set; }

        public int TotalWeight { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TotalScore { get; set; }

        [StringLength(5)] public string? FinalRating { get; set; }

        [StringLength(1000)] public string? FinalComment { get; set; }

        public ReviewStatus Status { get; set; } = ReviewStatus.Draft;

        public DateTime? ReviewDeadline { get; set; }
        public DateTime? FinalizedAt { get; set; }
        public bool IsPayrollSynced { get; set; }
        public DateTime? PayrollSyncedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<PerformanceDetail> Details { get; set; } = new List<PerformanceDetail>();
    }
}
