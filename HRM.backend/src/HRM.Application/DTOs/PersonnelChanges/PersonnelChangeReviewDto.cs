namespace HRM.backend.src.HRM.Application.DTOs.PersonnelChanges
{
    public class ReviewPersonnelChangeDto
    {
        public bool IsApproved { get; set; }
        public string? Note { get; set; }
        public int? HRAssignedAccountId { get; set; }
    }

    public class EmployeeConsentDto
    {
        public bool IsAccepted { get; set; }
        public string? Note { get; set; }
    }

    public class EmployeeExplanationDto
    {
        public string Explanation { get; set; } = string.Empty;
    }

    public class IssueDecisionDto
    {
        public string DecisionNumber { get; set; } = string.Empty;
        public string? DecisionFilePath { get; set; }
        public DateTime? DecisionIssuedAt { get; set; }
        public string? Note { get; set; }
    }

    public class ExecutePersonnelChangeDto
    {
        public DateTime? CompletedAt { get; set; }
        public string? Note { get; set; }
    }

    public class CancelPersonnelChangeDto
    {
        public string? Reason { get; set; }
    }
}
