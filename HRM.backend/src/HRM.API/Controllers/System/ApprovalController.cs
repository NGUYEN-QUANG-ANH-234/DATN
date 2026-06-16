using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Core.Enums;
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
                int approverId = User.GetAccountIdOrThrow();
                string actorRoleName = GetRole();

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
                int approverId = User.GetAccountIdOrThrow();
                string actorRoleName = GetRole();
                var action = string.IsNullOrWhiteSpace(dto.Action)
                    ? (dto.IsApproved ? "approve" : "reject")
                    : dto.Action.Trim().ToLowerInvariant();

                if (action is not ("approve" or "reject" or "revision" or "requestrevision" or "needmoreinfo"))
                    return BadRequest(new { Success = false, Message = "Hành động phê duyệt không hợp lệ." });

                var workflowAction = action switch
                {
                    "approve" => ApprovalWorkflowAction.Approve,
                    "reject" => ApprovalWorkflowAction.Reject,
                    _ => ApprovalWorkflowAction.RequestRevision
                };

                var note = workflowAction == ApprovalWorkflowAction.RequestRevision && !string.IsNullOrWhiteSpace(dto.Note)
                    ? "Yêu cầu bổ sung: " + dto.Note
                    : dto.Note;

                var status = await _approvalService.ProcessStepAsync(
                    dto.ModuleCode,
                    dto.ReferenceId,
                    approverId,
                    actorRoleName,
                    workflowAction,
                    note,
                    ct);

                var message = workflowAction == ApprovalWorkflowAction.Approve
                    ? "Đã duyệt yêu cầu."
                    : workflowAction == ApprovalWorkflowAction.RequestRevision
                        ? "Đã gửi yêu cầu bổ sung."
                        : "Đã từ chối yêu cầu.";

                return Ok(new { Success = true, Message = message, FinalStatus = status.ToString() });
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

        private string GetRole()
        {
            return User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }
    }

    public class ProcessApprovalDto
    {
        public required string ModuleCode { get; set; }
        public int ReferenceId { get; set; }
        public bool IsApproved { get; set; }
        public string? Action { get; set; }
        public string? Note { get; set; }
    }
}
