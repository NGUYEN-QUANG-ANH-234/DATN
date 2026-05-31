using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRM.backend.src.HRM.API.Controllers.System
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IIdentityUseCase _identityUseCase;

        public AuthController(IIdentityUseCase identityUseCase)
        {
            //Console.WriteLine("DI ĐÃ KHỞI TẠO THÀNH CÔNG IDENTITY USE CASE!");
            _identityUseCase = identityUseCase;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _identityUseCase.LoginWithPasswordAsync(dto, ct);

                if (result.Status == "SUCCESS" && !string.IsNullOrEmpty(result.RefreshToken))
                {
                    SetRefreshTokenCookie(result.RefreshToken);
                    result.RefreshToken = null;
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _identityUseCase.ProcessOAuthLoginAsync(dto.Code, ct);

                if (result.Status == "SUCCESS")
                {
                    SetRefreshTokenCookie(result.RefreshToken!);
                    result.RefreshToken = null;
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized(new { Message = "Không thể xác định danh tính người dùng." });
            }

            try
            {
                await _identityUseCase.LogoutAsync(userId);
                return Ok(new { Message = "Đăng xuất thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Đã xảy ra lỗi khi đăng xuất: " + ex.Message });
            }
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
        {
            try
            {
                var userId = User.GetAccountIdOrThrow();
                await _identityUseCase.ChangePasswordAsync(userId, dto, ct);
                return Ok(new { Message = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("verify-mfa")]
        public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _identityUseCase.VerifyMfaLoginAsync(dto.OtpCode, dto.TempToken, ct);

                if (result.Status == "FAILED")
                    return BadRequest(new { Message = "Mã xác thực không chính xác hoặc đã hết hạn." });

                if (result.Status == "SUCCESS" && !string.IsNullOrEmpty(result.RefreshToken))
                {
                    SetRefreshTokenCookie(result.RefreshToken);
                    result.RefreshToken = null; // Giấu khỏi body JSON
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("verify-recovery-code")]
        public async Task<IActionResult> VerifyRecoveryCode([FromBody] VerifyRecoveryCodeDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _identityUseCase.VerifyRecoveryCodeLoginAsync(dto.RecoveryCode, dto.TempToken, ct);

                if (result.Status == "FAILED")
                    return BadRequest(new { Message = "Mã khôi phục không chính xác hoặc đã được sử dụng." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("mfa/setup")]
        public async Task<IActionResult> InitiateMfaSetup()
        {
            try
            {
                int userId = User.GetAccountIdOrThrow();
                string email = User.FindFirst(ClaimTypes.Email)!.Value;
                var result = await _identityUseCase.InitiateMfaSetupAsync(userId, email);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // CHÍNH LÀ DÒNG NÀY: Trả về lỗi 400 để Frontend đọc được
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("mfa/confirm")]
        public async Task<IActionResult> ConfirmMfaSetup([FromBody] ConfirmMfaSetupDto dto, CancellationToken ct)
        {
            int userId = User.GetAccountIdOrThrow();

            try
            {
                var recoveryCodes = await _identityUseCase.ConfirmMfaSetupAsync(userId, dto.OtpCode, ct);
                return Ok(new
                {
                    Message = "Thiết lập MFA thành công.",
                    RecoveryCodes = recoveryCodes
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto, CancellationToken ct)
        {
            try
            {
                // 1. Đọc Refresh Token từ Cookie do trình duyệt gửi lên
                var refreshTokenCookie = Request.Cookies["refreshToken"];

                if (string.IsNullOrEmpty(refreshTokenCookie))
                    return Unauthorized(new { Message = "Không tìm thấy phiên đăng nhập hợp lệ." });

                // 2. Gọi UseCase (Truyền token từ cookie vào)
                var result = await _identityUseCase.RefreshTokenAsync(dto.AccessToken, refreshTokenCookie, ct);

                // 3. Ghi đè Cookie mới (Token Rotation)
                SetRefreshTokenCookie(result.RefreshToken!);

                // 4. Giấu RefreshToken khỏi body JSON trả về Frontend
                result.RefreshToken = null;

                return Ok(result);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        private void SetRefreshTokenCookie(string refreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Chặn JavaScript truy cập
                Secure = true,   // Bắt buộc chạy trên HTTPS
                SameSite = SameSiteMode.Strict, // Chống tấn công CSRF
                Expires = DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }
    }
}
