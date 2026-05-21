using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;

namespace HRM.backend.src.HRM.Core.Entities.TasksTraining
{
    [Table("performance_reviews")]
    public class PerformanceReview
    {
        [Key] public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")] public virtual Employee? Employee { get; set; }

        [StringLength(10)] public required string Period { get; set; } // Ví dụ: "05/2026"

        [Column(TypeName = "decimal(5,2)")]
        public decimal TotalScore { get; set; } = 0;

        [StringLength(5)] public string? FinalRating { get; set; } // Xếp loại (A, B, C)

        public ReviewStatus Status { get; set; } = ReviewStatus.Draft;

        // Navigation
        public virtual ICollection<PerformanceDetail> Details { get; set; } = new List<PerformanceDetail>();
    }
}