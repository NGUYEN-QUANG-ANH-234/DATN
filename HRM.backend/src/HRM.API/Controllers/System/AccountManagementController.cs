using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRM.backend.src.HRM.API.Controllers.System
{
    [ApiController]
    [Route("api/v1/accounts")]
    [Authorize]
    [RequirePermission("USER_MANAGE")] // Chỉ Admin/HR có quyền này mới được gọi
    public class AccountManagementController : ControllerBase
    {
        private readonly IAccountManagementUseCase _useCase;

        public AccountManagementController(IAccountManagementUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto dto, CancellationToken ct)
        {
            try
            {
                var newId = await _useCase.CreateAccountAsync(dto, ct);
                return CreatedAtAction(nameof(CreateAccount), new { id = newId }, new { success = true, message = "Khởi tạo tài khoản thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ToggleStatus(int id, [FromBody] ToggleStatusDto dto, CancellationToken ct)
        {
            try
            {
                await _useCase.ToggleAccountStatusAsync(id, dto.Status, ct);
                return Ok(new { success = true, message = "Trạng thái tài khoản đã được cập nhật." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, CancellationToken ct)
        {
            try
            {
                await _useCase.ResetPasswordManuallyAsync(id, ct);
                return Ok(new { success = true, message = "Mật khẩu mới đã được khởi tạo và gửi tới Email." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAccounts(CancellationToken ct)
        {
            var data = await _useCase.GetAllAccountsAsync(ct);
            return Ok(new { success = true, data });
        }

        [HttpPatch("{id}/role")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] int newRoleId, CancellationToken ct)
        {
            try
            {
                // Lấy ID của người đang thực hiện thao tác từ Token
                int actorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                await _useCase.UpdateAccountRoleAsync(id, newRoleId, actorId, ct);
                return Ok(new { success = true, message = "Cập nhật quyền hạn thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
