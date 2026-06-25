using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.PayrollAllowances
{
    [Table("overtime_rate_configs")]
    public class OvertimeRateConfig
    {
        [Key] public int Id { get; set; }

        [StringLength(80)] public required string Code { get; set; }
        public OvertimeType OvertimeType { get; set; }

        [Column(TypeName = "decimal(7,4)")] public decimal BaseMultiplier { get; set; }
        [Column(TypeName = "decimal(7,4)")] public decimal NightAllowanceRate { get; set; }
        [Column(TypeName = "decimal(7,4)")] public decimal NightOvertimeExtraRate { get; set; }

        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public int Version { get; set; } = 1;
        [StringLength(80)] public string? VersionCode { get; set; }
        public PolicyVersionStatus Status { get; set; } = PolicyVersionStatus.Active;
        [StringLength(200)] public string? SourceRef { get; set; }
        public int? SupersedesVersionId { get; set; }
        public int? CreatedByAccountId { get; set; }
        [ForeignKey(nameof(CreatedByAccountId))] public virtual Account? CreatedByAccount { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public bool LockedAfterUsed { get; set; }
        public bool IsActive { get; set; } = true;
        [StringLength(1000)] public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
