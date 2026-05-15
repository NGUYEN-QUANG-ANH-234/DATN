using System.Security.Claims;
using HRM.backend.src.HRM.Core.Entities.System;

namespace HRM.backend.src.HRM.Application.Interfaces.Services
{
    public interface IJwtService
    {
        // Tạo Access Token từ thông tin User (ID, Email, Role...)
        string GenerateAccessToken(Account account, CancellationToken ct = default);

        // Tạo một chuỗi ngẫu nhiên làm Refresh Token
        string GenerateRefreshToken(CancellationToken ct = default);

        // Giải mã một Token đã hết hạn để lấy lại thông tin User (Claims)
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token, CancellationToken ct = default);

        // MỚI: Dùng cho MFA
        string GeneratePreAuthToken(Account account, CancellationToken ct = default);
        int ExtractUserIdFromToken(string token, CancellationToken ct = default);
    }
}