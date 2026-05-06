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

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            var result = await _identityUseCase.ProcessOAuthLoginAsync(dto.Code);
            return Ok(result);
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

        [HttpPost("verify-mfa")]
        public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaDto dto)
        {
            var result = await _identityUseCase.VerifyMfaLoginAsync(dto.OtpCode, dto.TempToken);

            if (result.Status == "FAILED")
                return BadRequest(new { Message = "Mã xác thực không chính xác hoặc đã hết hạn." });

            return Ok(result);
        }

        [HttpPost("verify-recovery-code")]
        public async Task<IActionResult> VerifyRecoveryCode([FromBody] VerifyRecoveryCodeDto dto)
        {
            var result = await _identityUseCase.VerifyRecoveryCodeLoginAsync(dto.RecoveryCode, dto.TempToken);

            if (result.Status == "FAILED")
                return BadRequest(new { Message = "Mã khôi phục không chính xác hoặc đã được sử dụng." });

            return Ok(result);
        }

        [Authorize]
        [HttpPost("mfa/setup")]
        public async Task<IActionResult> InitiateMfaSetup()
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
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
        public async Task<IActionResult> ConfirmMfaSetup([FromBody] ConfirmMfaSetupDto dto)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            try
            {
                var recoveryCodes = await _identityUseCase.ConfirmMfaSetupAsync(userId, dto.OtpCode);
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
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
        {
            try
            {
                var result = await _identityUseCase.RefreshTokenAsync(dto.AccessToken, dto.RefreshToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Trả về 401 để Frontend biết đường đá văng user ra trang Đăng nhập
                return Unauthorized(new { Message = ex.Message });
            }
        }
    }
}
