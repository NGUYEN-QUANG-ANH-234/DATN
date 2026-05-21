using System.ComponentModel.DataAnnotations;

namespace HRM.backend.src.HRM.Application.DTOs.System
{
    public class ConfigureWorkScheduleDto
    {
        // --- Cấu hình Ca làm việc (WorkShift) ---
        [Required, StringLength(50)]
        public string ShiftName { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public TimeSpan? BreakStartTime { get; set; }
        public TimeSpan? BreakEndTime { get; set; }
        public int LateThresholdMins { get; set; } = 15;
        public int EarlyLeaveThresholdMins { get; set; } = 0;

        // --- Định biên ngày nghỉ theo Bộ phận (LeaveBalance) ---
        public int DeptId { get; set; }
        public int LeaveTypeId { get; set; }
        public short Year { get; set; }
        public decimal TotalDays { get; set; } // Số ngày phép được cấp trong năm
    }

    public class LeaveTypeSelectDto
    {
        public int Id { get; set; }
        public string TypeName { get; set; } = string.Empty;
    }
}
