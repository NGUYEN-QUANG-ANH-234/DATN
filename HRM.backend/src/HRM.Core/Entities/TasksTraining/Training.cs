using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.TasksTraining
{
    [Table("trainings")]
    public class Training
    {
        [Key] public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")] public virtual Employee? Employee { get; set; }

        public int? DeptId { get; set; }
        [ForeignKey("DeptId")] public virtual Department? Department { get; set; }

        public int? ManagerId { get; set; }
        [ForeignKey("ManagerId")] public virtual Employee? Manager { get; set; }

        [StringLength(255)] public string? CourseName { get; set; }
        [StringLength(80)] public string? TrainingType { get; set; }

        public TrainingStatus Status { get; set; } = TrainingStatus.InProgress;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? EvaluationDeadline { get; set; }
        public DateTime? EvaluatedAt { get; set; }

        // Kept for backward compatibility with existing seed/data.
        public DateTime? Deadline { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? FinalScore { get; set; }

        [StringLength(1000)]
        public string? ManagerEvaluation { get; set; }

        public bool IsPassed { get; set; }

        public int? PromotionRequestId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<WorkTask> Tasks { get; set; } = new List<WorkTask>();
    }
}
