namespace HRM.backend.src.HRM.Application.DTOs.PersonnelChanges
{
    public class CreateDismissalDto
    {
        public int EmployeeId { get; set; }
        public int SourcePenaltyRecordId { get; set; }
        public string? Reason { get; set; }
        public string? EvidenceFilePath { get; set; }
        public string? HRNote { get; set; }
        public string? ManagerNote { get; set; }
        public DateTime? ResponseDeadlineAt { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public int? RelatedContractId { get; set; }
        public bool LockAccountOnExecution { get; set; } = true;
        public bool RequiresFinalSettlement { get; set; } = true;
    }

    public class NotifyEmployeeDismissalDto
    {
        public string? HRNote { get; set; }
        public string? EvidenceFilePath { get; set; }
        public DateTime? EmployeeNotifiedAt { get; set; }
        public DateTime? ResponseDeadlineAt { get; set; }
        public string? Note { get; set; }
    }

    public class DismissalEmployeeExplanationDto
    {
        public string Explanation { get; set; } = string.Empty;
        public string? EvidenceFilePath { get; set; }
    }

    public class DirectorApproveDismissalDto
    {
        public bool IsApproved { get; set; }
        public string? Note { get; set; }
    }
}
