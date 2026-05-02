using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.TasksTraining
{
    [Table("task_progresses")]
    public class TaskProgress
    {
        [Key] public int Id { get; set; }

        public int? TaskId { get; set; }
        [ForeignKey("TaskId")] public virtual WorkTask? Task { get; set; }

        public int ProgressPercent { get; set; } = 0;
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}