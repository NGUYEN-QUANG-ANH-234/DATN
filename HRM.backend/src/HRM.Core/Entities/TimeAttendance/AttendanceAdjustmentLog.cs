using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.System;

namespace HRM.backend.src.HRM.Core.Entities.TimeAttendance
{
    [Table("attendance_adjustment_logs")]
    public class AttendanceAdjustmentLog
    {
        [Key] public int Id { get; set; }

        public int AttendanceDailySummaryId { get; set; }
        [ForeignKey("AttendanceDailySummaryId")] public virtual AttendanceDailySummary AttendanceDailySummary { get; set; } = null!;

        public string? OldValueJson { get; set; }
        public string? NewValueJson { get; set; }

        public int AdjustedByAccountId { get; set; }
        [ForeignKey("AdjustedByAccountId")] public virtual Account AdjustedByAccount { get; set; } = null!;

        public DateTime AdjustedAt { get; set; } = DateTime.UtcNow;
        [StringLength(1000)] public required string Reason { get; set; }
    }
}
