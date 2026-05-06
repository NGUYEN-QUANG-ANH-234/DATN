namespace HRM.backend.src.HRM.Application.DTOs.System
{
    public class VerifyRecoveryCodeDto
    {
        public required string RecoveryCode { get; set; }
        public required string TempToken { get; set; }
    }
}
