using System.Text;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.System
{
    [ApiController]
    [Route("api/v1/document-exports")]
    [Authorize]
    public class DocumentExportController : ControllerBase
    {
        private readonly IDocumentExportUseCase _useCase;

        public DocumentExportController(IDocumentExportUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet("templates")]
        public async Task<IActionResult> GetTemplates(CancellationToken ct)
        {
            var data = await _useCase.GetAvailableTemplatesAsync(ct);
            return Ok(new { Success = true, Data = data });
        }

        [HttpGet("{templateKey}/{referenceId:int}")]
        public async Task<IActionResult> Export(string templateKey, int referenceId, [FromQuery] string? layoutVersion, CancellationToken ct)
        {
            try
            {
                var result = await _useCase.ExportAsync(templateKey, referenceId, layoutVersion, ct);
                return File(Encoding.UTF8.GetBytes(result.Content), result.ContentType, result.FileName);
            }
            catch (InvalidOperationException ex)
            {
                return UnprocessableEntity(new { Success = false, Message = ex.Message });
            }
        }
    }
}
