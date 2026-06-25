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
    [Route("api/v1/system")]
    [Authorize(Roles = "Admin,HR")]
    public class SourceCatalogController : ControllerBase
    {
        private readonly ISourceCatalogUseCase _useCase;

        public SourceCatalogController(ISourceCatalogUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet("source-catalogs")]
        [RequirePermission("SOURCE_CATALOG_VIEW", GroupName = SystemModules.Config, Description = "View system payroll source catalogs")]
        public async Task<IActionResult> GetAllSourceCatalogs(CancellationToken ct)
        {
            var catalogs = await _useCase.GetAllSourceCatalogsAsync(ct);
            return Ok(new { success = true, data = catalogs });
        }

        [HttpPatch("source-catalogs/{id}/active")]
        [RequirePermission("SOURCE_CATALOG_UPDATE", GroupName = SystemModules.Config, Description = "Bật/tắt nguồn dữ liệu hệ thống")]
        public async Task<IActionResult> SetSourceCatalogActive(int id, [FromBody] SourceCatalogStatusDto dto, CancellationToken ct)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim, out var actorId))
                {
                    return Unauthorized(new { success = false, message = "Không xác định được danh tính người dùng." });
                }

                var catalog = await _useCase.SetSourceCatalogActiveAsync(id, dto.IsActive, actorId, ct);
                return Ok(new { success = true, message = "Đã cập nhật trạng thái nguồn hệ thống.", data = catalog });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("source-catalogs/{id}")]
        [RequirePermission("SOURCE_CATALOG_UPDATE", GroupName = SystemModules.Config, Description = "Xóa hẳn nguồn dữ liệu hệ thống")]
        public async Task<IActionResult> DeleteSourceCatalog(int id, CancellationToken ct)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim, out var actorId))
                {
                    return Unauthorized(new { success = false, message = "Không xác định được danh tính người dùng." });
                }

                await _useCase.DeleteSourceCatalogAsync(id, actorId, ct);
                return Ok(new { success = true, message = "Đã xóa nguồn dữ liệu lương." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
