using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;

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

        public int LateMinutes { get; set; }
        public int EarlyLeaveMinutes { get; set; }
        public int ActualOtMinutes { get; set; }

        public bool IsPayrollLocked { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
