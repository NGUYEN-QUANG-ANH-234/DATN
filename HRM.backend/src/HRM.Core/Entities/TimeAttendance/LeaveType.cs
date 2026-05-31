using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.TimeAttendance
{
    [Table("leave_types")]
    public class LeaveType
    {
        [Key] public int Id { get; set; }

        [StringLength(50)] public string? TypeName { get; set; }
        public bool IsPaid { get; set; } = true;
        public LeaveCategory Category { get; set; } = LeaveCategory.AnnualPaid;
        public bool CountsAsUnpaidForInsurance { get; set; }
        public bool CountsAsWorkday { get; set; } = true;
        public bool DeductAnnualLeave { get; set; } = true;
        public bool AffectsKpiPenalty { get; set; }

        public virtual ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
        public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    }
}
