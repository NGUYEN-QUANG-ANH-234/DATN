using HRM.backend.src.HRM.Core.Entities.System;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HRM.backend.src.HRM.Application.Interfaces.Services;

namespace HRM.backend.src.HRM.Application.Services.System
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        // Bổ sung hàm Helper để lấy và kiểm tra SecretKey an toàn
        private string GetSecretKey()
        {
            var secretKey = _config["JwtSettings:SecretKey"];

            // Nếu key trong appsettings.json trống hoặc quá ngắn ("abcde"), tự động lấy từ .env
            if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 16)
            {
                secretKey = Environment.GetEnvironmentVariable("SECRET_KEY");
            }

            // Chặn đứng lỗi StackOverflow / Crash hệ thống bằng Exception rõ ràng
            if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 16)
            {
                throw new InvalidOperationException("LỖI NGHIÊM TRỌNG: SecretKey bị thiếu hoặc quá ngắn (< 16 ký tự). JWT HMAC-SHA256 yêu cầu độ dài an toàn!");
            }

            return secretKey;
        }

        public string GenerateAccessToken(Account account)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, account.Email),
                new Claim("RoleId", account.RoleId.ToString())
            };

            return CreateToken(claims, _config.GetValue<int>("JwtSettings:AccessTokenExpirationMinutes", 60));
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSecretKey())), // Dùng hàm GetSecretKey
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Token không hợp lệ.");
            }

            return principal;
        }

        public string GeneratePreAuthToken(Account account)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
                new Claim("IsPreAuth", "true")
            };

            return CreateToken(claims, 5);
        }

        public int ExtractUserIdFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            var userIdString = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;
            return int.Parse(userIdString);
        }

        private string CreateToken(IEnumerable<Claim> claims, int expirationMinutes)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSecretKey())); // Dùng hàm GetSecretKey
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}