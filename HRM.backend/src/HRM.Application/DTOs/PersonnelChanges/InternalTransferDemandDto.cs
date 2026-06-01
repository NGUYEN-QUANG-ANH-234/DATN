namespace HRM.backend.src.HRM.Application.DTOs.PersonnelChanges
{
    public class InternalTransferDemandDto
    {
        public int RequestedDepartmentId { get; set; }
        public int? RequestedPositionId { get; set; }
        public int? RequestedManagerId { get; set; }
        public string? Reason { get; set; }
        public string? UrgencyLevel { get; set; }
        public DateTime? ExpectedEffectiveDate { get; set; }
        public string? RequiredSkills { get; set; }
    }

    public class HrSelectEmployeeDto
    {
        public int EmployeeId { get; set; }
        public int? NewDepartmentId { get; set; }
        public int? NewPositionId { get; set; }
        public int? NewManagerId { get; set; }
        public int? NewJobLevelId { get; set; }
        public bool RequiresContractAddendum { get; set; }
        public string? Note { get; set; }
    }

    public class CurrentManagerOpinionDto
    {
        public bool IsApproved { get; set; }
        public string? Opinion { get; set; }
    }

    public class DirectorApproveTransferDto
    {
        public bool IsApproved { get; set; }
        public string? Note { get; set; }
    }

    public class IssueTransferDecisionDto
    {
        public string DecisionNumber { get; set; } = string.Empty;
        public string? DecisionFilePath { get; set; }
        public DateTime? DecisionIssuedAt { get; set; }
        public string? Note { get; set; }
    }
}
