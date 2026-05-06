namespace HRM.backend.src.HRM.Application.DTOs.System
{
    public class AuthResponseDto
    {
        public required string Status { get; set; } // SUCCESS, MFA_REQUIRED, FAILED
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? Expiration { get; set; }
        public Boolean? IsMfaEnabled { get; set; } = false;
    }
}
