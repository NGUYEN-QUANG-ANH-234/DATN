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
        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedByAccountId { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedByAccountId { get; set; }
    }
}
