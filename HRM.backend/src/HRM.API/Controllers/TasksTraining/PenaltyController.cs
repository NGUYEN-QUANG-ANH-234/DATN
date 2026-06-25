using System.Security.Claims;
using HRM.backend.src.HRM.API.Extensions;
using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.TasksTraining;
using HRM.backend.src.HRM.Application.Interfaces.TasksTraining.Usecases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.TasksTraining
{
    [ApiController]
    [Route("api/v1/penalties")]
    [Authorize]
    public class PenaltyController : ControllerBase
    {
        private readonly IPenaltyManagementUseCase _useCase;

        public PenaltyController(IPenaltyManagementUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet]
        [RequirePermission("PENALTY_RECORD_VIEW", GroupName = SystemModules.PerformanceTraining, Description = "Xem danh sách biên bản vi phạm và điểm trừ")]
        public async Task<IActionResult> GetRecords([FromQuery] string? status, CancellationToken ct)
        {
            var data = await _useCase.GetRecordsAsync(status, GetAccountId(), GetRole(), ct);
            return Ok(new { Success = true, Data = data });
        }

        [HttpGet("my")]
        [RequirePermission("PENALTY_RECORD_EXPLAIN_SELF", GroupName = SystemModules.PerformanceTraining, Description = "Nhân viên xem biên bản vi phạm của mình")]
        public async Task<IActionResult> GetMyRecords(CancellationToken ct)
        {
            var data = await _useCase.GetMyRecordsAsync(GetAccountId(), ct);
            return Ok(new { Success = true, Data = data });
        }

        [HttpGet("{id:int}")]
        [RequirePermission("PENALTY_RECORD_VIEW", GroupName = SystemModules.PerformanceTraining, Description = "Xem chi tiết biên bản vi phạm")]
        public async Task<IActionResult> GetDetail(int id, CancellationToken ct)
        {
            var data = await _useCase.GetDetailAsync(id, GetAccountId(), GetRole(), ct);
            return Ok(new { Success = true, Data = data });
        }

        [HttpGet("employees/{employeeId:int}/history")]
        [RequirePermission("PENALTY_RECORD_VIEW", GroupName = SystemModules.PerformanceTraining, Description = "Xem lịch sử vi phạm của nhân sự")]
        public async Task<IActionResult> GetEmployeeHistory(int employeeId, CancellationToken ct)
        {
            var data = await _useCase.GetEmployeeHistoryAsync(employeeId, GetAccountId(), GetRole(), ct);
            return Ok(new { Success = true, Data = data });
        }

        [HttpPost("manual")]
        [RequirePermission("PENALTY_RECORD_CREATE", GroupName = SystemModules.PerformanceTraining, Description = "Manager hoặc HR lập biên bản vi phạm thủ công")]
        public async Task<IActionResult> CreateManual([FromBody] CreateManualPenaltyRecordDto dto, CancellationToken ct)
        {
            var data = await _useCase.CreateManualAsync(dto, GetAccountId(), GetRole(), ct);
            return Created($"api/v1/penalties/{data.Id}", new
            {
                Success = true,
                Message = "Biên bản vi phạm đã được ghi nhận và chuyển theo luồng xử lý.",
                Data = data
            });
        }

        [HttpPatch("{id:int}/explanation")]
        [RequirePermission("PENALTY_RECORD_EXPLAIN_SELF", GroupName = SystemModules.PerformanceTraining, Description = "Nhân viên gửi giải trình biên bản vi phạm")]
        public async Task<IActionResult> SubmitExplanation(int id, [FromBody] SubmitPenaltyExplanationDto dto, CancellationToken ct)
        {
            var data = await _useCase.SubmitExplanationAsync(id, dto, GetAccountId(), ct);
            return Ok(new
            {
                Success = true,
                Message = "Giải trình đã được gửi, chờ HR kiểm tra.",
                Data = data
            });
        }

        [HttpPatch("{id:int}/hr-review")]
        [RequirePermission("PENALTY_RECORD_REVIEW", GroupName = SystemModules.PerformanceTraining, Description = "HR ghi nhận hoặc hủy hiệu lực biên bản vi phạm")]
        public async Task<IActionResult> HrReview(int id, [FromBody] ReviewPenaltyRecordDto dto, CancellationToken ct)
        {
            var data = await _useCase.ReviewByHrAsync(id, dto, GetAccountId(), GetRole(), ct);
            return Ok(new
            {
                Success = true,
                Message = dto.IsApproved
                    ? "Biên bản đã được ghi nhận có hiệu lực xử lý."
                    : "Biên bản đã được đánh dấu không có hiệu lực xử lý.",
                Data = data
            });
        }

        [HttpPatch("{id:int}/director-review")]
        [RequirePermission("PENALTY_RECORD_DIRECTOR_REVIEW", GroupName = SystemModules.PerformanceTraining, Description = "Director duyệt biên bản vi phạm nghiêm trọng hoặc dùng cho hồ sơ kỷ luật")]
        public async Task<IActionResult> DirectorReview(int id, [FromBody] ReviewPenaltyRecordDto dto, CancellationToken ct)
        {
            var data = await _useCase.ReviewByDirectorAsync(id, dto, GetAccountId(), GetRole(), ct);
            return Ok(new
            {
                Success = true,
                Message = dto.IsApproved
                    ? "Giám đốc đã phê duyệt hiệu lực xử lý của biên bản."
                    : "Giám đốc không phê duyệt hiệu lực xử lý của biên bản.",
                Data = data
            });
        }

        private int GetAccountId() => User.GetAccountIdOrThrow();
        private string GetRole() => User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }
}
