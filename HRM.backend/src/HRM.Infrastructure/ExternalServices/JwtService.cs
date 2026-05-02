using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Application.Interfaces.Services;

namespace HRM.backend.src.HRM.Infrastructure.ExternalServices
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateAccessToken(Account user)
        {
            var jwtSettings = _config.GetSection("JwtSettings");

            // 1. Kiểm tra SECRET_KEY từ biến môi trường
            var secretKey = Environment.GetEnvironmentVariable("SECRET_KEY")
                            ?? throw new InvalidOperationException("SECRET_KEY is missing in environment variables.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            // 2. Khởi tạo danh sách Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                // Claim "role" dùng cho PermissionHandler của bạn
                new Claim("role", user.RoleId.ToString()),
                // Claim chuẩn .NET dùng cho [Authorize(Roles = "...")]
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Guest")
            };

            // 3. Duyệt danh sách quyền (Chỉ chạy nếu AuthService đã dùng .Include)
            if (user.Role?.RolePermissions != null)
            {
                foreach (var rp in user.Role.RolePermissions)
                {
                    // Kiểm tra null từng tầng để tránh lỗi sập Server (NullReference)
                    if (rp.Permission != null && !string.IsNullOrEmpty(rp.Permission.PermissionCode))
                    {
                        claims.Add(new Claim("permission", rp.Permission.PermissionCode));
                    }
                }
            }

            // 4. Lấy thời gian hết hạn an toàn
            var expiryVar = Environment.GetEnvironmentVariable("ExpiryInMinutes");
            if (!double.TryParse(expiryVar, out double expiryMinutes))
            {
                expiryMinutes = 60; // Mặc định 1 tiếng nếu .env lỗi
            }

            // 5. Tạo Token
            var tokenDescriptor = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(expiryMinutes),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var secretKey = Environment.GetEnvironmentVariable("SECRET_KEY")
                            ?? throw new InvalidOperationException("SECRET_KEY missing");

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        // ==========================================
        // CÁC HÀM BỔ SUNG CHO MFA
        // ==========================================

        public string GeneratePreAuthToken(Account user)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var secretKey = Environment.GetEnvironmentVariable("SECRET_KEY")
                            ?? throw new InvalidOperationException("SECRET_KEY missing");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("IsPreAuth", "true") // Đánh dấu là token tạm thời, không có quyền truy cập resource
            };

            // Token tạm thời chờ nhập OTP chỉ có hiệu lực trong thời gian ngắn (ví dụ 5 phút)
            var tokenDescriptor = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(5),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }

        public int ExtractUserIdFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            // Đọc Claim NameIdentifier mà ta đã gán lúc tạo Token (cả AccessToken và PreAuthToken)
            var userIdString = jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;
            return int.Parse(userIdString);
        }
    }
}