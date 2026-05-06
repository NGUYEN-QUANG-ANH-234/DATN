using OtpNet;

namespace HRM.backend.src.HRM.Application.Interfaces.System.Services
{
    public interface IMfaService
    {
        string GenerateMfaSecret();
        public string GenerateQrCodeUri(string email, string secretKey, string issuer);
        bool VerifyOTP(string otpCode, string secretKey);
    }
}
