using System.Security.Claims;
using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;
using HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Usecases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.PayrollAllowances
{
    [ApiController]
    [Route("api/v1/salary-slips")]
    [Authorize]
    public class SalarySlipController : ControllerBase
    {
        private readonly IPayrollAccessUseCase _accessUseCase;

        public SalarySlipController(IPayrollAccessUseCase accessUseCase)
        {
            _accessUseCase = accessUseCase;
        }

        [HttpGet]
        [RequirePermission("SALARY_SLIP_VIEW", GroupName = SystemModules.SalaryBonus, Description = "Tra cứu phiếu lương theo phạm vi phân quyền")]
        public async Task<IActionResult> Get([FromQuery] string? period, [FromQuery] byte? month, [FromQuery] short? year, CancellationToken ct)
        {
            try
            {
                var data = await _accessUseCase.GetSalarySlipsAsync(User.GetAccountIdOrThrow(), GetRole(), period, month, year, ct);
                return Ok(new { Success = true, Data = data });
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

        [HttpGet("my")]
        [RequirePermission("SALARY_SLIP_VIEW", GroupName = SystemModules.SalaryBonus, Description = "Xem phiếu lương cá nhân")]
        public async Task<IActionResult> GetMy([FromQuery] string? period, [FromQuery] byte? month, [FromQuery] short? year, CancellationToken ct)
        {
            try
            {
                var data = await _accessUseCase.GetMySalarySlipsAsync(User.GetAccountIdOrThrow(), period, month, year, ct);
                return Ok(new { Success = true, Data = data });
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

        [HttpGet("{id:int}")]
        [RequirePermission("SALARY_SLIP_VIEW", GroupName = SystemModules.SalaryBonus, Description = "Xem chi tiết phiếu lương")]
        public async Task<IActionResult> GetDetail(int id, CancellationToken ct)
        {
            try
            {
                var data = await _accessUseCase.GetSalarySlipDetailAsync(User.GetAccountIdOrThrow(), GetRole(), id, ct);
                return Ok(new { Success = true, Data = data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("my/{id:int}")]
        [RequirePermission("SALARY_SLIP_VIEW", GroupName = SystemModules.SalaryBonus, Description = "Xem chi tiết phiếu lương cá nhân")]
        public async Task<IActionResult> GetMyDetail(int id, CancellationToken ct)
        {
            try
            {
                var data = await _accessUseCase.GetMySalarySlipDetailAsync(User.GetAccountIdOrThrow(), id, ct);
                return Ok(new { Success = true, Data = data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("export")]
        [RequirePermission("SALARY_SLIP_EXPORT", GroupName = SystemModules.SalaryBonus, Description = "Kết xuất phiếu lương có kiểm tra quyền truy cập")]
        public async Task<IActionResult> Export([FromBody] SalarySlipExportRequestDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _accessUseCase.GenerateSalarySlipFilesAsync(User.GetAccountIdOrThrow(), GetRole(), GetEmail(), dto, ct);
                return File(result.Content, result.ContentType, result.FileName);
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

        private string GetRole()
        {
            return User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }

        private string? GetEmail()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value ??
                   User.FindFirst("email")?.Value ??
                   User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        }
    }
}
