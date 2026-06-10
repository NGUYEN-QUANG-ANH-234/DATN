using System.Security.Claims;
using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.TasksTraining;
using HRM.backend.src.HRM.Application.Interfaces.TasksTraining.Usecases;
using HRM.backend.src.HRM.Application.UseCases.TasksTraining;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.TasksTraining
{
    [ApiController]
    [Route("api/v1/kpis")]
    [Authorize]
    public class KpiController : ControllerBase
    {
        private readonly IKpiManagementUseCase _kpiUseCase;

        public KpiController(IKpiManagementUseCase kpiUseCase)
        {
            _kpiUseCase = kpiUseCase;
        }

        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        [RequirePermission("KPI_IMPORT", GroupName = SystemModules.PerformanceTraining, Description = "Import KPI đầu kỳ cho nhân viên trong phòng ban")]
        public async Task<IActionResult> Import([FromForm] KpiImportRequestDto dto, CancellationToken ct)
        {
            try
            {
                var result = await _kpiUseCase.ImportKpisFromExcelAsync(dto, GetAccountId(), GetRole(), ct);
                return StatusCode(StatusCodes.Status201Created, new
                {
                    Success = true,
                    Data = result,
                    Message = $"Import thành công {result.SuccessRows} dòng KPI cho {result.CreatedOrUpdatedReviews} nhân viên."
                });
            }
            catch (KpiImportValidationException ex)
            {
                return UnprocessableEntity(new
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = ex.Errors
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return UnprocessableEntity(new { Success = false, Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
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
    }
}
