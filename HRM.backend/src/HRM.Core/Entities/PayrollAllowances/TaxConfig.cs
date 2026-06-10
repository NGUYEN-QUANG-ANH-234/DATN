using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.PayrollAllowances
{
    [Table("tax_configs")]
    public class TaxConfig
    {
        [Key] public int Id { get; set; }

        [StringLength(80)] public required string Code { get; set; }
        [StringLength(200)] public required string Name { get; set; }

        [Column(TypeName = "decimal(15,2)")] public decimal PersonalDeduction { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal DependentDeduction { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal FlatTaxThreshold { get; set; }
        [Column(TypeName = "decimal(7,4)")] public decimal FlatTaxRate { get; set; }
        [Column(TypeName = "decimal(7,4)")] public decimal NonResidentTaxRate { get; set; }

        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public int Version { get; set; } = 1;
        [StringLength(80)] public string? VersionCode { get; set; }
        public PolicyVersionStatus Status { get; set; } = PolicyVersionStatus.Active;
        [StringLength(200)] public string? SourceRef { get; set; }
        public int? SupersedesVersionId { get; set; }
        public int? CreatedByAccountId { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public bool LockedAfterUsed { get; set; }
        public bool IsActive { get; set; } = true;
        [StringLength(1000)] public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
