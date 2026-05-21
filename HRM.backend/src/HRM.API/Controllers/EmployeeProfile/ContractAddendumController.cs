using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;
using HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            var result = await _useCase.CreateDraftAsync(contractId, dto, ct);
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

        [HttpGet("addendums/pending-director")]
        public async Task<IActionResult> GetPendingDirector(CancellationToken ct)
        {
            var result = await _useCase.GetPendingDirectorAsync(ct);
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
            return Ok(new { Success = true, Message = "Phụ lục đã được gửi Giám đốc phê duyệt." });
        }

        [HttpPatch("addendums/{id}/approve")]
        public async Task<IActionResult> Approve(int id, CancellationToken ct)
        {
            await _useCase.ApproveAsync(id, ct);
            return Ok(new { Success = true, Message = "Phụ lục hợp đồng đã có hiệu lực." });
        }

        [HttpPatch("addendums/{id}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] ReviewContractAddendumDto dto, CancellationToken ct)
        {
            await _useCase.RejectAsync(id, dto.RejectReason, ct);
            return Ok(new { Success = true, Message = "Phụ lục hợp đồng đã bị từ chối." });
        }
    }
}
