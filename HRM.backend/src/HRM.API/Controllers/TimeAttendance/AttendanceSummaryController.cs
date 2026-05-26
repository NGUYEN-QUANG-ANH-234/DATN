using System.Security.Claims;
using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.TimeAttendance;
using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Usecases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.TimeAttendance
{
    [ApiController]
    [Route("api/v1/attendance-summaries")]
    [Authorize]
    public class AttendanceSummaryController : ControllerBase
    {
        private readonly IAttendanceSummaryUseCase _useCase;

        public AttendanceSummaryController(IAttendanceSummaryUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet]
        [RequirePermission("ATTENDANCE_SUMMARY_VIEW", GroupName = SystemModules.TimekeepingLeave, Description = "Xem bảng công tổng hợp")]
        public async Task<IActionResult> GetMonthly([FromQuery] byte month, [FromQuery] short year, CancellationToken ct)
        {
            try
            {
                var data = await _useCase.GetMonthlyAsync(month, year, GetRole(), ct);
                return Ok(new { Success = true, Data = data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("generate")]
        [RequirePermission("ATTENDANCE_SUMMARY_GENERATE", GroupName = SystemModules.TimekeepingLeave, Description = "Tổng hợp bảng công theo tháng")]
        public async Task<IActionResult> Generate([FromBody] GenerateAttendanceSummaryDto dto, CancellationToken ct)
        {
            try
            {
                var data = await _useCase.GenerateMonthlyAsync(dto, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Data = data, Message = $"Đã tổng hợp bảng công tháng {dto.Month:D2}/{dto.Year}." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }

        private int GetAccountId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        private string GetRole()
        {
            return User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }
    }
}
