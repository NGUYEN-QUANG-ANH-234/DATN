using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;
using HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRM.backend.src.HRM.API.Controllers.EmployeeProfile
{
    [ApiController]
    [Route("api/v1")]
    [Authorize]
    public class ContractAddendumController : ControllerBase
    {
        private readonly IContractAddendumUseCase _useCase;

        public ContractAddendumController(IContractAddendumUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpPost("contracts/{contractId}/addendums")]
        public async Task<IActionResult> CreateDraft(int contractId, [FromBody] CreateContractAddendumDto dto, CancellationToken ct)
        {
            var result = await _useCase.CreateDraftAsync(contractId, dto, ct, GetIdempotencyKey());
            return Created($"api/v1/addendums/{result.Id}", new
            {
                Success = true,
                Message = "Bản thảo phụ lục hợp đồng đã sẵn sàng.",
                Data = result
            });
        }

        [HttpGet("contracts/{contractId}/addendums")]
        public async Task<IActionResult> GetByContract(int contractId, CancellationToken ct)
        {
            var result = await _useCase.GetByContractAsync(contractId, ct);
            return Ok(new { Success = true, Data = result });
        }

        [HttpGet("addendums")]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var result = await _useCase.GetAllAsync(ct);
            return Ok(new { Success = true, Data = result });
        }

        [HttpGet("addendums/pending-dept")]
        public async Task<IActionResult> GetPendingDept(CancellationToken ct)
        {
            var result = await _useCase.GetPendingDeptAsync(GetAccountId(), GetRole(), ct);
            return Ok(new { Success = true, Data = result });
        }

        [HttpGet("addendums/pending-hr")]
        public async Task<IActionResult> GetPendingHR(CancellationToken ct)
        {
            var result = await _useCase.GetPendingHRAsync(ct);
            return Ok(new { Success = true, Data = result });
        }

        [HttpGet("addendums/pending-director")]
        public async Task<IActionResult> GetPendingDirector(CancellationToken ct)
        {
            var result = await _useCase.GetPendingDirectorAsync(ct);
            return Ok(new { Success = true, Data = result });
        }

        [HttpGet("addendums/my-pending-confirmation")]
        public async Task<IActionResult> GetMyPendingConfirmation(CancellationToken ct)
        {
            var result = await _useCase.GetMyPendingEmployeeAsync(GetAccountId(), ct);
            return Ok(new { Success = true, Data = result });
        }

        [HttpPatch("addendums/{id}")]
        public async Task<IActionResult> UpdateDraft(int id, [FromBody] CreateContractAddendumDto dto, CancellationToken ct)
        {
            var result = await _useCase.UpdateDraftAsync(id, dto, ct);
            return Ok(new
            {
                Success = true,
                Message = "Bản nháp phụ lục hợp đồng đã được cập nhật.",
                Data = result
            });
        }

        [HttpPatch("addendums/{id}/submit")]
        public async Task<IActionResult> Submit(int id, CancellationToken ct)
        {
            await _useCase.SubmitAsync(id, ct);
            return Ok(new { Success = true, Message = "Phụ lục đã được gửi Trưởng phòng xác nhận." });
        }

        [HttpPatch("addendums/{id}/dept-review")]
        public async Task<IActionResult> DeptReview(int id, [FromBody] ReviewContractAddendumDto dto, CancellationToken ct)
        {
            await _useCase.ReviewByDeptAsync(id, GetAccountId(), GetRole(), dto, ct);
            return Ok(new
            {
                Success = true,
                Message = dto.IsApproved
                    ? "Trưởng phòng đã xác nhận phụ lục, chuyển HR kiểm tra chính sách."
                    : "Trưởng phòng đã từ chối phụ lục."
            });
        }

        [HttpPatch("addendums/{id}/hr-confirm")]
        public async Task<IActionResult> HrConfirm(int id, [FromBody] ReviewContractAddendumDto dto, CancellationToken ct)
        {
            await _useCase.ConfirmByHrAsync(id, GetAccountId(), GetRole(), dto, ct);
            return Ok(new
            {
                Success = true,
                Message = dto.IsApproved
                    ? "HR đã xác nhận chính sách, chuyển người lao động xác nhận điều khoản."
                    : "HR đã từ chối phụ lục."
            });
        }

        [HttpPatch("addendums/{id}/employee-confirm")]
        public async Task<IActionResult> EmployeeConfirm(int id, [FromBody] ReviewContractAddendumDto dto, CancellationToken ct)
        {
            await _useCase.EmployeeConfirmAsync(id, GetAccountId(), dto, ct);
            return Ok(new
            {
                Success = true,
                Message = dto.IsApproved
                    ? "Đã xác nhận điều khoản phụ lục, chờ Giám đốc phê duyệt."
                    : "Đã từ chối điều khoản phụ lục."
            });
        }

        [HttpPatch("addendums/{id}/approve")]
        public async Task<IActionResult> Approve(int id, CancellationToken ct)
        {
            await _useCase.ApproveAsync(id, GetAccountId(), GetRole(), ct);
            return Ok(new { Success = true, Message = "Phụ lục hợp đồng đã có hiệu lực." });
        }

        [HttpPatch("addendums/{id}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] ReviewContractAddendumDto dto, CancellationToken ct)
        {
            await _useCase.RejectAsync(id, GetAccountId(), GetRole(), dto.RejectReason, ct);
            return Ok(new { Success = true, Message = "Phụ lục hợp đồng đã bị từ chối." });
        }

        private int GetAccountId()
        {
            return User.GetAccountIdOrThrow();
        }

        private string GetRole()
        {
            return User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }

        private string? GetIdempotencyKey()
        {
            return Request.Headers["Idempotency-Key"].FirstOrDefault();
        }
    }
}
