namespace HRM.backend.src.HRM.Application.Interfaces.System.Services
{
    public interface IMfaService
    {
        bool VerifyOTP(string otpCode, string secretKey);
    }
}
