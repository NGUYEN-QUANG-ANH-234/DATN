using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.TimeAttendance
{
    [Table("leave_types")]
    public class LeaveType
    {
        [Key] public int Id { get; set; }

        [StringLength(50)] public string? TypeName { get; set; }
        public bool IsPaid { get; set; } = true;

        // --- Navigation Properties ---
        public virtual ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
        public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    }
}