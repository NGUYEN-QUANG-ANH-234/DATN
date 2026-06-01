using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.DTOs.PersonnelChanges
{
    public class PersonnelChangeCreateDto
    {
        public int EmployeeId { get; set; }
        public PersonnelChangeType ChangeType { get; set; }
        public string? Reason { get; set; }
        public DateTime? EffectiveDate { get; set; }

        public int? CurrentDepartmentId { get; set; }
        public int? CurrentPositionId { get; set; }
        public int? CurrentManagerId { get; set; }
        public int? CurrentJobLevelId { get; set; }
        public EmployeeType? CurrentEmployeeType { get; set; }

        public int? NewDepartmentId { get; set; }
        public int? NewPositionId { get; set; }
        public int? NewManagerId { get; set; }
        public int? NewJobLevelId { get; set; }
        public EmployeeType? NewEmployeeType { get; set; }

        public bool RequiresEmployeeConsent { get; set; }
        public bool RequiresContractFlow { get; set; }
        public PersonnelChangeContractFlowType ContractFlowType { get; set; } = PersonnelChangeContractFlowType.None;
        public int? RelatedContractId { get; set; }
        public int? RelatedContractRequestId { get; set; }
        public int? RelatedContractAddendumId { get; set; }

        public bool RequiresDirectorApproval { get; set; } = true;
        public bool RequiresHRProcessing { get; set; } = true;
        public int? HRAssignedAccountId { get; set; }

        public int? SourcePenaltyRecordId { get; set; }
        public int? SourcePerformanceReviewId { get; set; }
    }

    public class PersonnelChangeFilterDto
    {
        public PersonnelChangeType? ChangeType { get; set; }
        public PersonnelChangeStatus? Status { get; set; }
        public int? EmployeeId { get; set; }
        public DateTime? RequestedFrom { get; set; }
        public DateTime? RequestedTo { get; set; }
    }
}
