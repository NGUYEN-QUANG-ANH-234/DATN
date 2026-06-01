using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.DTOs.PersonnelChanges
{
    public class CreatePromotionDto
    {
        public int EmployeeId { get; set; }
        public PersonnelChangePromotionType PromotionType { get; set; } = PersonnelChangePromotionType.PositionPromotion;
        public int? NewPositionId { get; set; }
        public int? NewJobLevelId { get; set; }
        public EmployeeType? NewEmployeeType { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public string? Reason { get; set; }
        public int? SourcePerformanceReviewId { get; set; }
        public bool RequiresContractFlow { get; set; }
        public PersonnelChangeContractFlowType ContractFlowType { get; set; } = PersonnelChangeContractFlowType.ContractAddendum;
        public int? RelatedContractId { get; set; }
    }

    public class CreateConvertOfficialDto
    {
        public int EmployeeId { get; set; }
        public int? NewPositionId { get; set; }
        public int? NewJobLevelId { get; set; }
        public EmployeeType NewEmployeeType { get; set; } = EmployeeType.Official;
        public DateTime? EffectiveDate { get; set; }
        public string? Reason { get; set; }
        public int? SourcePerformanceReviewId { get; set; }
        public bool RequiresContractFlow { get; set; } = true;
        public PersonnelChangeContractFlowType ContractFlowType { get; set; } = PersonnelChangeContractFlowType.ContractRenewal;
        public int? RelatedContractId { get; set; }
    }

    public class ApprovePromotionDto
    {
        public bool IsApproved { get; set; }
        public string? Note { get; set; }
        public int? HRAssignedAccountId { get; set; }
        public bool? RequiresContractFlow { get; set; }
        public PersonnelChangeContractFlowType? ContractFlowType { get; set; }
        public int? RelatedContractId { get; set; }
    }
}
