using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
 using HRM.backend.src.HRM.Core.Entities.TasksTraining;

namespace HRM.backend.src.HRM.Core.Entities.RequestHandover
{
    [Table("handover_items")]
    public class HandoverItem
    {
        [Key] public int Id { get; set; }

        public int? HandoverRequestId { get; set; }
        [ForeignKey("HandoverRequestId")]
        public virtual HandoverRequest? HandoverRequest { get; set; }

        // Liên kết tới Task nếu hạng mục bàn giao là một công việc trên hệ thống
        public int? TaskId { get; set; }
         [ForeignKey("TaskId")] public virtual WorkTask? Task { get; set; }

        [StringLength(255)] public required string ItemName { get; set; }
        public string? Description { get; set; }

        public bool IsConfirmed { get; set; } = false;
    }
}