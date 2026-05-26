namespace HRM.backend.src.HRM.Application.DTOs.TimeAttendance
{
    public class GenerateAttendanceSummaryDto
    {
        public byte Month { get; set; }
        public short Year { get; set; }
    }

    public class AttendanceSummaryResponseDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public byte Month { get; set; }
        public short Year { get; set; }
        public decimal WorkDays { get; set; }
        public int LateMinutes { get; set; }
        public int EarlyLeaveMinutes { get; set; }
        public int ActualOtMinutes { get; set; }
        public bool IsPayrollLocked { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
