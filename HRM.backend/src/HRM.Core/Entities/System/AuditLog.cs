using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.System
{
    [Table("audit_logs")]
    public class AuditLog
    {
        [Key] public int Id { get; set; }

        public int? AccountId { get; set; } // Nên để Nullable phòng trường hợp log do Hệ thống tự chạy
        [ForeignKey("AccountId")]
        public virtual Account? Account { get; set; }

        [StringLength(100)] public string? ActionType { get; set; }
        [StringLength(50)] public string? TableName { get; set; }

        // Đổi từ Description thành OldValue / NewValue theo schema
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}