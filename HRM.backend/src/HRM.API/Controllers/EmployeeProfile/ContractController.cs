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
            int accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _useCase.CreateRequestAsync(accountId, dto, ct);
            return Created("", new { Success = true, Message = "Đã gửi yêu cầu ký kết/gia hạn hợp đồng." });
        }

        [HttpPatch("requests/{id}/dept-review")]
        public async Task<IActionResult> DeptReview(int id, [FromBody] ReviewContractDto dto, CancellationToken ct)
        {
            await _useCase.DeptReviewAsync(id, dto, ct);
            return Ok(new
            {
                Success = true,
                Message = dto.IsApproved
                    ? "Đã chuyển yêu cầu sang bộ phận HR."
                    : "Đã từ chối yêu cầu hợp đồng."
            });
        }

        [HttpPatch("requests/{id}/dept-confirm")]
        public async Task<IActionResult> DeptConfirm(int id, CancellationToken ct)
        {
            await _useCase.DeptReviewAsync(id, new ReviewContractDto { IsApproved = true }, ct);
            return Ok(new { Success = true, Message = "Đã chuyển yêu cầu sang bộ phận HR." });
        }

        [HttpPatch("requests/{id}/dept-reject")]
        public async Task<IActionResult> DeptReject(int id, [FromBody] ReviewContractDto dto, CancellationToken ct)
        {
            dto.IsApproved = false;
            await _useCase.DeptReviewAsync(id, dto, ct);
            return Ok(new { Success = true, Message = "Đã từ chối yêu cầu hợp đồng." });
        }

        [HttpPost("requests/{id}/hr-draft")]
        public async Task<IActionResult> HrCreateDraft(int id, [FromBody] CreateDraftDto dto, CancellationToken ct)
        {
            await _useCase.HrCreateDraftAsync(id, dto, ct);
            return Ok(new { Success = true, Message = "Bản nháp hợp đồng đã sẵn sàng, chờ nhân viên phản hồi." });
        }

        [HttpPatch("{id}/update-draft")]
        public async Task<IActionResult> UpdateDraft(int id, [FromBody] CreateDraftDto dto, CancellationToken ct)
        {
            await _useCase.HrCreateDraftAsync(id, dto, ct);
            return Ok(new { Success = true, Message = "Bản nháp hợp đồng đã được cập nhật." });
        }

        [HttpPatch("requests/{id}/hr-reject")]
        public async Task<IActionResult> HrReject(int id, [FromBody] ReviewContractDto dto, CancellationToken ct)
        {
            await _useCase.HrRejectAsync(id, dto.RejectReason ?? "Không đáp ứng chính sách.", ct);
            return Ok(new { Success = true, Message = "Đã từ chối hợp đồng." });
        }

        [HttpPut("{id}/negotiate")]
        public async Task<IActionResult> Negotiate(int id, [FromBody] NegotiateDto dto, CancellationToken ct)
        {
            await _useCase.NegotiateAsync(id, dto, ct);
            return Ok(new { Success = true, Message = "Đã chuyển ý kiến thương lượng tới HR." });
        }

        [HttpPatch("{id}/emp-accept")]
        public async Task<IActionResult> EmployeeAccept(int id, CancellationToken ct)
        {
            await _useCase.EmployeeAcceptAsync(id, ct);
            return Ok(new { Success = true, Message = "Đã xác nhận điều khoản, chờ Giám đốc duyệt." });
        }

        [HttpPatch("{id}/director-approve")]
        public async Task<IActionResult> DirectorApprove(int id, [FromBody] ReviewContractDto dto, CancellationToken ct)
        {
            await _useCase.DirectorReviewAsync(id, dto, ct);
            return Ok(new
            {
                Success = true,
                Message = dto.IsApproved
                    ? "Hợp đồng chính thức có hiệu lực."
                    : "Giám đốc đã từ chối phê duyệt hợp đồng."
            });
        }

        [HttpGet("my-contracts")]
        public async Task<IActionResult> GetMyContracts(CancellationToken ct)
        {
            int accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _useCase.GetMyContractsAsync(accountId, ct);
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
    }
}
