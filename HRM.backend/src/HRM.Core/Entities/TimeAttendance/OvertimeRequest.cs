using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.TimeAttendance
{
    [Table("overtime_requests")]
    public class OvertimeRequest
    {
        [Key] public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")] public virtual Employee Employee { get; set; } = null!;

        public int RequestedByAccountId { get; set; }
        [ForeignKey(nameof(RequestedByAccountId))] public virtual Account RequestedByAccount { get; set; } = null!;

        public DateTime WorkDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }

        [StringLength(500)]
        public required string Reason { get; set; }

        [StringLength(100)]
        public string? ProjectCode { get; set; }

        public OvertimeRequestStatus Status { get; set; } = OvertimeRequestStatus.PendingManager;

        public int? ManagerReviewerAccountId { get; set; }
        [ForeignKey(nameof(ManagerReviewerAccountId))] public virtual Account? ManagerReviewerAccount { get; set; }
        public DateTime? ManagerReviewedAt { get; set; }
        [StringLength(500)] public string? ManagerNote { get; set; }

        public int? HrReviewerAccountId { get; set; }
        [ForeignKey(nameof(HrReviewerAccountId))] public virtual Account? HrReviewerAccount { get; set; }
        public DateTime? HrReviewedAt { get; set; }
        [StringLength(500)] public string? HrNote { get; set; }

        public int? DirectorReviewerAccountId { get; set; }
        [ForeignKey(nameof(DirectorReviewerAccountId))] public virtual Account? DirectorReviewerAccount { get; set; }
        public DateTime? DirectorReviewedAt { get; set; }
        [StringLength(500)] public string? DirectorNote { get; set; }

        public int ApprovedMinutes { get; set; }
        public int ActualOtMinutes { get; set; }
        public bool IsPayrollLocked { get; set; }
        [StringLength(20)] public string? PayrollPeriod { get; set; }
        public DateTime? PayrollLockedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReconciledAt { get; set; }

        public virtual ICollection<OvertimeSegment> Segments { get; set; } = new List<OvertimeSegment>();
    }
}
