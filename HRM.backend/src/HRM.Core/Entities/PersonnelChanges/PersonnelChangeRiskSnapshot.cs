using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.System;

namespace HRM.backend.src.HRM.Core.Entities.PersonnelChanges
{
    [Table("personnel_change_risk_snapshots")]
    public class PersonnelChangeRiskSnapshot
    {
        [Key] public int Id { get; set; }

        public int RequestId { get; set; }
        [ForeignKey(nameof(RequestId))] public virtual PersonnelChangeRequest Request { get; set; } = null!;

        [Column(TypeName = "json")]
        public required string SnapshotJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedByAccountId { get; set; }
        [ForeignKey(nameof(CreatedByAccountId))] public virtual Account? CreatedByAccount { get; set; }
    }
}
