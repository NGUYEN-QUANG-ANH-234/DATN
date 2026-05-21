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
    [Route("api/v1/system/attendance-config")]
    [Authorize(Roles = "Admin")]
    public class AttendanceConfigController : ControllerBase
    {
        private readonly IAttendanceConfigUseCase _useCase;

        public AttendanceConfigController(IAttendanceConfigUseCase useCase)
        {
            _useCase = useCase;
        }

        [RequirePermission("ATTENDANCE_CONFIG_VIEW", GroupName = SystemModules.TimekeepingLeave, Description = "Xem tham số cấu hình chấm công")]
        [HttpGet]
        public async Task<IActionResult> GetConfig(CancellationToken ct)
        {
            var data = await _useCase.GetConfigAsync(ct);
            return Ok(new { success = true, data });
        }

        [RequirePermission("ATTENDANCE_CONFIG_UPDATE", GroupName = SystemModules.TimekeepingLeave, Description = "Cập nhật tham số cấu hình chấm công")]
        [HttpPut]
        public async Task<IActionResult> UpdateConfig([FromBody] AttendanceConfigDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu đầu vào không hợp lệ." });

            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim, out int adminId))
                    return Unauthorized(new { success = false, message = "Không xác định được danh tính Admin." });

                var isSuccess = await _useCase.UpdateConfigAsync(dto, adminId, ct);

                if (isSuccess)
                    return Ok(new { success = true, message = "Cập nhật tham số chấm công thành công." });

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
