using Google.Apis.Auth;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.Interfaces.Services;
using System.Text.Json.Serialization;

namespace HRM.backend.src.HRM.Application.Services.System
{
    public class GoogleOAuthService : IGoogleOAuthService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public GoogleOAuthService(IConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }

        // Helper để lấy cấu hình linh hoạt từ appsettings hoặc .env (Rất tốt cho Docker/CI-CD)
        private string GetConfigValue(string configKey, string envKey)
        {
            var value = _config[configKey];
            if (string.IsNullOrEmpty(value))
            {
                value = Environment.GetEnvironmentVariable(envKey);
            }
            return value ?? string.Empty;
        }

        public async Task<GoogleProfile> ExchangeCodeForProfileAsync(string authCode)
        {
            // Đọc thẳng qua IConfiguration (Đã tự map từ .env nhờ GoogleSettings__ClientId)
            var clientId = _config["GoogleSettings:ClientId"];
            var clientSecret = _config["GoogleSettings:ClientSecret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new Exception("LỖI CẤU HÌNH: Thiếu ClientId hoặc ClientSecret của Google.");
            }

            // --- BƯỚC 1: TRAO ĐỔI AUTH CODE LẤY TOKENS (OAuth2 Flow) ---
            var tokenRequest = new Dictionary<string, string>
            {
                { "code", authCode },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "redirect_uri", "postmessage" }, // "postmessage" là bắt buộc khi dùng thư viện @react-oauth/google (auth-code flow)
                { "grant_type", "authorization_code" }
            };

            var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(tokenRequest));

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new UnauthorizedAccessException($"Lỗi kết nối tới Google OAuth: {error}");
            }

            // Google trả về cục JSON chứa cả access_token và id_token
            var tokenData = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>();
            if (string.IsNullOrWhiteSpace(tokenData?.IdToken))
            {
                throw new UnauthorizedAccessException("Google OAuth không trả về id_token hợp lệ.");
            }

            // --- BƯỚC 2: XÁC THỰC VÀ GIẢI MÃ ID TOKEN (OIDC Flow) ---
            // Thay vì gọi API /userinfo, ta giải mã trực tiếp id_token nội bộ để tiết kiệm 1 request mạng.
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                // BẮT BUỘC: Xác nhận Token này được phát hành cho ClientId của hệ thống mình (chống token giả mạo từ app khác)
                Audience = new[] { clientId },

                // Tránh lỗi chênh lệch múi giờ giữa máy chủ Google và Server nội bộ
                IssuedAtClockTolerance = TimeSpan.FromMinutes(5)
            };

            // Hàm ValidateAsync sẽ ngầm thực hiện: 
            // 1. Tải Public Key từ Google để kiểm tra chữ ký điện tử.
            // 2. Kiểm tra 'exp' (hết hạn), 'iss' (issuer) và 'aud' (audience).
            // 3. Ném lỗi InvalidJwtException nếu Token bị can thiệp.
            var payload = await GoogleJsonWebSignature.ValidateAsync(tokenData.IdToken, settings);

            // Bổ sung lớp bảo mật: Đảm bảo email này đã được Google xác minh thật sự (chống email ảo)
            if (!payload.EmailVerified)
            {
                throw new UnauthorizedAccessException("Email chưa được Google xác minh.");
            }

            // --- BƯỚC 3: TRẢ VỀ THÔNG TIN ---
            return new GoogleProfile
            {
                Id = payload.Subject,
                Email = payload.Email,
                FullName = payload.Name,      // Lấy Tên
                PictureUrl = payload.Picture  // THÊM: Lấy URL Ảnh đại diện từ Google
            };
        }
    }

    internal class GoogleTokenResponse
    {
        [JsonPropertyName("id_token")]
        public string IdToken { get; set; } = null!;

        // (Tùy chọn) Có thể khai báo thêm access_token nếu sau này hệ thống cần gọi API khác của Google (vd: Google Drive, Calendar)
        // [JsonPropertyName("access_token")]
        // public string AccessToken { get; set; }
    }
}
