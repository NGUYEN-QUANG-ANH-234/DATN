using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.System
{
    [Table("payroll_policies")]
    public class PayrollPolicy
    {
        [Key] public int Id { get; set; }

        public PayrollPolicyType PolicyType { get; set; }

        [StringLength(80)]
        public required string Code { get; set; }

        [StringLength(200)]
        public required string Name { get; set; }

        public PayrollPolicyValueType ValueType { get; set; } = PayrollPolicyValueType.RatePercent;

        public decimal? RatePercent { get; set; }
        public decimal? Amount { get; set; }
        public decimal? FromAmount { get; set; }
        public decimal? ToAmount { get; set; }
        public decimal? QuickDeduction { get; set; }

        public string? FormulaJson { get; set; }

        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }

        public int Version { get; set; } = 1;
        [StringLength(80)]
        public string? VersionCode { get; set; }

        public PolicyVersionStatus Status { get; set; } = PolicyVersionStatus.Active;

        [StringLength(200)]
        public string? SourceRef { get; set; }

        public int? SupersedesVersionId { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public bool LockedAfterUsed { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedByAccountId { get; set; }
        [ForeignKey(nameof(CreatedByAccountId))] public virtual Account? CreatedByAccount { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedByAccountId { get; set; }
        [ForeignKey(nameof(UpdatedByAccountId))] public virtual Account? UpdatedByAccount { get; set; }
    }
}
