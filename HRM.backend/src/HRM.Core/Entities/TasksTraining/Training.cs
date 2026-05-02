using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;
 using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;

namespace HRM.backend.src.HRM.Core.Entities.TasksTraining
{
    [Table("trainings")]
    public class Training
    {
        [Key] public int Id { get; set; }

        public int? EmployeeId { get; set; }
         [ForeignKey("EmployeeId")] public virtual Employee? Employee { get; set; }

        [StringLength(255)] public string? CourseName { get; set; }

        public TrainingStatus Status { get; set; } = TrainingStatus.InProgress;
        public DateTime? Deadline { get; set; }
    }
}