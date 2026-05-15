using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.Services;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System.HRM.backend.src.HRM.Infrastructure.Repositories.Interfaces.System;
using static Google.Apis.Requests.BatchRequest;

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
        private readonly IAppCache _appCache;
        private readonly IMfaRecoveryCodeRepository _mfaRecoveryCodeRepo;
        private readonly ILockService _lockService;

        public IdentityUseCase(
            IAccountRepository accountRepo,
            IAuditLogRepository auditLogRepo,
            IUnitOfWork unitOfWork,
            IMfaService mfaService,
            IGoogleOAuthService googleOAuthService,
            IJwtService jwtService,
            IAppCache appCache,
            IMfaRecoveryCodeRepository mfaRecoveryCodeRepo,
            ILockService lockService
            )
        {
            _accountRepo = accountRepo;
            _auditLogRepo = auditLogRepo;
            _unitOfWork = unitOfWork;
            _mfaService = mfaService;
            _googleOAuthService = googleOAuthService;
            _jwtService = jwtService;
            _appCache = appCache;
            _mfaRecoveryCodeRepo = mfaRecoveryCodeRepo;
            _lockService = lockService;
        }

        public async Task<AuthResponseDto> LoginWithPasswordAsync(LoginDto dto, CancellationToken ct)
        {
            // Khóa luồng theo Email để chống brute-force hoặc race condition khi thao tác với Token
            return await _lockService.GetWithLockAsync($"login_pwd_{dto.Email}", async (innerCt) =>
            {
                // 1. Tìm kiếm người dùng bằng Email
                // LƯU Ý: Đảm bảo bạn đã có hàm GetByEmailAsync(email) trong AccountRepository
                var user = await _accountRepo.GetByEmailAsync(dto.Email, innerCt);

                // 2. Kiểm tra tài khoản tồn tại và có mật khẩu (tránh lỗi với tài khoản chỉ login bằng Google)
                if (user == null || string.IsNullOrEmpty(user.PasswordHash))
                {
                    throw new UnauthorizedAccessException("Tài khoản hoặc mật khẩu không chính xác.");
                }

                // 3. Kiểm tra trạng thái tài khoản
                if (user.Status != AccountStatus.Active)
                {
                    throw new UnauthorizedAccessException("Tài khoản của bạn đã bị khóa.");
                }

                // 4. Đối chiếu mật khẩu bằng BCrypt
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
                if (!isPasswordValid)
                {
                    await _auditLogRepo.LogSystemEventAsync("LOGIN_FAILED_PWD", user.Id, "accounts", "Sai mật khẩu");
                    await _unitOfWork.CommitAsync(innerCt);
                    throw new UnauthorizedAccessException("Tài khoản hoặc mật khẩu không chính xác.");
                }

                // 5. Xử lý cấp Token (Logic hoàn toàn giống ProcessOAuthLoginAsync)
                if (!user.IsMfaEnabled)
                {
                    // Trạng thái chưa bật MFA: Cấp thẳng Access Token
                    var accessToken = _jwtService.GenerateAccessToken(user, innerCt);
                    var refreshToken = _jwtService.GenerateRefreshToken(innerCt);

                    user.RefreshToken = refreshToken;
                    user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                    await _auditLogRepo.LogSystemEventAsync("LOGIN_SUCCESS_PWD", user.Id, "accounts", "Đăng nhập thành công");
                    await _unitOfWork.CommitAsync(innerCt);

                    return new AuthResponseDto
                    {
                        Status = "SUCCESS",
                        Token = accessToken,
                        RefreshToken = refreshToken,
                        Expiration = DateTime.UtcNow.AddHours(1),
                        IsMfaEnabled = user.IsMfaEnabled
                    };
                }
                else
                {
                    // Trạng thái đã bật MFA: Cấp Token Tạm và yêu cầu OTP
                    var tempToken = _jwtService.GeneratePreAuthToken(user, innerCt);

                    await _auditLogRepo.LogSystemEventAsync("LOGIN_MFA_CHALLENGE", user.Id, "accounts", "Yêu cầu xác thực OTP");
                    await _unitOfWork.CommitAsync(innerCt);

                    return new AuthResponseDto
                    {
                        Status = "MFA_REQUIRED",
                        Token = tempToken,
                        IsMfaEnabled = user.IsMfaEnabled
                    };
                }
            }, TimeSpan.FromSeconds(10), ct);
        }

        public async Task<AuthResponseDto> ProcessOAuthLoginAsync(string authCode, CancellationToken ct)
        {
            var googleProfile = await _googleOAuthService.ExchangeCodeForProfileAsync(authCode);

            //Khóa theo Email để đảm bảo tiến trình Upsert không bị đụng độ
            return await _lockService.GetWithLockAsync($"login_{googleProfile.Email}", async (innerCt) =>
            {
                var user = await _accountRepo.FindOrUpsertUserAsync(
                    googleProfile.Email,
                    googleProfile.FullName,    // Sửa: Gọi đúng FullName
                    googleProfile.PictureUrl,  // Thêm: Truyền URL ảnh từ Google
                    googleProfile.Id,
                    innerCt
                );

                if (user.Id == 0)
                {
                    await _unitOfWork.CommitAsync(innerCt);
                }

                if (!user.IsMfaEnabled)
                {
                    var accessToken = _jwtService.GenerateAccessToken(user, innerCt);
                    var refreshToken = _jwtService.GenerateRefreshToken(innerCt);

                    user.RefreshToken = refreshToken;
                    user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                    await _auditLogRepo.LogSystemEventAsync("LOGIN_SUCCESS_NO_MFA", user.Id, "accounts", "Đăng nhập bằng Google thành công");
                    await _unitOfWork.CommitAsync(innerCt);

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
                    var tempToken = _jwtService.GeneratePreAuthToken(user, innerCt);
                    await _auditLogRepo.LogSystemEventAsync("LOGIN_MFA_CHALLENGE", user.Id, "accounts", "Yêu cầu xác thực OTP (Google)");
                    await _unitOfWork.CommitAsync(innerCt);

                    return new AuthResponseDto { Status = "MFA_REQUIRED", Token = tempToken, IsMfaEnabled = user.IsMfaEnabled };
                }
            });
        }

        public async Task LogoutAsync(int userId)
        {
            var user = await _accountRepo.GetByIdAsync(userId);

            if (user != null)
            {
                // Thu hồi (xóa) Refresh Token để không thể cấp lại Access Token mới
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = DateTime.MinValue;

                // Ghi log Audit
                await _auditLogRepo.LogSystemEventAsync("LOGOUT_SUCCESS", userId, "accounts", "Người dùng đăng xuất");

                // Lưu thay đổi
                await _unitOfWork.CommitAsync();
            }
        }

        public async Task<AuthResponseDto> VerifyMfaLoginAsync(string otpCode, string tempToken, CancellationToken ct)
        {
            var userId = _jwtService.ExtractUserIdFromToken(tempToken);

            return await _lockService.GetWithLockAsync($"login_{userId}", async (innerCt) =>
            {
                var user = await _accountRepo.GetByIdAsync(userId, innerCt);

                if (user == null || string.IsNullOrEmpty(user.MfaSecretKey))
                    throw new Exception("Dữ liệu xác thực không hợp lệ.");

                bool isOtpValid = _mfaService.VerifyOTP(otpCode, user.MfaSecretKey);

                if (!isOtpValid)
                {
                    await _auditLogRepo.LogSystemEventAsync("LOGIN_MFA_FAILED", user.Id, "accounts", "Nhập sai mã OTP");
                    await _unitOfWork.CommitAsync(innerCt);
                    return new AuthResponseDto { Status = "FAILED" };
                }

                var accessToken = _jwtService.GenerateAccessToken(user, innerCt);
                var refreshToken = _jwtService.GenerateRefreshToken(innerCt);

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                await _auditLogRepo.LogSystemEventAsync("LOGIN_MFA_SUCCESS", user.Id, "accounts", "Xác thực OTP thành công");
                await _unitOfWork.CommitAsync(innerCt);

                return new AuthResponseDto
                {
                    Status = "SUCCESS",
                    Token = accessToken,
                    RefreshToken = refreshToken,
                    Expiration = DateTime.UtcNow.AddHours(1),
                    IsMfaEnabled = user.IsMfaEnabled,
                };
            }, TimeSpan.FromSeconds(10), ct);
        }

        public async Task<AuthResponseDto> VerifyRecoveryCodeLoginAsync(string recoveryCode, string tempToken, CancellationToken ct)
        {
            var userId = _jwtService.ExtractUserIdFromToken(tempToken);

            //// Khóa theo UserID để 1 user chỉ được xử lý 1 request Recovery tại một thời điểm
            return await _lockService.GetWithLockAsync($"recovery_{userId}", async (innerCt) =>
            {
                var user = await _accountRepo.GetByIdAsync(userId, innerCt);
                if (user == null) throw new Exception("Dữ liệu xác thực không hợp lệ.");

                var validCode = await _mfaRecoveryCodeRepo.GetUnusedCodeAsync(userId, recoveryCode);
                if (validCode == null)
                {
                    await _auditLogRepo.LogSystemEventAsync("LOGIN_RECOVERY_FAILED", user.Id, "accounts", "Nhập sai mã khôi phục");
                    await _unitOfWork.CommitAsync(innerCt);
                    return new AuthResponseDto { Status = "FAILED" };
                }

                _mfaRecoveryCodeRepo.Remove(validCode);
                user.MfaSecretKey = null;
                user.IsMfaEnabled = false;
                await _mfaRecoveryCodeRepo.DeleteAllUserCodesAsync(user.Id, innerCt);

                var accessToken = _jwtService.GenerateAccessToken(user, innerCt);
                var refreshToken = _jwtService.GenerateRefreshToken(innerCt);
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                await _auditLogRepo.LogSystemEventAsync("LOGIN_RECOVERY_SUCCESS", user.Id, "accounts", "Khôi phục tài khoản thành công");
                await _unitOfWork.CommitAsync(innerCt);

                return new AuthResponseDto
                {
                    Status = "SUCCESS",
                    Token = accessToken,
                    RefreshToken = refreshToken,
                    Expiration = DateTime.UtcNow.AddHours(1),
                    IsMfaEnabled = user.IsMfaEnabled
                };
            });
        }

        public async Task<MfaSetupResponseDto> InitiateMfaSetupAsync(int userId, string email)
        {
            var user = await _accountRepo.GetByIdAsync(userId);
            if (user == null || user.IsMfaEnabled)
                throw new Exception("Người dùng không hợp lệ hoặc MFA đã được bật.");

            var secretKey = _mfaService.GenerateMfaSecret();
            var qrCodeUri = _mfaService.GenerateQrCodeUri(email, secretKey, "HRM HICAS");

            // Lưu cache 10 phút
            var cacheKey = $"mfa_setup_{userId}";
            await _appCache.SetAsync(cacheKey, secretKey, TimeSpan.FromMinutes(10));

            return new MfaSetupResponseDto { QrCodeUri = qrCodeUri, SecretKey = secretKey };
        }

        public async Task<List<string>> ConfirmMfaSetupAsync(int userId, string otpCode, CancellationToken ct)
        {
            var cacheKey = $"mfa_setup_{userId}";
            var cachedSecret = await _appCache.GetAsync<string>(cacheKey);

            if (string.IsNullOrEmpty(cachedSecret))
                throw new Exception("Tiến trình thiết lập đã hết hạn. Vui lòng thao tác lại.");

            if (!_mfaService.VerifyOTP(otpCode, cachedSecret))
                throw new Exception("Mã OTP không chính xác.");

            return await _lockService.GetWithLockAsync($"confirm_mfa_{userId}", async (innerCt) =>
            {
                // Cập nhật Database
                var user = await _accountRepo.GetByIdAsync(userId, innerCt);
                user!.MfaSecretKey = cachedSecret;
                user.IsMfaEnabled = true;

                // Sinh mã khôi phục
                var plainRecoveryCodes = new List<string>();
                var recoveryEntities = new List<MfaRecoveryCode>();

                for (int i = 0; i < 8; i++)
                {
                    var code = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                    var formattedCode = $"{code.Substring(0, 4)}-{code.Substring(4, 4)}";

                    plainRecoveryCodes.Add(formattedCode);
                    recoveryEntities.Add(new MfaRecoveryCode
                    {
                        AccountId = userId,
                        CodeHash = BCrypt.Net.BCrypt.HashPassword(formattedCode)
                    });
                }

                await _mfaRecoveryCodeRepo.AddBulkAsync(recoveryEntities, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
                await _appCache.RemoveAsync(cacheKey, innerCt);

                return plainRecoveryCodes;
            }, TimeSpan.FromSeconds(10), ct);            
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string expiredToken, string refreshToken, CancellationToken ct)
        {
            var userId = _jwtService.ExtractUserIdFromToken(expiredToken);

            // Khóa luồng cấp lại Token của User này
            return await _lockService.GetWithLockAsync($"refresh_{userId}", async (innerCt) =>
            {
                var user = await _accountRepo.GetByIdAsync(userId, innerCt);

                if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                    throw new Exception("Refresh token không hợp lệ hoặc đã hết hạn.");

                var newAccessToken = _jwtService.GenerateAccessToken(user, innerCt);
                var newRefreshToken = _jwtService.GenerateRefreshToken(innerCt);

                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                await _auditLogRepo.LogSystemEventAsync("TOKEN_REFRESH_SUCCESS", user.Id, "accounts", "Cấp lại Access Token mới");
                await _unitOfWork.CommitAsync(innerCt);

                return new AuthResponseDto
                {
                    Status = "SUCCESS",
                    Token = newAccessToken,
                    RefreshToken = newRefreshToken,
                    Expiration = DateTime.UtcNow.AddHours(1),
                    IsMfaEnabled = user.IsMfaEnabled
                };
            });
        }

    }
}
