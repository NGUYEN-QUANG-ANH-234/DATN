using HRM.backend.src.HRM.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRM.backend.src.HRM.API.Controllers.System
{
    [ApiController]
    [Route("api/v1/approvals")]
    public class ApprovalController : ControllerBase
    {
        private readonly IApprovalWorkflowService _approvalService;

        public ApprovalController(IApprovalWorkflowService approvalService)
        {
            _approvalService = approvalService;
        }

        [HttpGet("pending")]
        [Authorize]
        public async Task<IActionResult> GetPendingApprovals(CancellationToken ct)
        {
            try
            {
                int approverId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                string actorRoleName = User.FindFirst(ClaimTypes.Role)!.Value;

                var result = await _approvalService.GetPendingApprovalsAsync(approverId, actorRoleName, ct);

                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPost("process")]
        [Authorize]
        public async Task<IActionResult> ProcessStep([FromBody] ProcessApprovalDto dto, CancellationToken ct)
        {
            try
            {
                int approverId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                string actorRoleName = User.FindFirst(ClaimTypes.Role)!.Value;

                var status = await _approvalService.ProcessStepAsync(dto.ModuleCode, dto.ReferenceId, approverId, actorRoleName, dto.IsApproved, dto.Note, ct);

                return Ok(new { Success = true, Message = "Phê duyệt thành công.", FinalStatus = status.ToString() });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return UnprocessableEntity(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }

    public class ProcessApprovalDto
    {
        public required string ModuleCode { get; set; }
        public int ReferenceId { get; set; }
        public bool IsApproved { get; set; }
        public string? Note { get; set; }
    }
}
