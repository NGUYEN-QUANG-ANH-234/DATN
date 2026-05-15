using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace HRM.backend.src.HRM.API.Extensions;

public static class JwtExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var jwtSettings = config.GetSection("JwtSettings");

        // 1. Đọc SecretKey chuẩn từ IConfiguration (đã tự động map từ biến JwtSettings__SecretKey trong .env)
        var secretKeyValue = config["JwtSettings:SecretKey"];

        // 2. Chặn đứng lỗi NullReference nếu cấu hình thiếu
        if (string.IsNullOrEmpty(secretKeyValue))
        {
            throw new InvalidOperationException("LỖI CẤU HÌNH: Không tìm thấy JwtSettings:SecretKey. Hãy kiểm tra lại file .env!");
        }

        var secretKey = Encoding.UTF8.GetBytes(secretKeyValue);

        services.AddAuthentication(opt =>
        {
            opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                ClockSkew = TimeSpan.Zero // Loại bỏ độ trễ 5 phút mặc định của JWT
            };
        });

        return services;
    }
}