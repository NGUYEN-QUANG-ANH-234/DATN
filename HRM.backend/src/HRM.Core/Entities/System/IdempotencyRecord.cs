using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.System
{
    [Table("idempotency_records")]
    public class IdempotencyRecord
    {
        [Key] public int Id { get; set; }

        [StringLength(100)]
        public required string Scope { get; set; }

        [StringLength(128)]
        public required string IdempotencyKey { get; set; }

        [StringLength(80)]
        public required string ResourceType { get; set; }

        public int ResourceId { get; set; }
        public int? AccountId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
    }
}
