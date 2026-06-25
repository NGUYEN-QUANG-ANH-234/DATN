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
    [Route("api/v1/leave-requests")]
    [Authorize]
    public class LeaveRequestController : ControllerBase
    {
        private readonly ILeaveRequestUseCase _useCase;

        public LeaveRequestController(ILeaveRequestUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpPost]
        [RequirePermission("LEAVE_REQUEST_CREATE", GroupName = SystemModules.TimekeepingLeave, Description = "Tạo đơn xin nghỉ phép")]
        public async Task<IActionResult> Create([FromBody] CreateLeaveRequestDto dto, CancellationToken ct)
        {
            try
            {
                var id = await _useCase.CreateAsync(dto, GetAccountId(), ct, GetIdempotencyKey());
                return Created($"/api/v1/leave-requests/{id}", new { Success = true, Data = id, Message = "Đã gửi đơn nghỉ phép. Chờ Trưởng phòng thẩm định." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return UnprocessableEntity(new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("my")]
        [RequirePermission("LEAVE_REQUEST_VIEW_SELF", GroupName = SystemModules.TimekeepingLeave, Description = "Xem đơn nghỉ phép cá nhân")]
        public async Task<IActionResult> GetMyRequests(CancellationToken ct)
        {
            try
            {
                var data = await _useCase.GetMyRequestsAsync(GetAccountId(), ct);
                return Ok(new { Success = true, Data = data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("pending-dept")]
        [RequirePermission("LEAVE_DEPT_REVIEW", GroupName = SystemModules.TimekeepingLeave, Description = "Xem và thẩm định đơn nghỉ phép cấp Trưởng phòng")]
        public async Task<IActionResult> GetPendingDept(CancellationToken ct)
        {
            try
            {
                var data = await _useCase.GetPendingDeptAsync(GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Data = data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("pending-director")]
        [RequirePermission("LEAVE_DIRECTOR_APPROVE", GroupName = SystemModules.TimekeepingLeave, Description = "Xem và phê duyệt cuối đơn nghỉ phép")]
        public async Task<IActionResult> GetPendingDirector(CancellationToken ct)
        {
            try
            {
                var data = await _useCase.GetPendingDirectorAsync(GetRole(), ct);
                return Ok(new { Success = true, Data = data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("pending-hr")]
        [RequirePermission("LEAVE_HR_CONFIRM", GroupName = SystemModules.TimekeepingLeave, Description = "Xem đơn nghỉ phép chờ HR ghi nhận")]
        public async Task<IActionResult> GetPendingHR(CancellationToken ct)
        {
            try
            {
                var data = await _useCase.GetPendingHRAsync(GetRole(), ct);
                return Ok(new { Success = true, Data = data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPatch("{id}/dept-approve")]
        [RequirePermission("LEAVE_DEPT_REVIEW", GroupName = SystemModules.TimekeepingLeave, Description = "Thẩm định đơn nghỉ phép cấp Trưởng phòng")]
        public async Task<IActionResult> ReviewByDept(int id, [FromBody] ReviewLeaveRequestDto dto, CancellationToken ct)
        {
            try
            {
                await _useCase.ReviewByDeptAsync(id, dto, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Message = "Trạng thái đơn nghỉ phép đã được cập nhật." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return UnprocessableEntity(new { Success = false, Message = ex.Message });
            }
        }

        [HttpPatch("{id}/final-approve")]
        [RequirePermission("LEAVE_DIRECTOR_APPROVE", GroupName = SystemModules.TimekeepingLeave, Description = "Phê duyệt cuối đơn nghỉ phép")]
        public async Task<IActionResult> FinalApprove(int id, [FromBody] ReviewLeaveRequestDto dto, CancellationToken ct)
        {
            try
            {
                await _useCase.FinalApproveAsync(id, dto, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Message = "Quy trình nghỉ phép đã hoàn tất." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return UnprocessableEntity(new { Success = false, Message = ex.Message });
            }
        }

        [HttpPatch("{id}/hr-confirm")]
        [RequirePermission("LEAVE_HR_CONFIRM", GroupName = SystemModules.TimekeepingLeave, Description = "HR ghi nhận đơn nghỉ phép")]
        public async Task<IActionResult> HrConfirm(int id, [FromBody] ReviewLeaveRequestDto dto, CancellationToken ct)
        {
            try
            {
                await _useCase.HrConfirmAsync(id, dto, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Message = "Đơn nghỉ phép đã được HR ghi nhận." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return UnprocessableEntity(new { Success = false, Message = ex.Message });
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

        private string? GetIdempotencyKey()
        {
            return Request.Headers["Idempotency-Key"].FirstOrDefault();
        }
    }
}
