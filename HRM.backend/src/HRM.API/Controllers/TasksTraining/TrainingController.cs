using System.Security.Claims;
using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.TasksTraining;
using HRM.backend.src.HRM.Application.Interfaces.TasksTraining.Usecases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.TasksTraining
{
    [ApiController]
    [Route("api/v1/training")]
    [Authorize]
    public class TrainingController : ControllerBase
    {
        private readonly ITrainingUseCase _useCase;

        public TrainingController(ITrainingUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet("pending-evaluation")]
        [RequirePermission("TRAINING_EVALUATE", GroupName = SystemModules.PerformanceTraining, Description = "Xem hồ sơ đào tạo chờ đánh giá")]
        public async Task<IActionResult> GetPending(CancellationToken ct)
        {
            var data = await _useCase.GetPendingEvaluationsAsync(GetAccountId(), GetRole(), ct);
            return Ok(new { Success = true, Data = data });
        }

        [HttpGet("summary/{trainingId:int}")]
        [RequirePermission("TRAINING_EVALUATE", GroupName = SystemModules.PerformanceTraining, Description = "Xem tổng hợp đào tạo")]
        public async Task<IActionResult> GetSummary(int trainingId, CancellationToken ct)
        {
            var data = await _useCase.GetTrainingReportAsync(trainingId, GetAccountId(), GetRole(), ct);
            return Ok(new { Success = true, Data = data });
        }

        [HttpPost("evaluate")]
        [RequirePermission("TRAINING_EVALUATE", GroupName = SystemModules.PerformanceTraining, Description = "Đánh giá quá trình đào tạo")]
        public async Task<IActionResult> Evaluate([FromBody] EvaluateTrainingDto dto, CancellationToken ct)
        {
            try
            {
                await _useCase.EvaluateProcessAsync(dto, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Message = "Training evaluation updated." });
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
        private string GetRole() => User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }
}
