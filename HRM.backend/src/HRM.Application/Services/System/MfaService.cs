using HRM.backend.src.HRM.Application.Interfaces.System.Services;

namespace HRM.backend.src.HRM.Application.Services.System
{
    public class MfaService : IMfaService
    {
        public bool VerifyOTP(string otpCode, string secretKey)
        {
            // Triển khai thực tế thường sử dụng thư viện Otp.NET
            // var totp = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(secretKey));
            // return totp.VerifyTotp(otpCode, out long timeWindowUsed, OtpNet.VerificationWindow.RfcSpecifiedNetworkDelay);

            throw new NotImplementedException("Triển khai Google Authenticator TOTP tại đây.");
        }
    }
}
