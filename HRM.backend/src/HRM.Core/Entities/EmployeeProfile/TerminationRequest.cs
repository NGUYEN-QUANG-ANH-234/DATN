using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.EmployeeProfile
{
    [Table("termination_requests")]
    public class TerminationRequest
    {
        [Key] public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")] public virtual Employee Employee { get; set; } = null!;

        public TerminationType TerminationType { get; set; } = TerminationType.Resignation;
        public TerminationRequestStatus Status { get; set; } = TerminationRequestStatus.Draft;
        public TerminationLegalStatus LegalStatus { get; set; } = TerminationLegalStatus.PendingHrReview;

        [StringLength(2000)] public required string Reason { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public DateTime? NoticeDate { get; set; }
        public DateTime ExpectedLastWorkingDate { get; set; }
        public DateTime? ApprovedLastWorkingDate { get; set; }
        public DateTime? ActualLastWorkingDate { get; set; }

        public int RequiredNoticeDays { get; set; }
        public int ActualNoticeDays { get; set; }
        public int MissingNoticeDays { get; set; }

        public int? ApprovedByAccountId { get; set; }
        [ForeignKey("ApprovedByAccountId")] public virtual Account? ApprovedByAccount { get; set; }
        public DateTime? ApprovedAt { get; set; }

        [StringLength(1000)] public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual FinalSettlement? FinalSettlement { get; set; }
    }
}
