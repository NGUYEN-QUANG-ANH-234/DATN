using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using OtpNet;

namespace HRM.backend.src.HRM.Application.Services.System
{
    public class MfaService : IMfaService
    {
        public string GenerateMfaSecret()
        {
            var key = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(key);
        }

        public string GenerateQrCodeUri(string email, string secretKey, string issuer)
        {
            // Trả về URI chuẩn để Frontend có thể dùng thư viện qrcode tạo ảnh
            return $"otpauth://totp/{issuer}:{email}?secret={secretKey}&issuer={issuer}";
        }

        public bool VerifyOTP(string otpCode, string secretKey)
        {
            // Đã thay thế NotImplementedException[cite: 4] bằng code thật
            var totp = new Totp(Base32Encoding.ToBytes(secretKey));
            return totp.VerifyTotp(otpCode, out long timeWindowUsed, VerificationWindow.RfcSpecifiedNetworkDelay);
        }
    }
}
