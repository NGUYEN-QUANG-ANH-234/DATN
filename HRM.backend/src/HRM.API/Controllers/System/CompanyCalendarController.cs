using System.Security.Claims;
using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.System
{
    [ApiController]
    [Route("api/v1/system/company-calendar")]
    [Authorize]
    public class CompanyCalendarController : ControllerBase
    {
        private readonly ICompanyCalendarUseCase _useCase;

        public CompanyCalendarController(ICompanyCalendarUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet("active/{year:int}")]
        public async Task<IActionResult> GetActiveByYear(short year, CancellationToken ct)
        {
            var data = await _useCase.GetActiveByYearAsync(year, ct);
            return Ok(new { success = true, data });
        }

        [HttpGet("{year:int}")]
        [Authorize(Roles = "Admin,HR")]
        [RequirePermission("ATTENDANCE_CONFIG_VIEW", GroupName = SystemModules.TimekeepingLeave, Description = "Xem lịch nghỉ công ty")]
        public async Task<IActionResult> GetByYear(short year, CancellationToken ct)
        {
            var data = await _useCase.GetByYearAsync(year, ct);
            return Ok(new { success = true, data });
        }

        [HttpPut("{year:int}")]
        [Authorize(Roles = "Admin,HR")]
        [RequirePermission("ATTENDANCE_CONFIG_UPDATE", GroupName = SystemModules.TimekeepingLeave, Description = "Cập nhật lịch nghỉ công ty")]
        public async Task<IActionResult> Save(short year, [FromBody] SaveCompanyCalendarDto dto, CancellationToken ct)
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(claim, out var actorId))
                return Unauthorized(new { success = false, message = "Không xác định được người dùng." });

            try
            {
                var data = await _useCase.SaveAsync(year, dto, actorId, ct);
                return Ok(new { success = true, data, message = "Đã lưu lịch nghỉ công ty." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
