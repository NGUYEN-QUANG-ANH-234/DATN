using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Core.Models.System;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.System
{
    public class SourceCatalogRepository : BaseRepository<SourceCatalog>, ISourceCatalogRepository
    {
        public SourceCatalogRepository(MyDbContext context) : base(context) { }

        public async Task<IEnumerable<SourceCatalog>> GetOrderedCatalogsAsync(IEnumerable<string> sourcePaths, CancellationToken ct = default)
        {
            var paths = sourcePaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return await _dbSet
                .AsNoTracking()
                .Where(x => paths.Contains(x.SourcePath))
                .OrderBy(x => x.Module)
                .ThenByDescending(x => x.IsActive)
                .ThenBy(x => x.DisplayName)
                .ToListAsync(ct);
        }

        public async Task<SourceCatalog?> GetActiveBySourcePathAsync(string sourcePath, CancellationToken ct = default)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SourcePath == sourcePath && x.IsActive, ct);
        }

        public async Task SyncSystemPayrollSourcesAsync(IEnumerable<PayrollSourceDefinition> sources, CancellationToken ct = default)
        {
            var definitions = sources
                .Where(source => !string.IsNullOrWhiteSpace(source.Code))
                .GroupBy(source => source.Code.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            var definitionCodes = definitions
                .Select(source => source.Code.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var existingCatalogs = await _dbSet.ToListAsync(ct);
            var existingByCode = existingCatalogs
                .GroupBy(source => source.SourcePath, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var definition in definitions)
            {
                var sourcePath = definition.Code.Trim();
                if (existingByCode.TryGetValue(sourcePath, out var existing))
                {
                    existing.DisplayName = definition.DisplayName.Trim();
                    existing.Module = definition.Module.Trim();
                    existing.DataType = definition.DataType;
                    existing.AggregationType = definition.AggregationType;
                    existing.IsPeriodBased = definition.IsPeriodBased;
                    continue;
                }

                await _dbSet.AddAsync(new SourceCatalog
                {
                    DisplayName = definition.DisplayName.Trim(),
                    SourcePath = sourcePath,
                    Module = definition.Module.Trim(),
                    DataType = definition.DataType,
                    AggregationType = definition.AggregationType,
                    IsPeriodBased = definition.IsPeriodBased,
                    IsActive = true
                }, ct);
            }

            foreach (var catalog in existingCatalogs)
            {
                if (!definitionCodes.Contains(catalog.SourcePath))
                {
                    catalog.IsActive = false;
                    catalog.Module = "Nguồn cũ";
                    catalog.DisplayName = $"Nguồn cũ - {catalog.SourcePath}";
                }
            }
        }
    }
}
