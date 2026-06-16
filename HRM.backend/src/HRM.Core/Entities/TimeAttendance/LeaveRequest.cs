using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;
 using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;

namespace HRM.backend.src.HRM.Core.Entities.TimeAttendance
{
    [Table("leave_requests")]
    public class LeaveRequest
    {
        [Key] public int Id { get; set; }

        public int? EmployeeId { get; set; }
         [ForeignKey("EmployeeId")] public virtual Employee? Employee { get; set; }

        public int? LeaveTypeId { get; set; }
        [ForeignKey("LeaveTypeId")] public virtual LeaveType? LeaveType { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string? Reason { get; set; }

        public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;

        public DateTime? DeadlineAt { get; set; } // Phục vụ SLA duyệt tự động

        public bool IsPayrollLocked { get; set; }

        [StringLength(7)]
        public string? PayrollPeriod { get; set; }

        public DateTime? PayrollLockedAt { get; set; }
    }
}
