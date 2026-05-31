using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;
using HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRM.backend.src.HRM.API.Controllers.EmployeeProfile
{
    [ApiController]
    [Route("api/v1/contracts")]
    [Authorize]
    public class ContractController : ControllerBase
    {
        private readonly IContractUseCase _useCase;

        public ContractController(IContractUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpPost("requests")]
        public async Task<IActionResult> CreateRequest([FromBody] ContractRequestDto dto, CancellationToken ct)
        {
            var id = await _useCase.CreateRequestAsync(GetAccountId(), dto, ct, GetIdempotencyKey());
            return Created($"/api/v1/contracts/{id}", new { Success = true, Data = id, Message = "Đã gửi yêu cầu ký kết/gia hạn hợp đồng." });
        }

        [HttpPatch("requests/{id}/dept-review")]
        public async Task<IActionResult> DeptReview(int id, [FromBody] ReviewContractDto dto, CancellationToken ct)
        {
            await _useCase.DeptReviewAsync(id, GetAccountId(), GetRole(), dto, ct);
            return Ok(new { Success = true, Message = dto.IsApproved ? "Đã chuyển yêu cầu sang HR." : "Đã từ chối yêu cầu hợp đồng." });
        }

        [HttpPatch("requests/{id}/dept-confirm")]
        public async Task<IActionResult> DeptConfirm(int id, CancellationToken ct)
        {
            await _useCase.DeptReviewAsync(id, GetAccountId(), GetRole(), new ReviewContractDto { IsApproved = true }, ct);
            return Ok(new { Success = true, Message = "Đã chuyển yêu cầu sang HR." });
        }

        [HttpPatch("requests/{id}/dept-reject")]
        public async Task<IActionResult> DeptReject(int id, [FromBody] ReviewContractDto dto, CancellationToken ct)
        {
            dto.IsApproved = false;
            await _useCase.DeptReviewAsync(id, GetAccountId(), GetRole(), dto, ct);
            return Ok(new { Success = true, Message = "Đã từ chối yêu cầu hợp đồng." });
        }

        [HttpPost("requests/{id}/hr-draft")]
        public async Task<IActionResult> HrCreateDraft(int id, [FromBody] CreateDraftDto dto, CancellationToken ct)
        {
            await _useCase.HrCreateDraftAsync(id, GetAccountId(), GetRole(), dto, ct);
            return Ok(new { Success = true, Message = "Bản nháp hợp đồng đã sẵn sàng." });
        }

        [HttpPatch("{id}/update-draft")]
        public async Task<IActionResult> UpdateDraft(int id, [FromBody] CreateDraftDto dto, CancellationToken ct)
        {
            await _useCase.HrCreateDraftAsync(id, GetAccountId(), GetRole(), dto, ct);
            return Ok(new { Success = true, Message = "Bản nháp hợp đồng đã được cập nhật." });
        }

        [HttpPatch("requests/{id}/hr-reject")]
        public async Task<IActionResult> HrReject(int id, [FromBody] ReviewContractDto dto, CancellationToken ct)
        {
            await _useCase.HrRejectAsync(id, GetAccountId(), GetRole(), dto.RejectReason ?? "Không đáp ứng chính sách.", ct);
            return Ok(new { Success = true, Message = "Đã từ chối hợp đồng." });
        }

        [HttpPut("{id}/negotiate")]
        public async Task<IActionResult> Negotiate(int id, [FromBody] NegotiateDto dto, CancellationToken ct)
        {
            await _useCase.NegotiateAsync(id, GetAccountId(), dto, ct);
            return Ok(new { Success = true, Message = "Da chuyen y kien thuong luong toi HR." });
        }

        [HttpPatch("{id}/emp-accept")]
        public async Task<IActionResult> EmployeeAccept(int id, CancellationToken ct)
        {
            await _useCase.EmployeeAcceptAsync(id, GetAccountId(), ct);
            return Ok(new { Success = true, Message = "Da xac nhan dieu khoan, cho Giám đốc duyet." });
        }

        [HttpPatch("{id}/director-approve")]
        public async Task<IActionResult> DirectorApprove(int id, [FromBody] ReviewContractDto dto, CancellationToken ct)
        {
            await _useCase.DirectorReviewAsync(id, GetAccountId(), GetRole(), dto, ct);
            return Ok(new { Success = true, Message = dto.IsApproved ? "Hợp đồng chính thức có hiệu lực." : "Giám đốc đã từ chối hợp đồng." });
        }

        [HttpGet("my-contracts")]
        public async Task<IActionResult> GetMyContracts(CancellationToken ct)
        {
            var result = await _useCase.GetMyContractsAsync(GetAccountId(), ct);
            return Ok(new { Success = true, Data = result });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllContracts(CancellationToken ct)
        {
            var result = await _useCase.GetAllContractsAsync(ct);
            return Ok(new { Success = true, Data = result });
        }

        [HttpGet("pending-dept")]
        public async Task<IActionResult> GetPendingDept(CancellationToken ct)
        {
            var result = await _useCase.GetPendingDeptAsync(ct);
            return Ok(new { Success = true, Data = result });
        }

        [HttpGet("pending-hr")]
        public async Task<IActionResult> GetPendingHR(CancellationToken ct)
        {
            var result = await _useCase.GetPendingHRAsync(ct);
            return Ok(new { Success = true, Data = result });
        }

        [HttpGet("pending-director")]
        public async Task<IActionResult> GetPendingDirector(CancellationToken ct)
        {
            var result = await _useCase.GetPendingDirectorAsync(ct);
            return Ok(new { Success = true, Data = result });
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
