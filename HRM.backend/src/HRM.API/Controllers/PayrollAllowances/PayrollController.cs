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
    [Route("api/v1/payroll")]
    [Authorize]
    public class PayrollController : ControllerBase
    {
        private readonly IPayrollCalculationUseCase _calculationUseCase;

        public PayrollController(IPayrollCalculationUseCase calculationUseCase)
        {
            _calculationUseCase = calculationUseCase;
        }

        [HttpPost("calculate")]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Tổng hợp bảng lương nháp theo kỳ")]
        public async Task<IActionResult> Calculate([FromBody] PayrollPeriodDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _calculationUseCase.ExecuteCalculationAsync(dto, User.GetAccountIdOrThrow(), GetRole(), ct);
                return Ok(new { Success = true, Data = result, Message = $"Đã tổng hợp bảng lương {dto.Month:00}/{dto.Year}." });
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

        [HttpGet("adjustments")]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Xem truy lĩnh/truy thu lương")]
        public async Task<IActionResult> GetAdjustments([FromQuery] byte month, [FromQuery] short year, CancellationToken ct)
        {
            try
            {
                var result = await _calculationUseCase.GetAdjustmentsAsync(month, year, GetRole(), ct);
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

        [HttpPost("adjustments")]
        [RequirePermission("PAYROLL_CALCULATE", GroupName = SystemModules.SalaryBonus, Description = "Tạo truy lĩnh/truy thu lương")]
        public async Task<IActionResult> CreateAdjustment([FromBody] CreatePayrollAdjustmentDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _calculationUseCase.CreateAdjustmentAsync(dto, User.GetAccountIdOrThrow(), GetRole(), ct);
                return CreatedAtAction(nameof(GetAdjustments), new { month = result.RecognizedMonth, year = result.RecognizedYear }, new { Success = true, Data = result, Message = "Đã ghi nhận điều chỉnh lương." });
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
    }
}
