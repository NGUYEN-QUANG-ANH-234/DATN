using HRM.backend.src.HRM.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.System
{
    [Table("approval_steps")]
    public class ApprovalStep
    {
        [Key] public int Id { get; set; }

        public int ApprovalRequestId { get; set; }

        public int Level { get; set; } // Cấp duyệt 1, 2, 3...

        // Chuyển thành int? để nếu duyệt theo Role thì không bắt buộc ghi chết ID Account từ đầu
        public int? ApproverAccountId { get; set; }

        // Bổ sung cột lưu tên Role được quyền duyệt bước này (Ví dụ: "HR", "Director")
        [StringLength(50)]
        public string? ApproverRoleName { get; set; }

        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

        [StringLength(500)]
        public string? Note { get; set; }

        public DateTime? ProcessedAt { get; set; }
    }
}