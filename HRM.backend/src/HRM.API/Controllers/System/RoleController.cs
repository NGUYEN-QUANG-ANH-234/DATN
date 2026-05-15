using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.System
{
    [ApiController]
    [Route("api/v1/system/roles")]
    [Authorize]
    public class RoleController : ControllerBase
    {
        private readonly IRbacUseCase _rbacUseCase;

        public RoleController(IRbacUseCase rbacUseCase)
        {
            _rbacUseCase = rbacUseCase;
        }

        [HttpGet]
        public async Task<IActionResult> GetSystemRoles(CancellationToken ct)
        {
            try
            {
                var roles = await _rbacUseCase.GetSystemRolesAsync(ct);
                return Ok(new { success = true, data = roles });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi truy xuất hệ thống quyền: " + ex.Message });
            }
        }
    }
}
