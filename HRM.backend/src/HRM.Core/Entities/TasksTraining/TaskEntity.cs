using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.TasksTraining
{
    [Table("tasks")]
    public class WorkTask
    {
        [Key] public int Id { get; set; }

        [StringLength(255)] public required string Title { get; set; }
        [StringLength(2000)] public string? Description { get; set; }
        public TaskType TaskType { get; set; } = TaskType.Project;

        public int? DeptId { get; set; }
        [ForeignKey("DeptId")] public virtual Department? Department { get; set; }

        public int? AssignedTo { get; set; }
        [ForeignKey("AssignedTo")] public virtual Employee? Assignee { get; set; }

        public int? CreatedByAccountId { get; set; }
        [ForeignKey("CreatedByAccountId")] public virtual Account? CreatedByAccount { get; set; }

        public int? TrainingId { get; set; }
        [ForeignKey("TrainingId")] public virtual Training? Training { get; set; }

        public int ProgressPercent { get; set; }

        [Column(TypeName = "decimal(15,2)")] public decimal? BonusAmount { get; set; } = 0;
        [Column(TypeName = "decimal(15,2)")] public decimal? ActualBonus { get; set; } = 0;

        public Enums.TaskStatus Status { get; set; } = Enums.TaskStatus.Assigned;

        [StringLength(500)] public string? EvidencePath { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime? ReviewDeadline { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<TaskProgress> Progresses { get; set; } = new List<TaskProgress>();
        public virtual ICollection<TaskFeedback> Feedbacks { get; set; } = new List<TaskFeedback>();
    }
}
