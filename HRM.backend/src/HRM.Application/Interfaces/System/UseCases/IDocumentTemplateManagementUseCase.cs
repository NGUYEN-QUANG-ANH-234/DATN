using HRM.backend.src.HRM.Application.DTOs.System;

namespace HRM.backend.src.HRM.Application.Interfaces.System.UseCases
{
    public interface IDocumentTemplateManagementUseCase
    {
        Task<IReadOnlyCollection<DocumentTemplateConfigDto>> GetTemplatesAsync(bool includeInactive = true, CancellationToken ct = default);
        Task<DocumentTemplateConfigDto> GetTemplateAsync(string templateKey, CancellationToken ct = default);
        Task<IReadOnlyCollection<DocumentFieldCatalogDto>> GetFieldCatalogsAsync(CancellationToken ct = default);
        Task<DocumentTemplateConfigDto> SaveTemplateAsync(DocumentTemplateConfigDto dto, int actorId, CancellationToken ct = default);
        Task<DocumentTemplateValidationResultDto> ValidateTemplateAsync(DocumentTemplateConfigDto dto, CancellationToken ct = default);
        Task<DocumentTemplatePreviewResultDto> PreviewTemplateAsync(DocumentTemplatePreviewRequestDto request, DocumentActorContextDto actor, CancellationToken ct = default);

        Task<IReadOnlyCollection<DocumentFormTemplateSummaryDto>> GetAvailableFormsAsync(DocumentActorContextDto actor, CancellationToken ct = default);
        Task<DocumentFormPrepareResultDto> PrepareFormAsync(string templateKey, int? employeeId, DocumentActorContextDto actor, CancellationToken ct = default);
        Task<DocumentFormGenerateResultDto> GenerateFormAsync(string templateKey, DocumentFormGenerateRequestDto request, DocumentActorContextDto actor, CancellationToken ct = default);
    }
}
