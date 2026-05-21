using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;
 using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;

namespace HRM.backend.src.HRM.Core.Entities.RequestHandover
{
    [Table("handover_requests")]
    public class HandoverRequest
    {
        [Key] public int Id { get; set; }

        public int? RequestId { get; set; }
        [ForeignKey("RequestId")] public virtual Request? Request { get; set; }

        public int? SenderId { get; set; }
         [ForeignKey("SenderId")] public virtual Employee? Sender { get; set; }

        public int? ReceiverId { get; set; }
         [ForeignKey("ReceiverId")] public virtual Employee? Receiver { get; set; }

        public HandoverStatus Status { get; set; } = HandoverStatus.Pending_Verification;
        public DateTime? DeadlineAt { get; set; }

        // Navigation Property: 1 Phiếu bàn giao có nhiều hạng mục con
        public virtual ICollection<HandoverItem> HandoverItems { get; set; } = new List<HandoverItem>();
    }
}