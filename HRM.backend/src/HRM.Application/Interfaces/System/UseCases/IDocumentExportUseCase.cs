using HRM.backend.src.HRM.Application.DTOs.System;

namespace HRM.backend.src.HRM.Application.Interfaces.System.UseCases
{
    public interface IDocumentExportUseCase
    {
        Task<IEnumerable<DocumentTemplateSummaryDto>> GetAvailableTemplatesAsync(CancellationToken ct = default);
        Task<DocumentExportResultDto> ExportAsync(string templateKey, int referenceId, string? layoutVersion = null, CancellationToken ct = default);
    }
}
