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

        // --- Quỹ thời gian làm việc theo kỳ công ---
        public byte Month { get; set; }
        public decimal StandardWorkDays { get; set; }
        public decimal StandardHoursPerDay { get; set; } = 8m;
        public bool IncludePaidLeaveInWorkDays { get; set; } = true;
        public string? WorkingDaysOfWeek { get; set; }
        public string? HolidayDatesJson { get; set; }
        public bool LockWorkCalendar { get; set; }
        public string? CalendarNote { get; set; }
    }

    public class LeaveTypeSelectDto
    {
        public int Id { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
        public bool CountsAsUnpaidForInsurance { get; set; }
        public bool CountsAsWorkday { get; set; }
        public bool DeductAnnualLeave { get; set; }
        public bool AffectsKpiPenalty { get; set; }
    }

    public class ScheduleChangeHistoryDto
    {
        public int Id { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string? ActorName { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
