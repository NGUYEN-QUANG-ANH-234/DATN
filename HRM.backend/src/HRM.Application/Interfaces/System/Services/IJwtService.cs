using System.Security.Claims;
using HRM.backend.src.HRM.Core.Entities.System;

namespace HRM.backend.src.HRM.Application.Interfaces.Services
{
    public interface IJwtService
    {
        // Tạo Access Token từ thông tin User (ID, Email, Role...)
        string GenerateAccessToken(Account account);

        // Tạo một chuỗi ngẫu nhiên làm Refresh Token
        string GenerateRefreshToken();

        // Giải mã một Token đã hết hạn để lấy lại thông tin User (Claims)
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);

        // MỚI: Dùng cho MFA
        string GeneratePreAuthToken(Account account);
        int ExtractUserIdFromToken(string token);
    }
}