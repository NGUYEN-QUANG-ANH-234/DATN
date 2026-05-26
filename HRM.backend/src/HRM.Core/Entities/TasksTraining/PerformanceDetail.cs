using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.TasksTraining
{
    [Table("performance_details")]
    public class PerformanceDetail
    {
        [Key] public int Id { get; set; }

        public int ReviewId { get; set; }
        [ForeignKey("ReviewId")] public virtual PerformanceReview? Review { get; set; }

        [StringLength(80)] public required string KpiCode { get; set; }
        [StringLength(255)] public required string KpiName { get; set; }
        [StringLength(1000)] public string? Description { get; set; }

        public int WeightPercent { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? TargetValue { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? ActualValue { get; set; }

        [StringLength(50)] public string? Unit { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal EmployeeSelfPercent { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal AchievedPercent { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal ManagerScore { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal FinalPoint { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal SystemPenaltyPoint { get; set; }

        [StringLength(1000)] public string? SystemPenaltyReason { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal ManualPenaltyPoint { get; set; }

        [StringLength(1000)] public string? ManualPenaltyReason { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal PenaltyPoint { get; set; }

        [StringLength(500)] public string? PenaltyReason { get; set; }

        [StringLength(1000)] public string? EmployeeComment { get; set; }
        [StringLength(1000)] public string? ManagerComment { get; set; }
        [StringLength(500)] public string? EvidencePath { get; set; }
    }
}
