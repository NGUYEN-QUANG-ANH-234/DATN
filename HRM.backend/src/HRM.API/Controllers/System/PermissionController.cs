using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRM.backend.src.HRM.API.Controllers.System
{
    [ApiController]
    [Route("api/v1/rbac")]
    [Authorize(Roles = "Admin")] // Bắt buộc là Admin hệ thống
    public class PermissionController : ControllerBase
    {
        private readonly IRbacUseCase _useCase;

        public PermissionController(IRbacUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet("roles")]
        [RequirePermission("RBAC_ROLE_VIEW", GroupName = SystemModules.SystemManagement, Description = "Xem danh sách vai trò và quyền hạn")]
        public async Task<IActionResult> GetRolesAndPermissions(CancellationToken ct)
        {
            var data = await _useCase.GetAllRolesAndPermissionsAsync(ct);
            return Ok(new { success = true, data });
        }

        [HttpPut("permissions")]
        [RequirePermission("RBAC_ROLE_UPDATE", GroupName = SystemModules.SystemManagement, Description = "Cập nhật phân quyền cho vai trò")]
        public async Task<IActionResult> UpdatePermissions([FromBody] UpdateRolePermissionsDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu cấu hình phân quyền không hợp lệ." });

            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim, out int adminId))
                {
                    return Unauthorized(new { success = false, message = "Không xác định được danh tính Admin." });
                }

                var isSuccess = await _useCase.UpdateRolePermissionsAsync(dto, adminId, ct);

                if (isSuccess)
                {
                    return Ok(new { success = true, message = "Cập nhật quyền hạn cho Vai trò thành công." });
                }

                return StatusCode(500, new { success = false, message = "Lỗi hệ thống khi lưu quyền hạn." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Lỗi xử lý Transaction nội bộ." });
            }
        }

        [HttpGet("permissions/all")]
        [RequirePermission("RBAC_PERMISSION_VIEW", GroupName = SystemModules.SystemManagement, Description = "Xem danh sách toàn bộ mã quyền hệ thống")]
        public async Task<IActionResult> GetAllPermissions(CancellationToken ct)
        {
            var data = await _useCase.GetAllAvailablePermissionsAsync(ct);

            // Trả về theo chuẩn BaseResponse chung của hệ thống
            return Ok(new
            {
                success = true,
                message = "Tải danh sách mã quyền thành công.",
                data
            });
        }
    }
}
