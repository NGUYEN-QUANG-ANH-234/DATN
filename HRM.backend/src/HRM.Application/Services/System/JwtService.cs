using HRM.backend.src.HRM.Core.Entities.System;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HRM.backend.src.HRM.Application.Interfaces.Services;

namespace HRM.backend.src.HRM.Application.Services.System
{
    public class JwtService : IJwtService // Nhớ sửa interface thành IJwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateAccessToken(Account account)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, account.Email),
                new Claim("RoleId", account.RoleId.ToString())
            };

            // Token chính thức sống 1 giờ (hoặc lấy từ config)
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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!)),
                ValidateLifetime = false // Bỏ qua check hạn để lấy thông tin từ token cũ
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;

            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Token không hợp lệ.");

            return principal;
        }

        public string GeneratePreAuthToken(Account account)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
                new Claim("IsPreAuth", "true") // Đánh dấu đây là token tạm
            };

            // Token tạm thời chờ nhập MFA chỉ sống trong 5 phút
            return CreateToken(claims, 5);
        }

        public int ExtractUserIdFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            var userIdString = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;
            return int.Parse(userIdString);
        }

        // --- Hàm Helper dùng chung ---
        private string CreateToken(IEnumerable<Claim> claims, int expirationMinutes)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!));
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