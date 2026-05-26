using HRM.backend.src.HRM.Application.DTOs.System;

namespace HRM.backend.src.HRM.Application.Interfaces.System.UseCases
{
    public interface ISourceCatalogUseCase
    {
        Task<IEnumerable<SourceCatalogDto>> GetAllSourceCatalogsAsync(CancellationToken ct = default);
        Task<SourceCatalogDto> CreateSourceCatalogAsync(CreateSourceCatalogDto dto, CancellationToken ct = default);
    }
}
