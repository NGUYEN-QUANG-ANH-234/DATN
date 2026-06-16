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

        [HttpPatch("submit")]
        [RequirePermission("ATTENDANCE_SUMMARY_VIEW", GroupName = SystemModules.TimekeepingLeave, Description = "Gửi chốt bảng công tháng")]
        public async Task<IActionResult> SubmitMonthly([FromBody] CloseAttendancePeriodDto dto, CancellationToken ct)
        {
            try
            {
                var data = await _useCase.SubmitMonthlyTimesheetAsync(dto, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Data = data, Message = $"Đã gửi chốt bảng công tháng {dto.Month:D2}/{dto.Year}." });
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

        [HttpPatch("approve")]
        [RequirePermission("ATTENDANCE_SUMMARY_VIEW", GroupName = SystemModules.TimekeepingLeave, Description = "Duyệt bảng công tháng")]
        public async Task<IActionResult> ApproveMonthly([FromBody] CloseAttendancePeriodDto dto, CancellationToken ct)
        {
            try
            {
                var data = await _useCase.ApproveMonthlyTimesheetAsync(dto, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Data = data, Message = $"Đã duyệt bảng công tháng {dto.Month:D2}/{dto.Year}." });
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

        [HttpPatch("lock")]
        [RequirePermission("ATTENDANCE_SUMMARY_VIEW", GroupName = SystemModules.TimekeepingLeave, Description = "Khóa bảng công tháng")]
        public async Task<IActionResult> LockMonthly([FromBody] CloseAttendancePeriodDto dto, CancellationToken ct)
        {
            try
            {
                var data = await _useCase.LockMonthlyTimesheetAsync(dto, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Data = data, Message = $"Đã khóa bảng công tháng {dto.Month:D2}/{dto.Year}." });
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

        [HttpGet("daily")]
        [RequirePermission("ATTENDANCE_SUMMARY_VIEW", GroupName = SystemModules.TimekeepingLeave, Description = "Xem bảng công theo ngày")]
        public async Task<IActionResult> GetDaily([FromQuery] byte month, [FromQuery] short year, CancellationToken ct)
        {
            try
            {
                var data = await _useCase.GetDailyAsync(month, year, GetRole(), ct);
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

        [HttpPatch("daily/{id:int}/adjust")]
        [RequirePermission("ATTENDANCE_SUMMARY_GENERATE", GroupName = SystemModules.TimekeepingLeave, Description = "Điều chỉnh bảng công theo ngày")]
        public async Task<IActionResult> AdjustDaily(int id, [FromBody] AdjustAttendanceDailySummaryDto dto, CancellationToken ct)
        {
            try
            {
                var data = await _useCase.AdjustDailyAsync(id, dto, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Data = data, Message = "Đã điều chỉnh bảng công ngày." });
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

        [HttpPatch("daily/{id:int}/approve")]
        [RequirePermission("ATTENDANCE_SUMMARY_GENERATE", GroupName = SystemModules.TimekeepingLeave, Description = "Phê duyệt bảng công theo ngày")]
        public async Task<IActionResult> ApproveDaily(int id, CancellationToken ct)
        {
            try
            {
                var data = await _useCase.ApproveDailyAsync(id, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Data = data, Message = "Đã phê duyệt bảng công ngày." });
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
            return User.GetAccountIdOrThrow();
        }

        private string GetRole()
        {
            return User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }
    }
}
