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
    [Route("api/v1/performance")]
    [Authorize]
    public class PerformanceController : ControllerBase
    {
        private readonly IPerformanceEvaluationUseCase _useCase;

        public PerformanceController(IPerformanceEvaluationUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet("my")]
        [RequirePermission("TASK_VIEW_SELF", GroupName = SystemModules.PerformanceTraining, Description = "Xem KPI cá nhân được giao")]
        public async Task<IActionResult> GetMy(CancellationToken ct)
        {
            var data = await _useCase.GetMyReviewsAsync(GetAccountId(), ct);
            return Ok(new { Success = true, Data = data });
        }

        [HttpGet("pending-evaluation")]
        [RequirePermission("PERFORMANCE_EVALUATE", GroupName = SystemModules.PerformanceTraining, Description = "Xem danh sách KPI chờ đánh giá")]
        public async Task<IActionResult> GetPending(CancellationToken ct)
        {
            var data = await _useCase.GetPendingEvaluationsAsync(GetAccountId(), GetRole(), ct);
            return Ok(new { Success = true, Data = data });
        }

        [HttpGet("{id}")]
        [RequirePermission("PERFORMANCE_EVALUATE", GroupName = SystemModules.PerformanceTraining, Description = "Xem chi tiết KPI để đánh giá")]
        public async Task<IActionResult> GetDetail(int id, CancellationToken ct)
        {
            var data = await _useCase.GetDetailAsync(id, GetAccountId(), GetRole(), ct);
            return Ok(new { Success = true, Data = data });
        }

        [HttpPatch("{id}/progress")]
        [RequirePermission("TASK_UPDATE_PROGRESS", GroupName = SystemModules.PerformanceTraining, Description = "Cập nhật tiến độ KPI cá nhân")]
        public async Task<IActionResult> UpdateProgress(int id, [FromBody] PerformanceProgressUpdateDto dto, CancellationToken ct)
        {
            try
            {
                await _useCase.UpdateMyProgressAsync(id, dto, GetAccountId(), ct);
                return Ok(new { Success = true, Message = "Performance progress updated." });
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

        [HttpPatch("{id}/finalize-score")]
        [RequirePermission("PERFORMANCE_EVALUATE", GroupName = SystemModules.PerformanceTraining, Description = "Chốt điểm đánh giá hiệu suất")]
        public async Task<IActionResult> FinalizeScore(int id, [FromBody] FinalizePerformanceDto dto, CancellationToken ct)
        {
            try
            {
                await _useCase.FinalizeScoreAsync(id, dto, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Message = "Performance evaluation updated." });
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
