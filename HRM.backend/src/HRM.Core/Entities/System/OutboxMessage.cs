using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.System
{
    [Table("outbox_messages")]
    public class OutboxMessage
    {
        [Key] public long Id { get; set; }

        [StringLength(40)]
        public string Type { get; set; } = "EMAIL";

        [StringLength(200)]
        public required string Recipient { get; set; }

        [StringLength(300)]
        public required string Subject { get; set; }

        public required string Body { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        public int RetryCount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public string? LastError { get; set; }
    }
}
