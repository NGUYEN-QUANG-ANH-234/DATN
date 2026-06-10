using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.TimeAttendance
{
    [Table("company_calendar_days")]
    public class CompanyCalendarDay
    {
        [Key] public int Id { get; set; }

        public int CalendarId { get; set; }
        [ForeignKey("CalendarId")] public virtual CompanyCalendar? Calendar { get; set; }

        public DateTime Date { get; set; }
        public CompanyCalendarDayType DayType { get; set; }

        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public bool IsPaid { get; set; } = true;
        public bool IsOvertimeHoliday { get; set; }
        public bool IsWorkingDayOverride { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
