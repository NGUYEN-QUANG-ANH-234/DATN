using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.TimeAttendance
{
    [Table("attendance_daily_summaries")]
    public class AttendanceDailySummary
    {
        [Key] public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")] public virtual Employee Employee { get; set; } = null!;

        public DateTime WorkDate { get; set; }
        public DateTime? FirstCheckIn { get; set; }
        public DateTime? LastCheckOut { get; set; }

        public int WorkingMinutes { get; set; }
        public int LateMinutes { get; set; }
        public int EarlyLeaveMinutes { get; set; }
        public int OvertimeMinutes { get; set; }

        [Column(TypeName = "decimal(5,2)")] public decimal WorkdayValue { get; set; }

        public AttendanceDailyStatus AttendanceStatus { get; set; } = AttendanceDailyStatus.Present;
        public AttendancePayrollApprovalStatus ApprovalStatus { get; set; } = AttendancePayrollApprovalStatus.Draft;

        public int? LeaveRequestId { get; set; }
        [ForeignKey("LeaveRequestId")] public virtual LeaveRequest? LeaveRequest { get; set; }

        public bool IsManualAdjusted { get; set; }
        public int? AdjustedByAccountId { get; set; }
        [ForeignKey("AdjustedByAccountId")] public virtual Account? AdjustedByAccount { get; set; }
        public DateTime? AdjustedAt { get; set; }
        [StringLength(1000)] public string? AdjustmentReason { get; set; }

        [StringLength(7)] public string? PayrollPeriod { get; set; }
        public bool IsPayrollLocked { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<AttendanceAdjustmentLog> AdjustmentLogs { get; set; } = new List<AttendanceAdjustmentLog>();
    }
}
