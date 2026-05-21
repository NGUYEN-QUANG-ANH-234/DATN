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
    [Route("api/v1/system/notification-templates")]
    [Authorize(Roles = "Admin")]
    public class NotificationTemplateController : ControllerBase
    {
        private readonly ITemplateManagementUseCase _useCase;

        public NotificationTemplateController(ITemplateManagementUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet]
        [RequirePermission("NOTIFICATION_TEMPLATE_VIEW", GroupName = SystemModules.Config, Description = "Xem danh sách mẫu thông báo")]
        public async Task<IActionResult> GetTemplates(CancellationToken ct)
        {
            var data = await _useCase.GetTemplatesAsync(ct);
            return Ok(new { success = true, data });
        }

        [HttpPut("{templateKey}")]
        [RequirePermission("NOTIFICATION_TEMPLATE_UPDATE", GroupName = SystemModules.Config, Description = "Cập nhật mẫu thông báo")]
        public async Task<IActionResult> UpdateTemplate(string templateKey, [FromBody] TemplateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu đầu vào không hợp lệ." });

            // Đảm bảo Key trên URL khớp với Body
            dto.TemplateKey = templateKey;

            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim, out int adminId))
                    return Unauthorized(new { success = false, message = "Không xác định được danh tính Admin." });

                var isSuccess = await _useCase.UpdateTemplateAsync(dto, adminId, ct);

                if (isSuccess)
                    return Ok(new { success = true, message = "Cập nhật mẫu thông báo thành công." });

                return StatusCode(500, new { success = false, message = "Lỗi không xác định khi lưu cấu hình." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống nội bộ." });
            }
        }
    }
}
