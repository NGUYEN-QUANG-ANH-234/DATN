using System.Security.Claims;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.System
{
    [ApiController]
    [Route("api/v1/document-forms")]
    [Authorize]
    public class DocumentFormsController : ControllerBase
    {
        private readonly IDocumentTemplateManagementUseCase _useCase;

        public DocumentFormsController(IDocumentTemplateManagementUseCase useCase)
        {
            _useCase = useCase;
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailable(CancellationToken ct)
        {
            var data = await _useCase.GetAvailableFormsAsync(GetActor(), ct);
            return Ok(new { success = true, data });
        }

        [HttpGet("{templateKey}/prepare")]
        public async Task<IActionResult> Prepare(string templateKey, [FromQuery] int? employeeId, CancellationToken ct)
        {
            try
            {
                var data = await _useCase.PrepareFormAsync(templateKey, employeeId, GetActor(), ct);
                return Ok(new { success = true, data });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{templateKey}/generate")]
        public async Task<IActionResult> Generate(string templateKey, [FromBody] DocumentFormGenerateRequestDto request, CancellationToken ct)
        {
            try
            {
                var data = await _useCase.GenerateFormAsync(templateKey, request, GetActor(), ct);
                return Ok(new { success = true, data });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
        }

        private DocumentActorContextDto GetActor()
        {
            var accountClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return new DocumentActorContextDto
            {
                AccountId = int.TryParse(accountClaim, out var accountId) ? accountId : 0,
                Roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList()
            };
        }
    }
}
