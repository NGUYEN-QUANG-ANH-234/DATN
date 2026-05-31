using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.TasksTraining
{
    [Table("penalty_records")]
    public class PenaltyRecord
    {
        [Key] public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey(nameof(EmployeeId))] public virtual Employee? Employee { get; set; }

        [StringLength(10)] public required string Period { get; set; }

        public PenaltySourceType SourceType { get; set; }
        public int? ReferenceId { get; set; }

        [StringLength(80)] public required string RuleCode { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal PenaltyPoint { get; set; }

        [StringLength(1000)] public string? Reason { get; set; }

        public PenaltyRecordStatus Status { get; set; } = PenaltyRecordStatus.PendingHRReview;
        public DateTime? OccurredAt { get; set; }
        public ViolationType ViolationType { get; set; } = ViolationType.Manual;
        public PenaltySeverity Severity { get; set; } = PenaltySeverity.Low;

        public bool AffectsAttendance { get; set; }
        public bool AffectsPerformance { get; set; } = true;
        public bool AffectsPersonnelDecision { get; set; }

        public bool CreatedBySystem { get; set; } = true;
        public int? CreatedByAccountId { get; set; }
        [ForeignKey(nameof(CreatedByAccountId))] public virtual Account? CreatedByAccount { get; set; }

        [StringLength(1000)] public string? EmployeeExplanation { get; set; }
        [StringLength(1000)] public string? ManagerNote { get; set; }
        [StringLength(1000)] public string? HRNote { get; set; }
        [StringLength(500)] public string? EvidenceFilePath { get; set; }

        public int? ApprovedByAccountId { get; set; }
        [ForeignKey(nameof(ApprovedByAccountId))] public virtual Account? ApprovedByAccount { get; set; }

        public int? AttendanceAdjustmentLogId { get; set; }
        [ForeignKey(nameof(AttendanceAdjustmentLogId))] public virtual AttendanceAdjustmentLog? AttendanceAdjustmentLog { get; set; }

        public int? DeductedMinutes { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? DeductedWorkday { get; set; }

        public int? PerformanceReviewId { get; set; }
        [ForeignKey(nameof(PerformanceReviewId))] public virtual PerformanceReview? PerformanceReview { get; set; }

        public DateTime? ReviewedAt { get; set; }
        public DateTime? AppliedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
