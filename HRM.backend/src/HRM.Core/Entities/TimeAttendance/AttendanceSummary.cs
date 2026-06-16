using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.TimeAttendance
{
    [Table("attendance_summaries")]
    public class AttendanceSummary
    {
        [Key] public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")] public virtual Employee Employee { get; set; } = null!;

        public byte Month { get; set; }
        public short Year { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal WorkDays { get; set; }

        public int WorkedMinutes { get; set; }

        [Column(TypeName = "decimal(7,2)")]
        public decimal PayableWorkHours { get; set; }

        public int LateMinutes { get; set; }
        public int EarlyLeaveMinutes { get; set; }
        public int ActualOtMinutes { get; set; }

        public AttendancePayrollApprovalStatus ApprovalStatus { get; set; } = AttendancePayrollApprovalStatus.Draft;

        public int? SubmittedByAccountId { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int? ApprovedByAccountId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? LockedByAccountId { get; set; }
        public DateTime? LockedAt { get; set; }

        [StringLength(500)]
        public string? PeriodNote { get; set; }

        public bool IsPayrollLocked { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
