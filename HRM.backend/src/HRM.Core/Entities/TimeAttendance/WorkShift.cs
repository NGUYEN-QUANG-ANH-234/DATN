using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.TimeAttendance
{
    [Table("work_shifts")]
    public class WorkShift
    {
        [Key] public int Id { get; set; }

        [StringLength(50)] public required string ShiftName { get; set; }

        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }

        public int LateThresholdMins { get; set; } = 15;

        // Navigation Property: 1 Ca làm việc có nhiều lượt chấm công
        public virtual ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();
    }
}