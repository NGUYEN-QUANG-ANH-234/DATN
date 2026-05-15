using HRM.backend.src.HRM.Application.DTOs;

namespace HRM.backend.src.HRM.Application.Interfaces.Services
{
    public class GoogleProfile
    {
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!; // Sửa Name -> FullName
        public string Id { get; set; } = null!;
        public string? PictureUrl { get; set; } // THÊM: Ảnh đại diện
    }

    public interface IGoogleOAuthService
    {
        Task<GoogleProfile> ExchangeCodeForProfileAsync(string authCode);
    }
}
