using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.Services;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;

namespace HRM.backend.src.HRM.Application.Interfaces.System.UseCases
{
    public interface IIdentityUseCase
    {
        Task<AuthResponseDto> LoginWithPasswordAsync(LoginDto dto, CancellationToken ct);
        Task<AuthResponseDto> ProcessOAuthLoginAsync(string authCode, CancellationToken ct);
        Task LogoutAsync(int userId);
        Task<AuthResponseDto> VerifyMfaLoginAsync(string otpCode, string tempToken, CancellationToken ct);
        Task<AuthResponseDto> VerifyRecoveryCodeLoginAsync(string recoveryCode, string tempToken, CancellationToken ct);
        Task<MfaSetupResponseDto> InitiateMfaSetupAsync(int userId, string email);
        Task<List<string>> ConfirmMfaSetupAsync(int userId, string otpCode, CancellationToken ct);
        Task<AuthResponseDto> RefreshTokenAsync(string expiredToken, string refreshToken, CancellationToken ct);
        Task ChangePasswordAsync(int accountId, ChangePasswordDto dto, CancellationToken ct);
    }
}
