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
    [Route("api/v1/overtime-requests")]
    [Authorize]
    public class OvertimeRequestController : ControllerBase
    {
        private readonly IOvertimeRequestUseCase _useCase;

        public OvertimeRequestController(IOvertimeRequestUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpPost]
        [RequirePermission("OVERTIME_REQUEST_CREATE", GroupName = SystemModules.TimekeepingLeave, Description = "Tạo yêu cầu làm thêm giờ")]
        public async Task<IActionResult> Create([FromBody] CreateOvertimeRequestDto dto, CancellationToken ct)
        {
            try
            {
                var id = await _useCase.CreateAsync(dto, GetAccountId(), GetRole(), ct, GetIdempotencyKey());
                return Created($"/api/v1/overtime-requests/{id}", new { Success = true, Data = id });
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

        [HttpPost("bulk")]
        [RequirePermission("OVERTIME_REQUEST_CREATE", GroupName = SystemModules.TimekeepingLeave, Description = "Tạo yêu cầu làm thêm giờ hàng loạt")]
        public async Task<IActionResult> CreateBulk([FromBody] CreateBulkOvertimeRequestDto dto, CancellationToken ct)
        {
            try
            {
                var ids = await _useCase.CreateBulkByManagerAsync(dto, GetAccountId(), GetRole(), ct, GetIdempotencyKey());
                return Created("/api/v1/overtime-requests/bulk", new { Success = true, Data = ids });
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
        [RequirePermission("OVERTIME_REQUEST_VIEW_SELF", GroupName = SystemModules.TimekeepingLeave, Description = "Xem yêu cầu OT cá nhân")]
        public async Task<IActionResult> GetMyRequests(CancellationToken ct)
        {
            var data = await _useCase.GetMyRequestsAsync(GetAccountId(), ct);
            return Ok(new { Success = true, Data = data });
        }

        [HttpGet("assignable-employees")]
        [RequirePermission("OVERTIME_REQUEST_CREATE", GroupName = SystemModules.TimekeepingLeave, Description = "Xem danh sách nhân viên có thể tạo OT")]
        public async Task<IActionResult> GetAssignableEmployees(CancellationToken ct)
        {
            try
            {
                var data = await _useCase.GetAssignableEmployeesAsync(GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Data = data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("pending-manager")]
        [RequirePermission("OVERTIME_MANAGER_REVIEW", GroupName = SystemModules.TimekeepingLeave, Description = "Xem và duyệt yêu cầu OT cấp quản lý")]
        public async Task<IActionResult> GetPendingManager(CancellationToken ct)
        {
            try
            {
                var data = await _useCase.GetPendingManagerAsync(GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Data = data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("pending-hr")]
        [RequirePermission("OVERTIME_HR_CONFIRM", GroupName = SystemModules.TimekeepingLeave, Description = "Xem và xác nhận yêu cầu OT cấp HR")]
        public async Task<IActionResult> GetPendingHr(CancellationToken ct)
        {
            try
            {
                var data = await _useCase.GetPendingHrAsync(GetRole(), ct);
                return Ok(new { Success = true, Data = data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("pending-director")]
        [RequirePermission("OVERTIME_DIRECTOR_REVIEW", GroupName = SystemModules.TimekeepingLeave, Description = "Xem va duyet truc tiep yeu cau OT cua HR/Truong phong")]
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

        [HttpGet("approved")]
        [RequirePermission("OVERTIME_RECONCILE", GroupName = SystemModules.TimekeepingLeave, Description = "Xem danh sách OT đã duyệt để đối chiếu")]
        public async Task<IActionResult> GetApproved([FromQuery] int? month, [FromQuery] int? year, CancellationToken ct)
        {
            try
            {
                var data = await _useCase.GetApprovedForHrAsync(GetRole(), month, year, ct);
                return Ok(new { Success = true, Data = data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPatch("{id}/manager-review")]
        [RequirePermission("OVERTIME_MANAGER_REVIEW", GroupName = SystemModules.TimekeepingLeave, Description = "Duyệt yêu cầu OT cấp quản lý")]
        public async Task<IActionResult> ReviewByManager(int id, [FromBody] ReviewOvertimeRequestDto dto, CancellationToken ct)
        {
            try
            {
                await _useCase.ReviewByManagerAsync(id, dto, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Message = "Đã xử lý yêu cầu OT ở cấp quản lý." });
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
        [RequirePermission("OVERTIME_HR_CONFIRM", GroupName = SystemModules.TimekeepingLeave, Description = "HR xác nhận yêu cầu OT")]
        public async Task<IActionResult> ConfirmByHr(int id, [FromBody] ReviewOvertimeRequestDto dto, CancellationToken ct)
        {
            try
            {
                await _useCase.ConfirmByHrAsync(id, dto, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Message = "Đã xử lý yêu cầu OT ở cấp HR." });
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

        [HttpPatch("{id}/director-review")]
        [RequirePermission("OVERTIME_DIRECTOR_REVIEW", GroupName = SystemModules.TimekeepingLeave, Description = "Giam doc duyet truc tiep yeu cau OT cua HR/Truong phong")]
        public async Task<IActionResult> ReviewByDirector(int id, [FromBody] ReviewOvertimeRequestDto dto, CancellationToken ct)
        {
            try
            {
                await _useCase.ReviewByDirectorAsync(id, dto, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Message = "Da xu ly yeu cau OT o cap Giam doc." });
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

        [HttpPost("{id}/reconcile")]
        [RequirePermission("OVERTIME_RECONCILE", GroupName = SystemModules.TimekeepingLeave, Description = "Đối chiếu OT với dữ liệu chấm công")]
        public async Task<IActionResult> Reconcile(int id, CancellationToken ct)
        {
            try
            {
                var data = await _useCase.ReconcileAsync(id, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Data = data, Message = "Đã đối chiếu OT với dữ liệu chấm công." });
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
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
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
