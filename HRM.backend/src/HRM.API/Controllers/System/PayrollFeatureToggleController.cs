using System.Security.Claims;
using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.System
{
    [ApiController]
    [Route("api/v1/system/payroll-feature-toggles")]
    [Authorize(Roles = "Admin,HR")]
    public class PayrollFeatureToggleController : ControllerBase
    {
        private readonly IPayrollFeatureToggleUseCase _useCase;

        public PayrollFeatureToggleController(IPayrollFeatureToggleUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet]
        [RequirePermission("PAYROLL_POLICY_VIEW", GroupName = SystemModules.Config, Description = "Xem cấu hình nhánh tính lương")]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var data = await _useCase.GetAsync(ct);
            return Ok(new { success = true, data });
        }

        [HttpPut]
        [RequirePermission("PAYROLL_POLICY_UPDATE", GroupName = SystemModules.Config, Description = "Cập nhật cấu hình nhánh tính lương")]
        public async Task<IActionResult> Update([FromBody] PayrollFeatureToggleDto dto, CancellationToken ct)
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(claim, out var actorId))
                return Unauthorized(new { success = false, message = "Không xác định được người dùng." });

            var data = await _useCase.UpdateAsync(dto, actorId, ct);
            return Ok(new { success = true, data, message = "Đã lưu cấu hình tính lương." });
        }
    }
}
