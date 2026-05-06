namespace HRM.backend.src.HRM.Application.DTOs.System
{
    public class VerifyMfaDto
    {
        public required string OtpCode { get; set; }
        public required string TempToken { get; set; }
    }

    public class MfaSetupResponseDto
    {
        // Link dùng để hiển thị mã QR trên Frontend (Frontend dùng thư viện qrcode.react để render từ chuỗi này)
        public required string QrCodeUri { get; set; }

        // Chuỗi khóa bí mật (dùng để copy thủ công nếu người dùng không quét được QR)
        public required string SecretKey { get; set; }
    }
    public class ConfirmMfaSetupDto
    {
        public required string OtpCode { get; set; }
    }
}