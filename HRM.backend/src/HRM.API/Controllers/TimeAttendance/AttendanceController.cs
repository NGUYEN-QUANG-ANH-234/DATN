using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.TimeAttendance;
using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Usecases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace HRM.backend.src.HRM.API.Controllers.TimeAttendance
{
    [ApiController]
    [Route("api/v1/attendance")]
    [Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceUseCase _useCase;

        public AttendanceController(IAttendanceUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet("me/today")]
        [RequirePermission("ATTENDANCE_SELF_LOG", GroupName = SystemModules.TimekeepingLeave, Description = "Nhân viên xem trạng thái chấm công trong ngày")]
        public async Task<IActionResult> GetTodayStatus(CancellationToken ct)
        {
            try
            {
                var accountId = User.GetAccountIdOrThrow();
                var result = await _useCase.GetTodayStatusAsync(accountId, ct);

                return Ok(new { Success = true, Data = result });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi truy xuất trạng thái chấm công: " + ex.Message });
            }
        }

        [HttpPost("log")]
        [RequirePermission("ATTENDANCE_SELF_LOG", GroupName = SystemModules.TimekeepingLeave, Description = "Nhân viên chấm công qua Web/PC")]
        public async Task<IActionResult> LogAttendance([FromBody] AttendanceGpsDto dto, CancellationToken ct)
        {
            try
            {
                var accountId = User.GetAccountIdOrThrow();
                var clientIp = ExtractClientIp();
                var result = await _useCase.VerifyAndRecordAsync(accountId, clientIp, dto, ct);

                return Ok(new { Success = true, Data = result, Message = result.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return UnprocessableEntity(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi xử lý chấm công: " + ex.Message });
            }
        }

        [HttpGet("my-network")]
        public IActionResult GetMyNetwork()
        {
            if (!IsAdmin())
                return Forbid();

            var source = "RemoteIpAddress";
            var clientIp = ExtractClientIpWithSource(ref source);

            return Ok(new
            {
                Success = true,
                Data = new AttendanceNetworkInfoDto
                {
                    ClientIp = clientIp,
                    SuggestedCidr = string.IsNullOrWhiteSpace(clientIp) ? string.Empty : $"{clientIp}/32",
                    Source = source
                }
            });
        }

        private string ExtractClientIp()
        {
            var source = "RemoteIpAddress";
            return ExtractClientIpWithSource(ref source);
        }

        private string ExtractClientIpWithSource(ref string source)
        {
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                source = "X-Forwarded-For";
                return forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).First();
            }

            var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(realIp))
            {
                source = "X-Real-IP";
                return realIp;
            }

            source = "RemoteIpAddress";
            var remoteIp = HttpContext.Connection.RemoteIpAddress;
            if (remoteIp == null)
                return string.Empty;

            if (IPAddress.IPv6Loopback.Equals(remoteIp) || IPAddress.Loopback.Equals(remoteIp))
                return IPAddress.Loopback.ToString();

            return remoteIp.IsIPv4MappedToIPv6 ? remoteIp.MapToIPv4().ToString() : remoteIp.ToString();
        }

        private bool IsAdmin()
        {
            var role = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
            return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        }
    }
}
