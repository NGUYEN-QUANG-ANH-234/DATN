using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.System
{
    [Table("audit_logs")]
    public class AuditLog
    {
        [Key] public int Id { get; set; }

        public int? AccountId { get; set; }
        [ForeignKey("AccountId")]
        public virtual Account? Account { get; set; }

        [StringLength(100)] public string? ActionType { get; set; } // Insert, Update, Delete
        [StringLength(50)] public string? TableName { get; set; } // Tên bảng (Module)

        // Đổi thành JSON string để lưu toàn bộ object
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }

        // THÊM MỚI: Danh sách các cột bị thay đổi (VD: ["Salary", "PositionId"])
        public string? AffectedColumns { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}