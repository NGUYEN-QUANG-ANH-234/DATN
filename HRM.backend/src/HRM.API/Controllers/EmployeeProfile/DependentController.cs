using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;
using HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRM.backend.src.HRM.API.Controllers.EmployeeProfile
{
    [ApiController]
    [Route("api/v1/employees")]
    [Authorize]
    public class DependentController : ControllerBase
    {
        private readonly IDependentUseCase _useCase;
        private readonly string[] _permittedExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };
        private const long FileSizeLimit = 5 * 1024 * 1024;

        public DependentController(IDependentUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet("me/dependents")]
        [RequirePermission("PROFILE_SELF_VIEW", GroupName = SystemModules.ProfileContract, Description = "Xem người phụ thuộc cá nhân")]
        public async Task<IActionResult> GetMyDependents(CancellationToken ct)
        {
            var data = await _useCase.GetMyDependentsAsync(User.GetAccountIdOrThrow(), ct);
            return Ok(new { Success = true, Data = data });
        }

        [HttpPost("dependents/requests")]
        [Consumes("multipart/form-data")]
        [RequirePermission("PROFILE_SELF_UPDATE", GroupName = SystemModules.ProfileContract, Description = "Gửi yêu cầu thêm người phụ thuộc")]
        public async Task<IActionResult> RequestCreate([FromForm] DependentRequestDto dto, CancellationToken ct)
        {
            var validation = ValidateEvidence(dto.EvidenceFile);
            if (validation != null) return validation;

            try
            {
                var id = await _useCase.RequestCreateDependentAsync(User.GetAccountIdOrThrow(), dto, ct);
                return Ok(new { Success = true, Message = "Đã gửi yêu cầu người phụ thuộc. Vui lòng chờ HR duyệt.", Data = id });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Success = false, Message = ex.Message });
            }
        }

        [HttpPut("dependents/{id:int}/requests")]
        [Consumes("multipart/form-data")]
        [RequirePermission("PROFILE_SELF_UPDATE", GroupName = SystemModules.ProfileContract, Description = "Gửi yêu cầu sửa người phụ thuộc")]
        public async Task<IActionResult> RequestUpdate(int id, [FromForm] DependentRequestDto dto, CancellationToken ct)
        {
            var validation = ValidateEvidence(dto.EvidenceFile);
            if (validation != null) return validation;

            try
            {
                var requestId = await _useCase.RequestUpdateDependentAsync(User.GetAccountIdOrThrow(), id, dto, ct);
                return Ok(new { Success = true, Message = "Đã gửi yêu cầu cập nhật người phụ thuộc.", Data = requestId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Success = false, Message = ex.Message });
            }
        }

        [HttpPatch("dependents/{id:int}/deactivate-request")]
        [RequirePermission("PROFILE_SELF_UPDATE", GroupName = SystemModules.ProfileContract, Description = "Gửi yêu cầu ngừng hiệu lực người phụ thuộc")]
        public async Task<IActionResult> RequestDeactivate(int id, CancellationToken ct)
        {
            try
            {
                var requestId = await _useCase.RequestDeactivateDependentAsync(User.GetAccountIdOrThrow(), id, ct);
                return Ok(new { Success = true, Message = "Đã gửi yêu cầu ngừng hiệu lực người phụ thuộc.", Data = requestId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("dependent-requests/pending")]
        [RequirePermission("PROFILE_REQUEST_VIEW", GroupName = SystemModules.ProfileContract, Description = "Xem yêu cầu người phụ thuộc chờ duyệt")]
        public async Task<IActionResult> GetPendingRequests(CancellationToken ct)
        {
            var data = await _useCase.GetPendingRequestsAsync(User.GetAccountIdOrThrow(), GetRole(), ct);
            return Ok(new { Success = true, Data = data });
        }

        [HttpPatch("dependent-requests/{id:int}/review")]
        [RequirePermission("PROFILE_REQUEST_REVIEW", GroupName = SystemModules.ProfileContract, Description = "Duyệt yêu cầu người phụ thuộc")]
        public async Task<IActionResult> ReviewRequest(int id, [FromBody] ReviewProfileUpdateDto dto, CancellationToken ct)
        {
            try
            {
                await _useCase.ReviewRequestAsync(id, User.GetAccountIdOrThrow(), GetRole(), dto, ct);
                return Ok(new { Success = true, Message = dto.IsApproved ? "Đã duyệt yêu cầu người phụ thuộc." : "Đã từ chối yêu cầu người phụ thuộc." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("{employeeId:int}/dependents")]
        [RequirePermission("PROFILE_REQUEST_VIEW", GroupName = SystemModules.ProfileContract, Description = "HR xem người phụ thuộc của nhân viên")]
        public async Task<IActionResult> GetEmployeeDependents(int employeeId, CancellationToken ct)
        {
            var data = await _useCase.GetEmployeeDependentsAsync(employeeId, ct);
            return Ok(new { Success = true, Data = data });
        }

        [HttpPost("{employeeId:int}/dependents/hr")]
        [RequirePermission("PROFILE_REQUEST_REVIEW", GroupName = SystemModules.ProfileContract, Description = "HR thêm người phụ thuộc cho nhân viên")]
        public async Task<IActionResult> HrCreate(int employeeId, [FromBody] HrDependentDto dto, CancellationToken ct)
        {
            var data = await _useCase.HrCreateDependentAsync(employeeId, dto, User.GetAccountIdOrThrow(), ct);
            return Ok(new { Success = true, Message = "Đã thêm người phụ thuộc.", Data = data });
        }

        [HttpPut("{employeeId:int}/dependents/{dependentId:int}/hr")]
        [RequirePermission("PROFILE_REQUEST_REVIEW", GroupName = SystemModules.ProfileContract, Description = "HR sửa người phụ thuộc cho nhân viên")]
        public async Task<IActionResult> HrUpdate(int employeeId, int dependentId, [FromBody] HrDependentDto dto, CancellationToken ct)
        {
            var data = await _useCase.HrUpdateDependentAsync(employeeId, dependentId, dto, User.GetAccountIdOrThrow(), ct);
            return Ok(new { Success = true, Message = "Đã cập nhật người phụ thuộc.", Data = data });
        }

        [HttpPatch("{employeeId:int}/dependents/{dependentId:int}/deactivate/hr")]
        [RequirePermission("PROFILE_REQUEST_REVIEW", GroupName = SystemModules.ProfileContract, Description = "HR ngừng hiệu lực người phụ thuộc")]
        public async Task<IActionResult> HrDeactivate(int employeeId, int dependentId, CancellationToken ct)
        {
            await _useCase.HrDeactivateDependentAsync(employeeId, dependentId, User.GetAccountIdOrThrow(), ct);
            return Ok(new { Success = true, Message = "Đã ngừng hiệu lực người phụ thuộc." });
        }

        private IActionResult? ValidateEvidence(IFormFile? file)
        {
            if (file == null) return null;
            if (file.Length > FileSizeLimit)
                return BadRequest(new { Success = false, Message = "Chỉ chấp nhận file có dung lượng < 5MB." });
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !_permittedExtensions.Contains(ext))
                return BadRequest(new { Success = false, Message = "Định dạng không hợp lệ. Chỉ chấp nhận JPG, PNG hoặc PDF." });
            return null;
        }

        private string GetRole()
        {
            return User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }
    }
}
