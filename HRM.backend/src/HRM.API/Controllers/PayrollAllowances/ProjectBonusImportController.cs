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
    [Route("api/v1/payroll/project-bonus-imports")]
    [Authorize]
    public class ProjectBonusImportController : ControllerBase
    {
        private readonly IProjectBonusImportUseCase _useCase;

        public ProjectBonusImportController(IProjectBonusImportUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Xem batch thưởng dự án")]
        public async Task<IActionResult> GetBatches([FromQuery] byte? month, [FromQuery] short? year, [FromQuery] ProjectBonusImportStatus? status, CancellationToken ct)
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
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Xem batch thưởng dự án chờ duyệt")]
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
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Xem chi tiết batch thưởng dự án")]
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
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Xem trước file thưởng dự án")]
        public async Task<IActionResult> Preview([FromForm] ProjectBonusImportRequestDto dto, CancellationToken ct)
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
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Import thưởng dự án")]
        public async Task<IActionResult> Import([FromForm] ProjectBonusImportRequestDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.ImportAsync(dto, GetAccountId(), GetRole(), ct);
                return StatusCode(StatusCodes.Status201Created, new { Success = true, Data = result, Message = "Đã import thưởng dự án ở trạng thái nháp." });
            }
            catch (ProjectBonusImportValidationException ex)
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
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Gửi duyệt thưởng dự án")]
        public async Task<IActionResult> Submit([FromRoute] int id, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.SubmitAsync(id, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Data = result, Message = "Đã gửi batch thưởng dự án cho Giám đốc duyệt." });
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
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Hủy batch thưởng dự án")]
        public async Task<IActionResult> Cancel([FromRoute] int id, [FromBody] CancelProjectBonusImportDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.CancelAsync(id, GetAccountId(), GetRole(), dto.Note, ct);
                return Ok(new { Success = true, Data = result, Message = "Đã hủy batch thưởng dự án." });
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
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Duyệt thưởng dự án")]
        public async Task<IActionResult> DirectorReview([FromRoute] int id, [FromBody] ReviewProjectBonusImportDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.DirectorReviewAsync(id, dto, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Data = result, Message = dto.IsApproved ? "Đã duyệt thưởng dự án." : "Đã từ chối batch thưởng dự án." });
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
