using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.PersonnelChanges
{
    [Table("personnel_change_approvals")]
    public class PersonnelChangeApproval
    {
        [Key] public int Id { get; set; }

        public int RequestId { get; set; }
        [ForeignKey(nameof(RequestId))] public virtual PersonnelChangeRequest Request { get; set; } = null!;

        [StringLength(100)]
        public required string StepName { get; set; }

        [StringLength(50)]
        public required string ApproverRole { get; set; }

        public int? ApproverAccountId { get; set; }
        [ForeignKey(nameof(ApproverAccountId))] public virtual Account? ApproverAccount { get; set; }

        public PersonnelChangeApprovalDecision Decision { get; set; } = PersonnelChangeApprovalDecision.Pending;

        [StringLength(2000)]
        public string? Note { get; set; }

        public DateTime? DecidedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
