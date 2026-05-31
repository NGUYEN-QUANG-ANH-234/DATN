using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.Organization
{
    [Table("position_job_level_policies")]
    public class PositionJobLevelPolicy
    {
        [Key] public int Id { get; set; }

        public int PositionId { get; set; }
        [ForeignKey(nameof(PositionId))] public virtual Position Position { get; set; } = null!;

        public int JobLevelId { get; set; }
        [ForeignKey(nameof(JobLevelId))] public virtual JobLevel JobLevel { get; set; } = null!;

        [Column(TypeName = "decimal(15,2)")] public decimal? BaseSalaryMin { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal? BaseSalaryMax { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal PositionAllowance { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal ResponsibilityAllowance { get; set; }

        public bool IsInsuranceBased { get; set; }
        public bool IsTaxable { get; set; } = true;

        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public int Version { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
