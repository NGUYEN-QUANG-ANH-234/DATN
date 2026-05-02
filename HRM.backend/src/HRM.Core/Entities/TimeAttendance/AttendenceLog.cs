using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;

namespace HRM.backend.src.HRM.Core.Entities.TimeAttendance
{
    [Table("attendance_logs")]
    public class AttendanceLog
    {
        [Key] public int Id { get; set; }

        public int? EmployeeId { get; set; }
         [ForeignKey("EmployeeId")] public virtual Employee? Employee { get; set; }

        public int? ShiftId { get; set; }
        [ForeignKey("ShiftId")] public virtual WorkShift? WorkShift { get; set; }

        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }

        [StringLength(45)] public string? IpAddress { get; set; }
        [Column(TypeName = "decimal(10,8)")] public decimal GpsLat { get; set; }
        [Column(TypeName = "decimal(11,8)")] public decimal GpsLong { get; set; }

        public AttendanceStatus Status { get; set; } = AttendanceStatus.Valid;
    }
}