using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRM.backend.src.HRM.API.Controllers.System
{
    [ApiController]
    [Route("api/v1/system/sla")]
    [Authorize(Roles = "Admin, HR")] // Chỉ Admin mới được cấu hình SLA
    public class SlaConfigController : ControllerBase
    {
        private readonly ISlaManagementUseCase _useCase;

        public SlaConfigController(ISlaManagementUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet]
        [RequirePermission("SLA_CONFIG_VIEW", GroupName = SystemModules.Config, Description = "Xem cấu hình KPI thời gian xử lý (SLA) hệ thống")]
        public async Task<IActionResult> GetSLAConfigs(CancellationToken ct)
        {
            var data = await _useCase.GetSLAConfigsAsync(ct);
            return Ok(new { success = true, data });
        }

        [HttpPut]
        [RequirePermission("SLA_CONFIG_UPDATE", GroupName = SystemModules.Config, Description = "Cập nhật thông số KPI thời gian xử lý (SLA) hệ thống")]
        public async Task<IActionResult> UpdateSLAConfig([FromBody] SlaDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu đầu vào không hợp lệ." });

            try
            {
                // Trích xuất Admin ID an toàn
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim, out int adminId))
                {
                    return Unauthorized(new { success = false, message = "Không xác định được danh tính Admin." });
                }

                var isSuccess = await _useCase.UpdateSLAParameterAsync(dto, adminId, ct);

                if (isSuccess)
                {
                    return Ok(new
                    {
                        success = true,
                        message = $"Cập nhật SLA cho phân hệ {dto.ModuleCode.ToUpper()} thành công."
                    });
                }

                return StatusCode(500, new { success = false, message = "Lỗi không xác định khi lưu cấu hình." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống nội bộ." });
            }
        }
    }
}
