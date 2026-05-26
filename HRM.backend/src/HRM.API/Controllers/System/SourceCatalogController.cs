using HRM.backend.src.HRM.API.Middlewares;
using HRM.backend.src.HRM.Application.DTOs;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.System
{
    [ApiController]
    [Route("api/v1/system")]
    [Authorize(Roles = "Admin")]
    public class SourceCatalogController : ControllerBase
    {
        private readonly ISourceCatalogUseCase _useCase;

        public SourceCatalogController(ISourceCatalogUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet("source-catalogs")]
        [RequirePermission("SOURCE_CATALOG_VIEW", GroupName = SystemModules.Config, Description = "Xem danh mục nguồn dữ liệu hệ thống")]
        public async Task<IActionResult> GetAllSourceCatalogs(CancellationToken ct)
        {
            var catalogs = await _useCase.GetAllSourceCatalogsAsync(ct);
            return Ok(new { success = true, data = catalogs });
        }

        [HttpPost("source-catalogs")]
        [RequirePermission("SOURCE_CATALOG_CREATE", GroupName = SystemModules.Config, Description = "Thêm nguồn dữ liệu cho biến lương")]
        public async Task<IActionResult> CreateSourceCatalog([FromBody] CreateSourceCatalogDto dto, CancellationToken ct)
        {
            try
            {
                var catalog = await _useCase.CreateSourceCatalogAsync(dto, ct);
                return StatusCode(201, new { success = true, data = catalog });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
