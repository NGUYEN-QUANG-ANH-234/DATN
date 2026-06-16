using System.Security.Claims;
using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;
using HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Usecases;
using HRM.backend.src.HRM.Application.UseCases.PayrollAllowances;
using HRM.backend.src.HRM.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.PayrollAllowances
{
    [ApiController]
    [Route("api/v1/payroll/external-timesheet-imports")]
    [Authorize]
    public class ExternalTimesheetImportController : ControllerBase
    {
        private readonly IExternalTimesheetImportUseCase _useCase;

        public ExternalTimesheetImportController(IExternalTimesheetImportUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Xem batch giờ công cộng tác viên")]
        public async Task<IActionResult> GetBatches([FromQuery] byte? month, [FromQuery] short? year, [FromQuery] ExternalTimesheetImportStatus? status, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.GetBatchesAsync(month, year, status, GetRole(), ct);
                return Ok(new { Success = true, Data = result });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("pending-director")]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Xem batch giờ công cộng tác viên chờ duyệt")]
        public async Task<IActionResult> GetPendingDirector(CancellationToken ct)
        {
            try
            {
                var result = await _useCase.GetPendingDirectorAsync(GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Data = result });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("{id:int}")]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Xem chi tiết batch giờ công cộng tác viên")]
        public async Task<IActionResult> GetDetail([FromRoute] int id, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.GetDetailAsync(id, GetRole(), ct);
                return Ok(new { Success = true, Data = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Success = false, Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("preview")]
        [Consumes("multipart/form-data")]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Xem trước file giờ công cộng tác viên")]
        public async Task<IActionResult> Preview([FromForm] ExternalTimesheetImportRequestDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.PreviewAsync(dto, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Data = result });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Success = false, Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Import giờ công cộng tác viên")]
        public async Task<IActionResult> Import([FromForm] ExternalTimesheetImportRequestDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.ImportAsync(dto, GetAccountId(), GetRole(), ct);
                return StatusCode(StatusCodes.Status201Created, new { Success = true, Data = result, Message = "Đã import giờ công cộng tác viên ở trạng thái nháp." });
            }
            catch (ExternalTimesheetImportValidationException ex)
            {
                return UnprocessableEntity(new { Success = false, Message = ex.Message, Data = ex.Preview });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Success = false, Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return UnprocessableEntity(new { Success = false, Message = ex.Message });
            }
        }

        [HttpPatch("{id:int}/submit")]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Gửi duyệt giờ công cộng tác viên")]
        public async Task<IActionResult> Submit([FromRoute] int id, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.SubmitAsync(id, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Data = result, Message = "Đã gửi batch giờ công cộng tác viên cho Giám đốc duyệt." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Success = false, Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return UnprocessableEntity(new { Success = false, Message = ex.Message });
            }
        }

        [HttpPatch("{id:int}/cancel")]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Hủy batch giờ công cộng tác viên")]
        public async Task<IActionResult> Cancel([FromRoute] int id, [FromBody] CancelExternalTimesheetImportDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.CancelAsync(id, GetAccountId(), GetRole(), dto.Note, ct);
                return Ok(new { Success = true, Data = result, Message = "Đã hủy batch giờ công cộng tác viên." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Success = false, Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return UnprocessableEntity(new { Success = false, Message = ex.Message });
            }
        }

        [HttpPatch("{id:int}/director-review")]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Duyệt giờ công cộng tác viên")]
        public async Task<IActionResult> DirectorReview([FromRoute] int id, [FromBody] ReviewExternalTimesheetImportDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.DirectorReviewAsync(id, dto, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Data = result, Message = dto.IsApproved ? "Đã duyệt giờ công cộng tác viên." : "Đã từ chối batch giờ công cộng tác viên." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Success = false, Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return UnprocessableEntity(new { Success = false, Message = ex.Message });
            }
        }

        private int GetAccountId() => User.GetAccountIdOrThrow();

        private string GetRole() =>
            User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }
}
