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

        // Helper lấy Secret Key từ User Secrets / appsettings
        private string GetSecretKey() => _config["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("LỖI CẤU HÌNH: JwtSettings:SecretKey bị thiếu.");

        public string GenerateAccessToken(Account user, CancellationToken ct = default)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSecretKey()));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
    
                // THÊM: Trả về Tên để Frontend hiện trên Header
                new Claim(ClaimTypes.Name, user.FullName ?? "Người dùng"),
                new Claim("avatar", user.AvatarUrl ?? ""),
                // SỬA: Trả về Tên Role (chữ) vào biến "role" để Sidebar map đúng menu
                new Claim("role", user.Role?.RoleName ?? "Guest"),
                new Claim("RoleId", user.RoleId.ToString()),
            };

            if (user.Role?.RolePermissions != null)
            {
                foreach (var rp in user.Role.RolePermissions.Where(rp => rp.Permission != null && !string.IsNullOrEmpty(rp.Permission.PermissionCode)))
                {
                    claims.Add(new Claim("permission", rp.Permission.PermissionCode));
                }
            }

            // Đọc thời gian hết hạn từ IConfiguration (mặc định 60 phút nếu không có)
            var expiryMinutes = _config.GetValue<double>("JwtSettings:AccessTokenExpirationMinutes", 60);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(expiryMinutes),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }

        public string GenerateRefreshToken(CancellationToken ct = default)
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token, CancellationToken ct = default)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSecretKey())),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        public string GeneratePreAuthToken(Account user, CancellationToken ct = default)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSecretKey()));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("IsPreAuth", "true")
            };

            var tokenDescriptor = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(5),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }

        public int ExtractUserIdFromToken(string token, CancellationToken ct = default)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var userIdString = jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;
            return int.Parse(userIdString);
        }
    }
}