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
                    DisplayName = "So phut OT hop le trong ky",
                    SourcePath = "Overtime.ActualOtMinutes",
                    Module = "Cham cong",
                    DataType = SalaryVariableDataType.Hours,
                    AggregationType = SalaryAggregationType.MonthlyTotal,
                    IsPeriodBased = true
                },
                new()
                {
                    DisplayName = "So phut OT ngay thuong trong ky",
                    SourcePath = "Overtime.WeekdayMinutes",
                    Module = "Cham cong",
                    DataType = SalaryVariableDataType.Hours,
                    AggregationType = SalaryAggregationType.MonthlyTotal,
                    IsPeriodBased = true
                },
                new()
                {
                    DisplayName = "So phut OT cuoi tuan trong ky",
                    SourcePath = "Overtime.WeekendMinutes",
                    Module = "Cham cong",
                    DataType = SalaryVariableDataType.Hours,
                    AggregationType = SalaryAggregationType.MonthlyTotal,
                    IsPeriodBased = true
                },
                new()
                {
                    DisplayName = "So phut di muon trong ky",
                    SourcePath = "Attendance.LateMinutes",
                    Module = "Cham cong",
                    DataType = SalaryVariableDataType.Number,
                    AggregationType = SalaryAggregationType.MonthlyTotal,
                    IsPeriodBased = true
                },
                new()
                {
                    DisplayName = "So ngay cong thuc te trong ky",
                    SourcePath = "Attendance.WorkDays",
                    Module = "Cham cong",
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
