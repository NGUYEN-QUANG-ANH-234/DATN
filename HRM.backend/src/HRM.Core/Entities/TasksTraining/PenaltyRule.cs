using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.TasksTraining
{
    [Table("penalty_rules")]
    public class PenaltyRule
    {
        [Key] public int Id { get; set; }

        public PenaltySourceType SourceType { get; set; }

        [StringLength(80)] public required string RuleCode { get; set; }
        [StringLength(255)] public required string RuleName { get; set; }
        [StringLength(1000)] public string? Description { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? ThresholdValue { get; set; }

        [StringLength(50)] public string? ThresholdUnit { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal PenaltyPoint { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
