using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.Services;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System.HRM.backend.src.HRM.Infrastructure.Repositories.Interfaces.System;

namespace HRM.backend.src.HRM.Application.Interfaces.System.UseCases
{
    public class IdentityUseCase : IIdentityUseCase
    {
        private readonly IAccountRepository _accountRepo;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMfaService _mfaService;
        private readonly IGoogleOAuthService _googleOAuthService;
        private readonly IJwtService _jwtService;

        public IdentityUseCase(
            IAccountRepository accountRepo,
            IAuditLogRepository auditLogRepo,
            IUnitOfWork unitOfWork,
            IMfaService mfaService,
            IGoogleOAuthService googleOAuthService,
            IJwtService jwtService)
        {
            _accountRepo = accountRepo;
            _auditLogRepo = auditLogRepo;
            _unitOfWork = unitOfWork;
            _mfaService = mfaService;
            _googleOAuthService = googleOAuthService;
            _jwtService = jwtService;
        }

        public async Task<AuthResponseDto> ProcessOAuthLoginAsync(string authCode)
        {
            // 1. Xác thực Google & Lấy Profile
            var googleProfile = await _googleOAuthService.ExchangeCodeForProfileAsync(authCode);

            // 2. Upsert User qua Repository
            var user = await _accountRepo.FindOrUpsertUserAsync(googleProfile.Email, googleProfile.Name, googleProfile.Id);

            // 3. Xử lý MFA
            if (!user.IsMfaEnabled)
            {
                var accessToken = _jwtService.GenerateAccessToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                await _auditLogRepo.LogAuditActionAsync("LOGIN_SUCCESS_NO_MFA", user.Id, "accounts");
                await _unitOfWork.CommitAsync();

                return new AuthResponseDto
                {
                    Status = "SUCCESS",
                    Token = accessToken,
                    RefreshToken = refreshToken,
                    Expiration = DateTime.UtcNow.AddHours(1)
                };
            }
            else
            {
                var tempToken = _jwtService.GeneratePreAuthToken(user);

                await _auditLogRepo.LogAuditActionAsync("LOGIN_MFA_CHALLENGE", user.Id, "accounts");
                await _unitOfWork.CommitAsync();

                return new AuthResponseDto { Status = "MFA_REQUIRED", Token = tempToken };
            }
        }

        public async Task<AuthResponseDto> VerifyMfaLoginAsync(string otpCode, string tempToken)
        {
            var userId = _jwtService.ExtractUserIdFromToken(tempToken);
            var user = await _accountRepo.GetByIdAsync(userId);

            if (user == null || string.IsNullOrEmpty(user.MfaSecretKey))
                throw new Exception("Dữ liệu xác thực không hợp lệ.");

            bool isOtpValid = _mfaService.VerifyOTP(otpCode, user.MfaSecretKey);

            if (!isOtpValid)
            {
                await _auditLogRepo.LogAuditActionAsync("LOGIN_MFA_FAILED", user.Id, "accounts");
                await _unitOfWork.CommitAsync();
                return new AuthResponseDto { Status = "FAILED" };
            }

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _auditLogRepo.LogAuditActionAsync("LOGIN_MFA_SUCCESS", user.Id, "accounts");
            await _unitOfWork.CommitAsync();

            return new AuthResponseDto
            {
                Status = "SUCCESS",
                Token = accessToken,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddHours(1)
            };
        }
    }
}
