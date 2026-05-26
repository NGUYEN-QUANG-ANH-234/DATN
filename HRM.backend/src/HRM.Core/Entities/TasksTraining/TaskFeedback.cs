using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.TasksTraining
{
    [Table("task_feedbacks")]
    public class TaskFeedback
    {
        [Key] public int Id { get; set; }

        public int? TaskId { get; set; }
        [ForeignKey("TaskId")] public virtual WorkTask? Task { get; set; }

        public int? ProgressId { get; set; }
        [ForeignKey("ProgressId")] public virtual TaskProgress? Progress { get; set; }

        public int? ReviewerId { get; set; }
        [ForeignKey("ReviewerId")] public virtual Employee? Reviewer { get; set; }

        public TaskFeedbackType FeedbackType { get; set; } = TaskFeedbackType.Comment;

        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
