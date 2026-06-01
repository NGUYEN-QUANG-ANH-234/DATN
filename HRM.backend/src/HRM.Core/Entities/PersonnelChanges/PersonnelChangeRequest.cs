using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.PersonnelChanges
{
    [Table("personnel_change_requests")]
    public class PersonnelChangeRequest
    {
        [Key] public int Id { get; set; }

        public int? EmployeeId { get; set; }
        [ForeignKey(nameof(EmployeeId))] public virtual Employee? Employee { get; set; }

        public PersonnelChangeType ChangeType { get; set; }
        public PersonnelChangePromotionType? PromotionType { get; set; }
        public PersonnelChangeStatus Status { get; set; } = PersonnelChangeStatus.Draft;

        public int RequestedByAccountId { get; set; }
        [ForeignKey(nameof(RequestedByAccountId))] public virtual Account RequestedByAccount { get; set; } = null!;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        [StringLength(2000)]
        public string? Reason { get; set; }

        public DateTime? EffectiveDate { get; set; }

        public int? CurrentDepartmentId { get; set; }
        [ForeignKey(nameof(CurrentDepartmentId))] public virtual Department? CurrentDepartment { get; set; }

        public int? CurrentPositionId { get; set; }
        [ForeignKey(nameof(CurrentPositionId))] public virtual Position? CurrentPosition { get; set; }

        public int? CurrentManagerId { get; set; }
        [ForeignKey(nameof(CurrentManagerId))] public virtual Employee? CurrentManager { get; set; }

        public int? CurrentJobLevelId { get; set; }
        [ForeignKey(nameof(CurrentJobLevelId))] public virtual JobLevel? CurrentJobLevel { get; set; }

        public EmployeeType? CurrentEmployeeType { get; set; }

        public int? NewDepartmentId { get; set; }
        [ForeignKey(nameof(NewDepartmentId))] public virtual Department? NewDepartment { get; set; }

        public int? NewPositionId { get; set; }
        [ForeignKey(nameof(NewPositionId))] public virtual Position? NewPosition { get; set; }

        public int? NewManagerId { get; set; }
        [ForeignKey(nameof(NewManagerId))] public virtual Employee? NewManager { get; set; }

        public int? NewJobLevelId { get; set; }
        [ForeignKey(nameof(NewJobLevelId))] public virtual JobLevel? NewJobLevel { get; set; }

        public EmployeeType? NewEmployeeType { get; set; }

        public bool RequiresEmployeeConsent { get; set; }
        public PersonnelChangeConsentStatus EmployeeConsentStatus { get; set; } = PersonnelChangeConsentStatus.NotRequired;
        public DateTime? EmployeeConsentAt { get; set; }

        [StringLength(2000)]
        public string? EmployeeConsentNote { get; set; }

        public bool RequiresContractFlow { get; set; }
        public PersonnelChangeContractFlowType ContractFlowType { get; set; } = PersonnelChangeContractFlowType.None;

        public int? RelatedContractId { get; set; }
        [ForeignKey(nameof(RelatedContractId))] public virtual Contract? RelatedContract { get; set; }

        public int? RelatedContractRequestId { get; set; }

        public int? RelatedContractAddendumId { get; set; }
        [ForeignKey(nameof(RelatedContractAddendumId))] public virtual ContractAddendum? RelatedContractAddendum { get; set; }

        [StringLength(50)]
        public string? ContractFlowStatus { get; set; }

        public bool RequiresDirectorApproval { get; set; } = true;

        public int? DirectorApprovedByAccountId { get; set; }
        [ForeignKey(nameof(DirectorApprovedByAccountId))] public virtual Account? DirectorApprovedByAccount { get; set; }

        public DateTime? DirectorApprovedAt { get; set; }

        [StringLength(2000)]
        public string? DirectorNote { get; set; }

        public bool RequiresHRProcessing { get; set; } = true;

        public int? HRAssignedAccountId { get; set; }
        [ForeignKey(nameof(HRAssignedAccountId))] public virtual Account? HRAssignedAccount { get; set; }

        [StringLength(2000)]
        public string? HRNote { get; set; }

        public DateTime? HRProcessedAt { get; set; }

        public DateTime? EmployeeNotifiedAt { get; set; }
        public DateTime? ResponseDeadlineAt { get; set; }

        [StringLength(500)]
        public string? EvidenceFilePath { get; set; }

        [StringLength(2000)]
        public string? ManagerNote { get; set; }

        [StringLength(2000)]
        public string? EmployeeExplanation { get; set; }

        public DateTime? EmployeeExplanationAt { get; set; }

        public bool LockAccountOnExecution { get; set; }
        public DateTime? AccountLockedAt { get; set; }

        public bool RequiresFinalSettlement { get; set; }
        public int? RelatedFinalSettlementId { get; set; }
        [ForeignKey(nameof(RelatedFinalSettlementId))] public virtual FinalSettlement? RelatedFinalSettlement { get; set; }

        public int? SourcePenaltyRecordId { get; set; }
        [ForeignKey(nameof(SourcePenaltyRecordId))] public virtual PenaltyRecord? SourcePenaltyRecord { get; set; }

        public int? SourcePerformanceReviewId { get; set; }
        [ForeignKey(nameof(SourcePerformanceReviewId))] public virtual PerformanceReview? SourcePerformanceReview { get; set; }

        [StringLength(100)]
        public string? DecisionNumber { get; set; }

        [StringLength(500)]
        public string? DecisionFilePath { get; set; }

        public DateTime? DecisionIssuedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        [StringLength(2000)]
        public string? RejectedReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<PersonnelChangeApproval> Approvals { get; set; } = new List<PersonnelChangeApproval>();
        public virtual ICollection<PersonnelChangeHistory> Histories { get; set; } = new List<PersonnelChangeHistory>();
        public virtual ICollection<PersonnelChangeContractLink> ContractLinks { get; set; } = new List<PersonnelChangeContractLink>();
        public virtual ICollection<PersonnelChangeRiskSnapshot> RiskSnapshots { get; set; } = new List<PersonnelChangeRiskSnapshot>();
    }
}
