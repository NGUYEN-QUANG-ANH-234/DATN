using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;
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
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Module)
                .ThenBy(x => x.DisplayName)
                .ToListAsync(ct);
        }

        public async Task<SourceCatalog?> GetActiveBySourcePathAsync(string sourcePath, CancellationToken ct = default)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SourcePath == sourcePath && x.IsActive, ct);
        }

        public async Task EnsureDefaultPayrollCatalogsAsync(CancellationToken ct = default)
        {
            var defaults = new List<SourceCatalog>
            {
                new()
                {
                    DisplayName = "Lương cơ bản theo hợp đồng",
                    SourcePath = "Contract.BasicSalary",
                    Module = "Hợp đồng",
                    DataType = SalaryVariableDataType.Money,
                    AggregationType = SalaryAggregationType.Latest,
                    IsPeriodBased = false
                },
                new()
                {
                    DisplayName = "Số phút OT hợp lệ trong kỳ",
                    SourcePath = "Overtime.ActualOtMinutes",
                    Module = "Chấm công",
                    DataType = SalaryVariableDataType.Hours,
                    AggregationType = SalaryAggregationType.MonthlyTotal,
                    IsPeriodBased = true
                },
                new()
                {
                    DisplayName = "Số phút đi muộn trong kỳ",
                    SourcePath = "Attendance.LateMinutes",
                    Module = "Chấm công",
                    DataType = SalaryVariableDataType.Number,
                    AggregationType = SalaryAggregationType.MonthlyTotal,
                    IsPeriodBased = true
                },
                new()
                {
                    DisplayName = "Số ngày công thực tế trong kỳ",
                    SourcePath = "Attendance.WorkDays",
                    Module = "Chấm công",
                    DataType = SalaryVariableDataType.Days,
                    AggregationType = SalaryAggregationType.MonthlyTotal,
                    IsPeriodBased = true
                }
            };

            var defaultPaths = defaults.Select(x => x.SourcePath).ToList();
            var existingPaths = await _dbSet
                .Where(x => defaultPaths.Contains(x.SourcePath))
                .Select(x => x.SourcePath)
                .ToListAsync(ct);

            var missing = defaults.Where(x => !existingPaths.Contains(x.SourcePath)).ToList();
            if (missing.Count > 0)
                await _dbSet.AddRangeAsync(missing, ct);
        }
    }
}
