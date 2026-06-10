using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Models.System;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.System
{
    public interface ISourceCatalogRepository : IBaseRepository<SourceCatalog>
    {
        Task<IEnumerable<SourceCatalog>> GetOrderedCatalogsAsync(IEnumerable<string> sourcePaths, CancellationToken ct = default);
        Task<SourceCatalog?> GetActiveBySourcePathAsync(string sourcePath, CancellationToken ct = default);
        Task SyncSystemPayrollSourcesAsync(IEnumerable<PayrollSourceDefinition> sources, CancellationToken ct = default);
    }
}
