using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.DTOs.PersonnelChanges
{
    public class PersonnelChangeResponseDto
    {
        public bool Success { get; set; } = true;
        public string? Message { get; set; }
        public PersonnelChangeDetailDto? Data { get; set; }
    }

    public class PersonnelChangeListItemDto
    {
        public int Id { get; set; }
        public int? EmployeeId { get; set; }
        public int? EmployeeAccountId { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public PersonnelChangeType ChangeType { get; set; }
        public PersonnelChangePromotionType? PromotionType { get; set; }
        public PersonnelChangeStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public int RequestedByAccountId { get; set; }
        public string? RequestedByName { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public string? Reason { get; set; }
        public bool RequiresEmployeeConsent { get; set; }
        public PersonnelChangeConsentStatus EmployeeConsentStatus { get; set; }
        public bool RequiresContractFlow { get; set; }
        public PersonnelChangeContractFlowType ContractFlowType { get; set; }
        public bool RequiresDirectorApproval { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class PersonnelChangeDetailDto : PersonnelChangeListItemDto
    {
        public int? CurrentDepartmentId { get; set; }
        public string? CurrentDepartmentName { get; set; }
        public int? CurrentPositionId { get; set; }
        public string? CurrentPositionName { get; set; }
        public int? CurrentManagerId { get; set; }
        public int? CurrentManagerAccountId { get; set; }
        public string? CurrentManagerName { get; set; }
        public int? CurrentJobLevelId { get; set; }
        public string? CurrentJobLevelName { get; set; }
        public EmployeeType? CurrentEmployeeType { get; set; }

        public int? NewDepartmentId { get; set; }
        public string? NewDepartmentName { get; set; }
        public int? NewPositionId { get; set; }
        public string? NewPositionName { get; set; }
        public int? NewManagerId { get; set; }
        public string? NewManagerName { get; set; }
        public int? NewJobLevelId { get; set; }
        public string? NewJobLevelName { get; set; }
        public EmployeeType? NewEmployeeType { get; set; }

        public DateTime? EmployeeConsentAt { get; set; }
        public string? EmployeeConsentNote { get; set; }
        public int? RelatedContractId { get; set; }
        public int? RelatedContractRequestId { get; set; }
        public int? RelatedContractAddendumId { get; set; }
        public string? ContractFlowStatus { get; set; }
        public int? DirectorApprovedByAccountId { get; set; }
        public string? DirectorApprovedByName { get; set; }
        public DateTime? DirectorApprovedAt { get; set; }
        public string? DirectorNote { get; set; }
        public bool RequiresHRProcessing { get; set; }
        public int? HRAssignedAccountId { get; set; }
        public string? HRAssignedName { get; set; }
        public string? HRNote { get; set; }
        public DateTime? HRProcessedAt { get; set; }
        public DateTime? EmployeeNotifiedAt { get; set; }
        public DateTime? ResponseDeadlineAt { get; set; }
        public string? EvidenceFilePath { get; set; }
        public string? ManagerNote { get; set; }
        public string? EmployeeExplanation { get; set; }
        public DateTime? EmployeeExplanationAt { get; set; }
        public bool LockAccountOnExecution { get; set; }
        public DateTime? AccountLockedAt { get; set; }
        public bool RequiresFinalSettlement { get; set; }
        public int? RelatedFinalSettlementId { get; set; }
        public int? SourcePenaltyRecordId { get; set; }
        public int? SourcePerformanceReviewId { get; set; }
        public string? DecisionNumber { get; set; }
        public string? DecisionFilePath { get; set; }
        public DateTime? DecisionIssuedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? RejectedReason { get; set; }

        public List<PersonnelChangeApprovalDto> Approvals { get; set; } = new();
        public List<PersonnelChangeContractFlowDto> ContractLinks { get; set; } = new();
        public List<PersonnelChangeTimelineDto> Histories { get; set; } = new();
    }

    public class PersonnelChangeApprovalDto
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public string StepName { get; set; } = string.Empty;
        public string ApproverRole { get; set; } = string.Empty;
        public int? ApproverAccountId { get; set; }
        public string? ApproverName { get; set; }
        public PersonnelChangeApprovalDecision Decision { get; set; }
        public string? Note { get; set; }
        public DateTime? DecidedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
