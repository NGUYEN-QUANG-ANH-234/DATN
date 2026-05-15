using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRM.backend.src.HRM.API.Controllers.System
{
    [ApiController]
    [Route("api/v1/system")]
    [Authorize(Roles = "Admin,HR")] // Mở comment khi tích hợp Middleware JWT
    public class SalaryVariableController : ControllerBase
    {
        private readonly ISalaryVariableUseCase _useCase;

        public SalaryVariableController(ISalaryVariableUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet("salary-variables")]
        public async Task<IActionResult> GetAllVariables(CancellationToken ct)
        {
            var variables = await _useCase.GetAllVariablesAsync(ct);
            return Ok(new { success = true, data = variables });
        }

        [HttpPost("salary-variables")]
        public async Task<IActionResult> DefineVariable([FromBody] VariableDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ.", errors = ModelState });

            try
            {
                // Lấy User ID an toàn (Tránh Null Reference)
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim, out int adminOrHrId))
                {
                    return Unauthorized(new { success = false, message = "Không xác định được danh tính người dùng." });
                }

                var isSuccess = await _useCase.DefineVariableAsync(dto, adminOrHrId, ct);

                if (isSuccess)
                {
                    return StatusCode(201, new
                    {
                        success = true,
                        message = "Biến lương mới đã sẵn sàng. HR hiện có thể chọn biến này khi lập công thức."
                    });
                }

                return StatusCode(500, new { success = false, message = "Lưu dữ liệu thất bại do lỗi không xác định." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception)
            {
                // Ở môi trường Dev, bạn nên dùng ILogger để ghi lại Exception thật để dễ fix bug
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống nội bộ trong quá trình xử lý Transaction." });
            }
        }
    }
}
