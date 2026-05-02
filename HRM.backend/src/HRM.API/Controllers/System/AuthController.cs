using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.System
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IIdentityUseCase _identityUseCase;

        public AuthController(IIdentityUseCase identityUseCase)
        {
            _identityUseCase = identityUseCase;
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            var result = await _identityUseCase.ProcessOAuthLoginAsync(dto.Code);
            return Ok(result);
        }

        [HttpPost("verify-mfa")]
        public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaDto dto)
        {
            var result = await _identityUseCase.VerifyMfaLoginAsync(dto.OtpCode, dto.TempToken);

            if (result.Status == "FAILED")
                return BadRequest(new { Message = "Mã xác thực không chính xác hoặc đã hết hạn." });

            return Ok(result);
        }
    }
}
