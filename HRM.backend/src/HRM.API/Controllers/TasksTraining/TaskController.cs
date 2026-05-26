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
    [Route("api/v1/tasks")]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly ITaskManagementUseCase _useCase;

        public TaskController(ITaskManagementUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet("my")]
        [RequirePermission("TASK_VIEW_SELF", GroupName = SystemModules.PerformanceTraining, Description = "Xem công việc được giao")]
        public async Task<IActionResult> GetMyTasks(CancellationToken ct)
        {
            var data = await _useCase.GetMyTasksAsync(GetAccountId(), ct);
            return Ok(new { Success = true, Data = data });
        }

        [HttpGet("pending-review")]
        [RequirePermission("TASK_REVIEW", GroupName = SystemModules.PerformanceTraining, Description = "Xem công việc chờ đánh giá")]
        public async Task<IActionResult> GetPendingReview(CancellationToken ct)
        {
            var data = await _useCase.GetPendingReviewAsync(GetAccountId(), GetRole(), ct);
            return Ok(new { Success = true, Data = data });
        }

        [HttpGet("{id}/review-context")]
        [RequirePermission("TASK_REVIEW", GroupName = SystemModules.PerformanceTraining, Description = "Xem ngữ cảnh đánh giá công việc")]
        public async Task<IActionResult> GetReviewContext(int id, CancellationToken ct)
        {
            var data = await _useCase.GetReviewContextAsync(id, GetAccountId(), GetRole(), ct);
            return Ok(new { Success = true, Data = data });
        }

        [HttpPatch("{id}/progress")]
        [Consumes("multipart/form-data")]
        [RequirePermission("TASK_UPDATE_PROGRESS", GroupName = SystemModules.PerformanceTraining, Description = "Cập nhật tiến độ công việc")]
        public async Task<IActionResult> UpdateProgress(int id, [FromForm] TaskProgressUpdateDto dto, CancellationToken ct)
        {
            try
            {
                await _useCase.UpdateProgressAsync(id, dto, GetAccountId(), ct);
                return Ok(new { Success = true, Message = "Task progress updated." });
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

        [HttpPatch("{id}/feedback")]
        [RequirePermission("TASK_REVIEW", GroupName = SystemModules.PerformanceTraining, Description = "Yêu cầu điều chỉnh công việc")]
        public async Task<IActionResult> ProvideFeedback(int id, [FromBody] TaskFeedbackDto dto, CancellationToken ct)
        {
            try
            {
                await _useCase.ProvideFeedbackAsync(id, dto, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Message = "Task feedback saved." });
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

        [HttpPatch("{id}/approve")]
        [RequirePermission("TASK_REVIEW", GroupName = SystemModules.PerformanceTraining, Description = "Phê duyệt kết quả công việc")]
        public async Task<IActionResult> Approve(int id, CancellationToken ct)
        {
            try
            {
                await _useCase.ApproveTaskAsync(id, GetAccountId(), GetRole(), ct);
                return Ok(new { Success = true, Message = "Task approved." });
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

        private int GetAccountId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        private string GetRole() => User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }
}
