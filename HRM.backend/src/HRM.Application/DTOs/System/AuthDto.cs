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

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
