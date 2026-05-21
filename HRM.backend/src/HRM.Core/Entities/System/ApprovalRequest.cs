using HRM.backend.src.HRM.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.System
{
    [Table("approval_requests")]
    public class ApprovalRequest
    {
        [Key] public int Id { get; set; }
        public required string ModuleCode { get; set; } // VD: "LEAVE_REQUEST", "CONTRACT"
        public int ReferenceId { get; set; } // ID của bảng nghiệp vụ
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
        public int CurrentLevel { get; set; } = 1; // Đang ở cấp duyệt số mấy?
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<ApprovalStep> Steps { get; set; } = new List<ApprovalStep>();
    }
}
