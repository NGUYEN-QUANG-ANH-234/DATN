using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.PersonnelChanges
{
    [Table("personnel_change_histories")]
    public class PersonnelChangeHistory
    {
        [Key] public int Id { get; set; }

        public int RequestId { get; set; }
        [ForeignKey(nameof(RequestId))] public virtual PersonnelChangeRequest Request { get; set; } = null!;

        [StringLength(100)]
        public required string Action { get; set; }

        public PersonnelChangeStatus? OldStatus { get; set; }
        public PersonnelChangeStatus? NewStatus { get; set; }

        public int? ActorAccountId { get; set; }
        [ForeignKey(nameof(ActorAccountId))] public virtual Account? ActorAccount { get; set; }

        [StringLength(2000)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
