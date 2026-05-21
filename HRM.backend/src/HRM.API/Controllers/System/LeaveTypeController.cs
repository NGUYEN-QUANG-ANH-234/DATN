using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.System
{
    [ApiController]
    [Route("api/v1/system/leave-types")] // Khớp 100% với URL 404 từ Client
    [Authorize]
    public class LeaveTypeController : ControllerBase
    {
        private readonly ILeaveTypeUseCase _useCase;

        public LeaveTypeController(ILeaveTypeUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var data = await _useCase.GetLeaveTypesForSelectAsync(ct);
            return Ok(new { Success = true, Data = data });
        }
    }
}
