using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;

namespace HRM.backend.src.HRM.Core.Entities.TasksTraining
{
    [Table("task_progresses")]
    public class TaskProgress
    {
        [Key] public int Id { get; set; }

        public int TaskId { get; set; }
        [ForeignKey("TaskId")] public virtual WorkTask? Task { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")] public virtual Employee? Employee { get; set; }

        public int ProgressPercent { get; set; }
        [StringLength(1000)] public string? Note { get; set; }
        [StringLength(500)] public string? EvidencePath { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<TaskFeedback> Feedbacks { get; set; } = new List<TaskFeedback>();
    }
}
