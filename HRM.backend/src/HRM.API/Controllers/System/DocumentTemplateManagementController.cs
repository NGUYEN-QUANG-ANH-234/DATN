using System.Security.Claims;
using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.System
{
    [ApiController]
    [Route("api/v1/system/document-templates")]
    [Authorize(Roles = "Admin")]
    public class DocumentTemplateManagementController : ControllerBase
    {
        private readonly IDocumentTemplateManagementUseCase _useCase;

        public DocumentTemplateManagementController(IDocumentTemplateManagementUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet]
        [RequirePermission("DOCUMENT_TEMPLATE_VIEW", GroupName = SystemModules.Config, Description = "Xem cấu hình biểu mẫu/đơn từ")]
        public async Task<IActionResult> GetTemplates(CancellationToken ct)
        {
            var data = await _useCase.GetTemplatesAsync(true, ct);
            return Ok(new { success = true, data });
        }

        [HttpGet("{templateKey}")]
        [RequirePermission("DOCUMENT_TEMPLATE_VIEW", GroupName = SystemModules.Config, Description = "Xem chi tiết cấu hình biểu mẫu/đơn từ")]
        public async Task<IActionResult> GetTemplate(string templateKey, CancellationToken ct)
        {
            try
            {
                var data = await _useCase.GetTemplateAsync(templateKey, ct);
                return Ok(new { success = true, data });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{templateKey}")]
        [RequirePermission("DOCUMENT_TEMPLATE_UPDATE", GroupName = SystemModules.Config, Description = "Cập nhật cấu hình biểu mẫu/đơn từ")]
        public async Task<IActionResult> SaveTemplate(string templateKey, [FromBody] DocumentTemplateConfigDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu cấu hình biểu mẫu không hợp lệ.", errors = ModelState });

            try
            {
                dto.TemplateKey = templateKey;
                var actorId = GetCurrentAccountId();
                var data = await _useCase.SaveTemplateAsync(dto, actorId, ct);
                return Ok(new { success = true, message = "Đã lưu cấu hình biểu mẫu.", data });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{templateKey}/validate")]
        [RequirePermission("DOCUMENT_TEMPLATE_UPDATE", GroupName = SystemModules.Config, Description = "Kiểm tra cấu hình biểu mẫu/đơn từ")]
        public async Task<IActionResult> ValidateTemplate(string templateKey, [FromBody] DocumentTemplateConfigDto dto, CancellationToken ct)
        {
            dto.TemplateKey = templateKey;
            var data = await _useCase.ValidateTemplateAsync(dto, ct);
            return Ok(new { success = true, data });
        }

        [HttpPost("{templateKey}/preview")]
        [RequirePermission("DOCUMENT_TEMPLATE_VIEW", GroupName = SystemModules.Config, Description = "Xem trước biểu mẫu/đơn từ")]
        public async Task<IActionResult> PreviewTemplate(string templateKey, [FromBody] DocumentTemplatePreviewRequestDto request, CancellationToken ct)
        {
            try
            {
                request.TemplateConfig.TemplateKey = templateKey;
                var data = await _useCase.PreviewTemplateAsync(request, GetActor(), ct);
                return Ok(new { success = true, data });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("field-catalogs")]
        [RequirePermission("DOCUMENT_TEMPLATE_VIEW", GroupName = SystemModules.Config, Description = "Xem danh mục biến hệ thống cho biểu mẫu/đơn từ")]
        public async Task<IActionResult> GetFieldCatalogs(CancellationToken ct)
        {
            var data = await _useCase.GetFieldCatalogsAsync(ct);
            return Ok(new { success = true, data });
        }

        private int GetCurrentAccountId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var accountId) ? accountId : 0;
        }

        private DocumentActorContextDto GetActor()
        {
            return new DocumentActorContextDto
            {
                AccountId = GetCurrentAccountId(),
                Roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList()
            };
        }
    }
}
