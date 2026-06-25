using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;
using HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases;
using HRM.backend.src.HRM.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.EmployeeProfile
{
    [ApiController]
    [Route("api/v1/onboarding-requests")]
    public class OnboardingController : ControllerBase
    {
        private readonly IOnboardingUseCase _useCase;
        private readonly string[] _permittedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };

        public OnboardingController(IOnboardingUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpPost("resolve")]
        [AllowAnonymous]
        public async Task<IActionResult> ResolveCandidate([FromBody] ResolveOnboardingCandidateDto dto, CancellationToken ct)
        {
            try
            {
                var data = await _useCase.ResolveCandidateAsync(dto, ct);
                return Ok(new { Success = true, Data = data });
            }
            catch (Exception ex)
            {
                return UnprocessableEntity(new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [AllowAnonymous]
        public async Task<IActionResult> SubmitProfile([FromForm] SubmitOnboardingDto dto, CancellationToken ct)
        {
            if (dto.IdentityFrontFile == null || dto.IdentityBackFile == null)
                return BadRequest(new { Success = false, Message = "Thiếu giấy tờ tùy thân bắt buộc." });

            try
            {
                await _useCase.SubmitProfileAsync(dto, ct);
                return Created("", new { Success = true, Message = "Hồ sơ đã gửi, chờ HR xác minh giấy tờ." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("pending")]
        [Authorize]
        [RequirePermission("ONBOARDING_REVIEW", GroupName = SystemModules.ProfileContract, Description = "Xem hồ sơ nhân viên mới chờ duyệt")]
        public async Task<IActionResult> GetPending(CancellationToken ct)
        {
            var data = await _useCase.GetPendingRequestsAsync(ct);
            return Ok(new { Success = true, Data = data });
        }

        [HttpPatch("{id}/hr-review")]
        [Authorize]
        [RequirePermission("ONBOARDING_REVIEW", GroupName = SystemModules.ProfileContract, Description = "Duyệt hồ sơ nhân viên mới")]
        public async Task<IActionResult> ReviewByHR(int id, [FromBody] ReviewOnboardingDto dto, CancellationToken ct)
        {
            try
            {
                await _useCase.ReviewByHrAsync(id, dto, ct);
                return Ok(new { Success = true, Message = dto.IsApproved ? "Đã kích hoạt nhân viên." : "Đã từ chối hồ sơ." });
            }
            catch (Exception ex)
            {
                return UnprocessableEntity(new { Success = false, Message = ex.Message });
            }
        }
    }
}
