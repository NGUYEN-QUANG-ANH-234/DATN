using HRM.backend.src.HRM.Core.Enums;
using Microsoft.AspNetCore.Http;

namespace HRM.backend.src.HRM.Application.DTOs.TimeAttendance
{
    public class GenerateAttendanceSummaryDto
    {
        public byte Month { get; set; }
        public short Year { get; set; }
    }

    public class CloseAttendancePeriodDto
    {
        public byte Month { get; set; }
        public short Year { get; set; }
        public string? Note { get; set; }
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
        public AttendancePayrollApprovalStatus ApprovalStatus { get; set; }
        public int? SubmittedByAccountId { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int? ApprovedByAccountId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? LockedByAccountId { get; set; }
        public DateTime? LockedAt { get; set; }
        public string? PeriodNote { get; set; }
        public bool IsPayrollLocked { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class AttendancePeriodApprovalDto
    {
        public byte Month { get; set; }
        public short Year { get; set; }
        public string Period { get; set; } = string.Empty;
        public List<AttendanceSummaryResponseDto> Summaries { get; set; } = new();
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

    public class AttendanceAdjustmentLogResponseDto
    {
        public int Id { get; set; }
        public int AttendanceDailySummaryId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public DateTime WorkDate { get; set; }
        public int AdjustedByAccountId { get; set; }
        public string AdjustedByName { get; set; } = string.Empty;
        public DateTime AdjustedAt { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? OldValueJson { get; set; }
        public string? NewValueJson { get; set; }
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

    public class ImportAttendanceDailySummaryDto
    {
        public byte Month { get; set; }
        public short Year { get; set; }
        public required IFormFile File { get; set; }
        public string? Reason { get; set; }
    }

    public class AttendanceDailyImportResultDto
    {
        public int TotalRows { get; set; }
        public int UpdatedRows { get; set; }
        public int CreatedRows { get; set; }
        public int ErrorRows { get; set; }
        public List<AttendanceDailyImportErrorDto> Errors { get; set; } = new();
    }

    public class AttendanceDailyImportErrorDto
    {
        public int RowNumber { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string WorkDate { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
