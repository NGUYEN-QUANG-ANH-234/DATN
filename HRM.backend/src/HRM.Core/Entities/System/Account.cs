using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.System
{
    [Table("accounts")]
    public class Account
    {
        [Key] public int Id { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public required string Email { get; set; }

        public string? PasswordHash { get; set; }
        public string? OAuthId { get; set; }

        public int RoleId { get; set; }
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!; // Navigation property
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public AccountStatus Status { get; set; } = AccountStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        // Bổ sung các trường MFA (Multi-Factor Authentication) từ DB
        public bool IsMfaEnabled { get; set; } = false;
        public string? MfaSecretKey { get; set; }
    }
}