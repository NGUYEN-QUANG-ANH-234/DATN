using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.EmployeeProfile
{
    [Table("maternity_leaves")]
    public class MaternityLeave
    {
        [Key] public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")] public virtual Employee Employee { get; set; } = null!;

        public int? LeaveRequestId { get; set; }
        [ForeignKey("LeaveRequestId")] public virtual LeaveRequest? LeaveRequest { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? ExpectedReturnDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }

        public MaternityLeaveStatus Status { get; set; } = MaternityLeaveStatus.Draft;

        public int? ApprovedByAccountId { get; set; }
        [ForeignKey("ApprovedByAccountId")] public virtual Account? ApprovedByAccount { get; set; }
        public DateTime? ApprovedAt { get; set; }

        [StringLength(1000)] public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
