using HRM.backend.src.HRM.Core.Enums;

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
        public int WorkedMinutes { get; set; }
        public decimal WorkedHours { get; set; }
        public decimal PayableWorkHours { get; set; }
        public int LateMinutes { get; set; }
        public int EarlyLeaveMinutes { get; set; }
        public int ActualOtMinutes { get; set; }
        public bool IsPayrollLocked { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class AttendanceDailySummaryResponseDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public DateTime WorkDate { get; set; }
        public DateTime? FirstCheckIn { get; set; }
        public DateTime? LastCheckOut { get; set; }
        public int WorkingMinutes { get; set; }
        public int LateMinutes { get; set; }
        public int EarlyLeaveMinutes { get; set; }
        public int OvertimeMinutes { get; set; }
        public decimal WorkdayValue { get; set; }
        public AttendanceDailyStatus AttendanceStatus { get; set; }
        public AttendancePayrollApprovalStatus ApprovalStatus { get; set; }
        public bool IsManualAdjusted { get; set; }
        public string? AdjustmentReason { get; set; }
        public bool IsPayrollLocked { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class AdjustAttendanceDailySummaryDto
    {
        public int? WorkingMinutes { get; set; }
        public int? LateMinutes { get; set; }
        public int? EarlyLeaveMinutes { get; set; }
        public int? OvertimeMinutes { get; set; }
        public decimal? WorkdayValue { get; set; }
        public AttendanceDailyStatus? AttendanceStatus { get; set; }
        public required string Reason { get; set; }
    }
}
