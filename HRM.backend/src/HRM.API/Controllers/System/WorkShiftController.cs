using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Usecases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRM.backend.src.HRM.API.Controllers.System
{
    [ApiController]
    [Route("api/v1/system/work-shifts")]
    [Authorize]
    public class WorkShiftController : ControllerBase
    {
        private readonly IShiftManagementUseCase _useCase;

        public WorkShiftController(IShiftManagementUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpPost]
        [RequirePermission("WORK_SHIFT_UPDATE", GroupName = SystemModules.TimekeepingLeave, Description = "Thiết lập cấu hình ca làm việc và quỹ phép")]
        public async Task<IActionResult> ConfigureSchedule([FromBody] ConfigureWorkScheduleDto dto, CancellationToken ct)
        {
            try
            {
                // Trích xuất ID Admin thực hiện thao tác từ JWT Token
                int actorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                await _useCase.ConfigureWorkScheduleAsync(dto, actorId, ct);

                return Ok(new { Success = true, Message = "Thiết lập Ca và Quỹ phép bộ phận thành công." });
            }
            catch (ArgumentException ex)
            {
                // Trả về mã lỗi 400 Bad Request chuẩn xác khi sai logic thời gian đầu vào
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                // Trả về mã lỗi 500 nếu có sự cố DB hoặc Rollback Transaction xảy ra
                return StatusCode(500, new { Success = false, Message = "Lỗi xử lý hệ thống: " + ex.Message });
            }
        }

        [HttpGet("configs")]
        [RequirePermission("WORK_SHIFT_VIEW", GroupName = SystemModules.TimekeepingLeave, Description = "Xem cấu hình ca làm việc và quỹ phép")]
        public async Task<IActionResult> GetConfiguredSchedules(CancellationToken ct)
        {
            try
            {
                var data = await _useCase.GetConfiguredSchedulesAsync(ct);
                return Ok(new { Success = true, Data = data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi truy xuất hệ thống: " + ex.Message });
            }
        }
    }
}
