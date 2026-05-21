using HRM.backend.src.HRM.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.System
{
    [Table("sla_tracking_tasks")]
    public class SlaTrackingTask
    {
        [Key] public int Id { get; set; }
        public SlaModuleType ModuleType { get; set; }
        public int ReferenceId { get; set; } // ID của ProfileRequest, OnboardingRequest...

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime Deadline { get; set; }
        public DateTime? ResolvedAt { get; set; }

        public SlaTaskStatus Status { get; set; } = SlaTaskStatus.Pending;
    }
}
