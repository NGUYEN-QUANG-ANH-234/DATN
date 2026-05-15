using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.System
{
    public class SourceCatalogRepository : BaseRepository<SourceCatalog>, ISourceCatalogRepository
    {
        public SourceCatalogRepository(MyDbContext context) : base(context) { }
        public async Task<IEnumerable<SourceCatalog>> GetOrderedCatalogsAsync(CancellationToken ct = default)
        {
            return await _dbSet
                .AsNoTracking() // Tối ưu hiệu năng đọc
                .OrderBy(x => x.Module)
                .ThenBy(x => x.DisplayName)
                .ToListAsync(ct);
        }
    }
}
