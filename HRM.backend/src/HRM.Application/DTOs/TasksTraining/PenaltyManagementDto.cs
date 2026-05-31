namespace HRM.backend.src.HRM.Application.DTOs.TasksTraining
{
    public class CreateManualPenaltyRecordDto
    {
        public int EmployeeId { get; set; }
        public DateTime OccurredAt { get; set; }
        public string? Period { get; set; }
        public string ViolationType { get; set; } = "Manual";
        public string Severity { get; set; } = "Low";
        public required string Description { get; set; }
        public decimal PenaltyPoint { get; set; }
        public bool RequiresEmployeeExplanation { get; set; }
        public bool AffectsAttendance { get; set; }
        public bool AffectsPerformance { get; set; } = true;
        public bool AffectsPersonnelDecision { get; set; }
        public int? DeductedMinutes { get; set; }
        public decimal? DeductedWorkday { get; set; }
        public string? EvidenceFilePath { get; set; }
        public string? ManagerNote { get; set; }
        public string? RuleCode { get; set; }
    }

    public class SubmitPenaltyExplanationDto
    {
        public required string Explanation { get; set; }
        public string? EvidenceFilePath { get; set; }
    }

    public class ReviewPenaltyRecordDto
    {
        public bool IsApproved { get; set; }
        public string? Note { get; set; }
    }

    public class PenaltyRecordResponseDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public string Period { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public int? ReferenceId { get; set; }
        public string RuleCode { get; set; } = string.Empty;
        public decimal PenaltyPoint { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? OccurredAt { get; set; }
        public string ViolationType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public bool AffectsAttendance { get; set; }
        public bool AffectsPerformance { get; set; }
        public bool AffectsPersonnelDecision { get; set; }
        public bool CreatedBySystem { get; set; }
        public int? CreatedByAccountId { get; set; }
        public string? EmployeeExplanation { get; set; }
        public string? ManagerNote { get; set; }
        public string? HRNote { get; set; }
        public string? EvidenceFilePath { get; set; }
        public int? ApprovedByAccountId { get; set; }
        public int? AttendanceAdjustmentLogId { get; set; }
        public int? DeductedMinutes { get; set; }
        public decimal? DeductedWorkday { get; set; }
        public int? PerformanceReviewId { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime? AppliedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool RequiresDirectorReview { get; set; }
    }
}
