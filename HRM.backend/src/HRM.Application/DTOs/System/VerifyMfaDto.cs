namespace HRM.backend.src.HRM.Application.DTOs.System
{
    public class VerifyMfaDto
    {
        public required string OtpCode { get; set; }
        public required string TempToken { get; set; }
    }
}