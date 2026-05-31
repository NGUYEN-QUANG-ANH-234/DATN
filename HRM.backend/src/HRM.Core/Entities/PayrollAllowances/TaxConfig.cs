using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public bool IsActive { get; set; } = true;
        [StringLength(1000)] public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
