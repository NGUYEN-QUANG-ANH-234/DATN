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

        public async Task<GoogleProfile> ExchangeCodeForProfileAsync(string authCode)
        {
            // 1. Lấy Config (Nên dùng Environment Variables hoặc User Secrets trên Production)
            var clientId = _config["GoogleSettings:ClientId"] ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
            var clientSecret = _config["GoogleSettings:ClientSecret"] ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");

            // 2. Gửi request đổi Code lấy Tokens
            var tokenRequest = new Dictionary<string, string>
            {
                { "code", authCode },
                { "client_id", clientId! },
                { "client_secret", clientSecret! },
                // Lưu ý cực kỳ quan trọng: Với @react-oauth/google dùng popup, redirect_uri BẮT BUỘC là "postmessage"
                { "redirect_uri", "postmessage" },
                { "grant_type", "authorization_code" }
            };

            var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(tokenRequest));

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new UnauthorizedAccessException($"Lỗi xác thực từ Google: {error}");
            }

            var tokenData = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>();

            // 3. Giải mã và xác thực ID Token
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(tokenData!.IdToken, settings);

            // 4. Trả về Profile
            return new GoogleProfile
            {
                Email = payload.Email,
                Name = payload.Name,
                Id = payload.Subject // Đây là chuỗi ID định danh duy nhất của user từ Google
            };
        }
    }

    // DTO nội bộ để parse kết quả từ API Google
    internal class GoogleTokenResponse
    {
        [JsonPropertyName("id_token")]
        public string IdToken { get; set; } = null!;
    }
}
