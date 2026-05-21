using HRM.backend.src.HRM.Application.Interfaces.Recruitment.Usecases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.Recruitment
{
    [ApiController]
    [Route("api/v1/positions")]
    [Authorize] // Đảm bảo chỉ người dùng nội bộ đã đăng nhập mới gọi được
    public class PositionController : ControllerBase
    {
        private readonly IPositionUseCase _positionUseCase;

        public PositionController(IPositionUseCase positionUseCase)
        {
            _positionUseCase = positionUseCase;
        }

        [HttpGet]
        public async Task<IActionResult> GetActivePositions(CancellationToken ct)
        {
            try
            {
                var positions = await _positionUseCase.GetActivePositionsAsync(ct);
                return Ok(new { Success = true, Data = positions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi truy xuất hệ thống: " + ex.Message });
            }
        }
    }
}
