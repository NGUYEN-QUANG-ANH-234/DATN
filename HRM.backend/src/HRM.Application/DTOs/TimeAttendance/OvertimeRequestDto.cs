using System.ComponentModel.DataAnnotations;

namespace HRM.backend.src.HRM.Application.DTOs.TimeAttendance
{
    public class CreateOvertimeRequestDto
    {
        public int? EmployeeId { get; set; }

        [Required]
        public DateTime WorkDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required, StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ProjectCode { get; set; }
    }

    public class CreateBulkOvertimeRequestDto
    {
        [Required, MinLength(1)]
        public List<int> EmployeeIds { get; set; } = new();

        [Required]
        public DateTime WorkDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required, StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ProjectCode { get; set; }
    }

    public class ReviewOvertimeRequestDto
    {
        public bool IsApproved { get; set; }
        public string? Note { get; set; }
    }

    public class OvertimeEmployeeOptionDto
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
    }

    public class OvertimeRequestResponseDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public int RequestedByAccountId { get; set; }
        public DateTime WorkDate { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? ProjectCode { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ManagerNote { get; set; }
        public string? HrNote { get; set; }
        public int ApprovedMinutes { get; set; }
        public int ActualOtMinutes { get; set; }
        public bool IsPayrollLocked { get; set; }
        public string? PayrollPeriod { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReconciledAt { get; set; }
    }
}
