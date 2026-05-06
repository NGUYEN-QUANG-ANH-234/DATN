using System.ComponentModel.DataAnnotations;

namespace HRM.backend.src.HRM.Core.Entities.System
{
    public class MfaRecoveryCode
    {
        [Key] public int Id { get; set; }
        public int AccountId { get; set; }
        public required string CodeHash { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
