using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
 using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;

namespace HRM.backend.src.HRM.Core.Entities.TimeAttendance
{
    [Table("leave_balances")]
    [PrimaryKey(nameof(EmployeeId), nameof(LeaveTypeId), nameof(Year))] // Khai báo Composite Key
    public class LeaveBalance
    {
        public int EmployeeId { get; set; }
         [ForeignKey("EmployeeId")] public virtual Employee? Employee { get; set; }

        public int LeaveTypeId { get; set; }
        [ForeignKey("LeaveTypeId")] public virtual LeaveType? LeaveType { get; set; }

        public short Year { get; set; }

        [Column(TypeName = "decimal(4,1)")] public decimal? TotalDays { get; set; }
        [Column(TypeName = "decimal(4,1)")] public decimal? UsedDays { get; set; } = 0;
    }
}