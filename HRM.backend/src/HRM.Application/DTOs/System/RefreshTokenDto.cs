namespace HRM.backend.src.HRM.Application.DTOs.System
{
    public class RefreshTokenDto
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }
}
