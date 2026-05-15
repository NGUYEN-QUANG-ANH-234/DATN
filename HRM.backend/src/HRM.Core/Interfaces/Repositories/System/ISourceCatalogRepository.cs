using HRM.backend.src.HRM.Core.Entities.System;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.System
{
    public interface ISourceCatalogRepository : IBaseRepository<SourceCatalog>
    {
        Task<IEnumerable<SourceCatalog>> GetOrderedCatalogsAsync(CancellationToken ct = default);
    }
}
