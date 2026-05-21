using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile; // Khai báo nếu Employee ở thư mục khác

namespace HRM.backend.src.HRM.Core.Entities.TasksTraining
{
    [Table("tasks")]
    public class WorkTask
    {
        [Key] public int Id { get; set; }

        [StringLength(255)] public required string Title { get; set; }
        public TaskType TaskType { get; set; } = TaskType.Project;

        public int? AssignedTo { get; set; }
        [ForeignKey("AssignedTo")] public virtual Employee? Assignee { get; set; }

        [Column(TypeName = "decimal(15,2)")] public decimal? BonusAmount { get; set; } = 0;
        [Column(TypeName = "decimal(15,2)")] public decimal? ActualBonus { get; set; } = 0;

        public Enums.TaskStatus Status { get; set; } = Enums.TaskStatus.Todo;

        [StringLength(255)] public string? EvidencePath { get; set; }
        public DateTime? Deadline { get; set; }

        // Navigation Properties (Quan hệ 1-N)
        public virtual ICollection<TaskFeedback> Feedbacks { get; set; } = new List<TaskFeedback>();
    }
}