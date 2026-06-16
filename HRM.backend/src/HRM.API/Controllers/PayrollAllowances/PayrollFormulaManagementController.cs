using System.Security.Claims;
using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;
using HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Usecases;
using HRM.backend.src.HRM.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.PayrollAllowances
{
    [ApiController]
    [Route("api/v1/payroll/formulas")]
    [Authorize]
    public class PayrollFormulaManagementController : ControllerBase
    {
        private readonly IPayrollFormulaManagementUseCase _useCase;

        public PayrollFormulaManagementController(IPayrollFormulaManagementUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Xem cong thuc luong")]
        public async Task<IActionResult> GetList([FromQuery] FormulaStatus? status, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.GetListAsync(status, GetRole(), ct);
                return Ok(new { Success = true, Data = result });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("variables")]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Xem bien luong dung cho cong thuc")]
        public async Task<IActionResult> GetVariables(CancellationToken ct)
        {
            try
            {
                var result = await _useCase.GetVariablesAsync(GetRole(), ct);
                return Ok(new { Success = true, Data = result });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("{id:int}")]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Xem chi tiet cong thuc luong")]
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

        [HttpPost("validate")]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Kiem tra cong thuc luong")]
        public async Task<IActionResult> Validate([FromBody] UpsertPayrollFormulaDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.ValidateAsync(dto, GetRole(), ct);
                return Ok(new { Success = true, Data = result });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Tao cong thuc luong")]
        public async Task<IActionResult> CreateDraft([FromBody] UpsertPayrollFormulaDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.CreateDraftAsync(dto, GetAccountId(), GetRole(), ct);
                return StatusCode(StatusCodes.Status201Created, new { Success = true, Data = result, Message = "Da tao ban nhap cong thuc luong." });
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

        [HttpPut("{id:int}")]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Sua cong thuc luong")]
        public async Task<IActionResult> UpdateDraft([FromRoute] int id, [FromBody] UpsertPayrollFormulaDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.UpdateDraftAsync(id, dto, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Data = result, Message = "Da cap nhat cong thuc luong." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Success = false, Message = ex.Message });
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

        [HttpPost("{id:int}/clone")]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Clone version cong thuc luong")]
        public async Task<IActionResult> CloneVersion([FromRoute] int id, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.CloneVersionAsync(id, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Data = result, Message = "Da tao version cong thuc moi." });
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

        [HttpPatch("{id:int}/submit")]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Gui duyet cong thuc luong")]
        public async Task<IActionResult> Submit([FromRoute] int id, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.SubmitForApprovalAsync(id, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Data = result, Message = "Da gui cong thuc cho Giam doc duyet." });
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
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Duyet cong thuc luong")]
        public async Task<IActionResult> DirectorReview([FromRoute] int id, [FromBody] PayrollFormulaReviewDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.DirectorReviewAsync(id, dto, GetAccountId(), GetRole(), ct);
                var message = dto.IsApproved
                    ? "Da duyet cong thuc luong."
                    : dto.RequestRevision ? "Da gui yeu cau chinh sua ve HR." : "Da tu choi cong thuc luong.";
                return Ok(new { Success = true, Data = result, Message = message });
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

        [HttpPatch("{id:int}/activate")]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Kich hoat cong thuc luong")]
        public async Task<IActionResult> Activate([FromRoute] int id, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.ActivateAsync(id, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Data = result, Message = "Da kich hoat cong thuc luong." });
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

        [HttpPatch("{id:int}/archive")]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Luu tru cong thuc luong")]
        public async Task<IActionResult> Archive([FromRoute] int id, [FromBody] PayrollFormulaActionNoteDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.ArchiveAsync(id, dto, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Data = result, Message = "Da luu tru cong thuc luong." });
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
