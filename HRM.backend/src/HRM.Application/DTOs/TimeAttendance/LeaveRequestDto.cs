using System.ComponentModel.DataAnnotations;

namespace HRM.backend.src.HRM.Application.DTOs.TimeAttendance
{
    public class CreateLeaveRequestDto
    {
        [Required]
        public int LeaveTypeId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required, StringLength(1000)]
        public string Reason { get; set; } = string.Empty;
    }

    public class ReviewLeaveRequestDto
    {
        public bool IsApproved { get; set; }
        public string? Note { get; set; }
    }

    public class LeaveRequestResponseDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public int LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public bool IsPaidLeave { get; set; }
        public string LeaveCategory { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal RequestedDays { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? DeadlineAt { get; set; }
    }
}
