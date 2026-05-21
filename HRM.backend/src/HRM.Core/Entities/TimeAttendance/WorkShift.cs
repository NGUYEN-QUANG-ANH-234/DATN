using HRM.backend.src.HRM.Core.Entities.Organization;
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

        // BỔ SUNG: Quản lý giờ nghỉ trưa (Break time)
        public TimeSpan? BreakStartTime { get; set; }
        public TimeSpan? BreakEndTime { get; set; }

        // Cập nhật ngưỡng đi muộn / về sớm
        public int LateThresholdMins { get; set; } = 15;

        // BỔ SUNG: Ngưỡng về sớm (Ví dụ: 0 phút)
        public int EarlyLeaveThresholdMins { get; set; } = 0;

        // BỔ SUNG: Trạng thái ca làm (Để ẩn khỏi Dropdown nếu công ty không áp dụng ca này nữa)
        public bool IsActive { get; set; } = true;

        public int? DeptId { get; set; }
        [ForeignKey("DeptId")] public virtual Department? Department { get; set; }

        // Navigation Property: 1 Ca làm việc có nhiều lượt chấm công
        public virtual ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();
    }
}