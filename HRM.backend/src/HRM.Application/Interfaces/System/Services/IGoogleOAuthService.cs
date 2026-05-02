using HRM.backend.src.HRM.Application.DTOs;

namespace HRM.backend.src.HRM.Application.Interfaces.Services
{
    public class GoogleProfile { public string Email { get; set; } = null!; public string Name { get; set; } = null!; public string Id { get; set; } = null!; }

    public interface IGoogleOAuthService
    {
        Task<GoogleProfile> ExchangeCodeForProfileAsync(string authCode);
    }
}
