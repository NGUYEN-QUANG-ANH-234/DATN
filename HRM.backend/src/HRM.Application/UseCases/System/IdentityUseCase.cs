using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.Services;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Core.Entities.System;
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
        private readonly IAppCache _appCache;
        private readonly IMfaRecoveryCodeRepository _mfaRecoveryCodeRepo;

        public IdentityUseCase(
            IAccountRepository accountRepo,
            IAuditLogRepository auditLogRepo,
            IUnitOfWork unitOfWork,
            IMfaService mfaService,
            IGoogleOAuthService googleOAuthService,
            IJwtService jwtService,
            IAppCache appCache,
            IMfaRecoveryCodeRepository mfaRecoveryCodeRepo)
        {
            _accountRepo = accountRepo;
            _auditLogRepo = auditLogRepo;
            _unitOfWork = unitOfWork;
            _mfaService = mfaService;
            _googleOAuthService = googleOAuthService;
            _jwtService = jwtService;
            _appCache = appCache;
            _mfaRecoveryCodeRepo = mfaRecoveryCodeRepo;
        }

        public async Task<AuthResponseDto> ProcessOAuthLoginAsync(string authCode)
        {
            Console.WriteLine(">>> [BƯỚC 1]: Bắt đầu chạy vào hàm xử lý đăng nhập.");

            var googleProfile = await _googleOAuthService.ExchangeCodeForProfileAsync(authCode);
            Console.WriteLine($">>> [BƯỚC 2]: Đã gọi xong Google API. Email lấy được: {googleProfile.Email}");

            var user = await _accountRepo.FindOrUpsertUserAsync(googleProfile.Email, googleProfile.Name, googleProfile.Id);

            // Nếu là user mới (Id = 0), phải lưu ngay xuống DB để MySQL cấp phát ID thật
            if (user.Id == 0)
            {
                await _unitOfWork.CommitAsync();
            }

            Console.WriteLine($">>> [BƯỚC 3]: Đã tìm/tạo xong User trong Database. ID là: {user.Id}");

            if (!user.IsMfaEnabled)
            {
                Console.WriteLine(">>> [BƯỚC 4]: MFA đang tắt. Bắt đầu tạo JWT Token...");
                var accessToken = _jwtService.GenerateAccessToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                Console.WriteLine(">>> [BƯỚC 5]: Tạo JWT xong. Bắt đầu ghi log Audit...");
                await _auditLogRepo.LogAuditActionAsync("LOGIN_SUCCESS_NO_MFA", user.Id, "accounts");

                Console.WriteLine(">>> [BƯỚC 6]: Bắt đầu lưu cập nhật Token & Log xuống Database (CommitAsync)...");
                await _unitOfWork.CommitAsync();

                Console.WriteLine(">>> [BƯỚC 7]: LƯU THÀNH CÔNG! Đang đóng gói dữ liệu trả về Frontend.");
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
                // Tương tự cho nhánh MFA
                Console.WriteLine(">>> [BƯỚC 4B]: Hệ thống yêu cầu MFA...");
                var tempToken = _jwtService.GeneratePreAuthToken(user);
                await _auditLogRepo.LogAuditActionAsync("LOGIN_MFA_CHALLENGE", user.Id, "accounts");
                await _unitOfWork.CommitAsync();
                return new AuthResponseDto { Status = "MFA_REQUIRED", Token = tempToken, IsMfaEnabled = user.IsMfaEnabled };
            }
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
                await _auditLogRepo.LogAuditActionAsync("LOGOUT_SUCCESS", userId, "accounts");

                // Lưu thay đổi
                await _unitOfWork.CommitAsync();
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
                Expiration = DateTime.UtcNow.AddHours(1),
                IsMfaEnabled = user.IsMfaEnabled,
            };
        }

        public async Task<AuthResponseDto> VerifyRecoveryCodeLoginAsync(string recoveryCode, string tempToken)
        {
            // 1. Lấy userId từ token tạm
            var userId = _jwtService.ExtractUserIdFromToken(tempToken);
            var user = await _accountRepo.GetByIdAsync(userId);

            if (user == null)
                throw new Exception("Dữ liệu xác thực không hợp lệ.");

            // 2. Tìm mã khôi phục hợp lệ (chưa dùng và khớp mã Hash)
            var validCode = await _mfaRecoveryCodeRepo.GetUnusedCodeAsync(userId, recoveryCode);

            if (validCode == null)
            {
                await _auditLogRepo.LogAuditActionAsync("LOGIN_RECOVERY_FAILED", user.Id, "accounts");
                await _unitOfWork.CommitAsync();
                return new AuthResponseDto { Status = "FAILED" };
            }

            // 3. XÓA mã khôi phục thay vì chỉ đánh dấu
            _mfaRecoveryCodeRepo.Remove(validCode); // Thay bằng hàm xóa tương ứng trong Repo của bạn (VD: Remove)

            // VÔ HIỆU HÓA MFA (Đưa về trạng thái chưa thiết lập)
            user.MfaSecretKey = null;

            // Cập nhật trạng thái thiết lập MFA tại bảng accounts với trường IsMfaEnabled 
            user.IsMfaEnabled = false;

            // Xóa nốt các mã khôi phục còn lại của user này cho an toàn tuyệt đối
            await _mfaRecoveryCodeRepo.DeleteAllUserCodesAsync(user.Id);            

            // 4. Tạo token chính thức cho người dùng
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            // 5. Lưu Log và Commit DB
            await _auditLogRepo.LogAuditActionAsync("LOGIN_RECOVERY_SUCCESS", user.Id, "accounts");
            await _unitOfWork.CommitAsync();

            return new AuthResponseDto
            {
                Status = "SUCCESS",
                Token = accessToken,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddHours(1),
                IsMfaEnabled = user.IsMfaEnabled,
            };
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

        public async Task<List<string>> ConfirmMfaSetupAsync(int userId, string otpCode)
        {
            var cacheKey = $"mfa_setup_{userId}";
            var cachedSecret = await _appCache.GetAsync<string>(cacheKey);

            if (string.IsNullOrEmpty(cachedSecret))
                throw new Exception("Tiến trình thiết lập đã hết hạn. Vui lòng thao tác lại.");

            if (!_mfaService.VerifyOTP(otpCode, cachedSecret))
                throw new Exception("Mã OTP không chính xác.");

            // Cập nhật Database
            var user = await _accountRepo.GetByIdAsync(userId);
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

            await _mfaRecoveryCodeRepo.AddBulkAsync(recoveryEntities);
            await _unitOfWork.CommitAsync();
            await _appCache.RemoveAsync(cacheKey);

            return plainRecoveryCodes;
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string expiredToken, string refreshToken)
        {
            // 1. Lấy UserId từ token cũ (Lưu ý: Hàm này trong JwtService phải cho phép đọc token đã hết hạn)
            var userId = _jwtService.ExtractUserIdFromToken(expiredToken);

            var user = await _accountRepo.GetByIdAsync(userId);

            // 2. Kiểm tra tính hợp lệ của Refresh Token
            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                throw new Exception("Refresh token không hợp lệ hoặc đã hết hạn.");
            }

            // 3. Tạo cặp Token mới
            var newAccessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            // 4. Cập nhật Database
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _auditLogRepo.LogAuditActionAsync("TOKEN_REFRESH_SUCCESS", user.Id, "accounts");
            await _unitOfWork.CommitAsync();

            return new AuthResponseDto
            {
                Status = "SUCCESS",
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                Expiration = DateTime.UtcNow.AddHours(1),
                IsMfaEnabled = user.IsMfaEnabled
            };
        }
    }
}
