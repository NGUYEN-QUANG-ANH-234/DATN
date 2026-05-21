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

        [StringLength(255)] public required string KpiName { get; set; } // Tên chỉ tiêu

        public int WeightPercent { get; set; } // Trọng số (%)

        [Column(TypeName = "decimal(5,2)")]
        public decimal AchievedPercent { get; set; } // % Hoàn thành

        [Column(TypeName = "decimal(5,2)")]
        public decimal FinalPoint { get; set; } // Điểm quy đổi = WeightPercent * AchievedPercent
    }
}