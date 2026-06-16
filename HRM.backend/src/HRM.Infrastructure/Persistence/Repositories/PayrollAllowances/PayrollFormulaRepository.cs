using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PayrollAllowances;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.PayrollAllowances
{
    public class PayrollFormulaRepository : BaseRepository<PayrollFormula>, IPayrollFormulaRepository
    {
        public PayrollFormulaRepository(MyDbContext context) : base(context) { }

        public async Task<List<PayrollFormula>> GetListAsync(FormulaStatus? status, CancellationToken ct = default)
        {
            var query = BuildQuery().AsNoTracking();
            if (status.HasValue)
                query = query.Where(f => f.Status == status.Value);

            return await query
                .OrderByDescending(f => f.IsActive)
                .ThenBy(f => f.FormulaCode)
                .ThenByDescending(f => f.Version)
                .ToListAsync(ct);
        }

        public async Task<PayrollFormula?> GetDetailAsync(int id, CancellationToken ct = default)
        {
            return await BuildQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id, ct);
        }

        public async Task<PayrollFormula?> GetTrackedDetailAsync(int id, CancellationToken ct = default)
        {
            return await BuildQuery()
                .FirstOrDefaultAsync(f => f.Id == id, ct);
        }

        public async Task<int> GetNextVersionAsync(string formulaCode, CancellationToken ct = default)
        {
            var normalizedCode = formulaCode.Trim();
            var maxVersion = await _dbSet
                .Where(f => f.FormulaCode == normalizedCode)
                .Select(f => (int?)f.Version)
                .MaxAsync(ct);
            return (maxVersion ?? 0) + 1;
        }

        public async Task<List<PayrollFormula>> GetOverlappingActiveAsync(PayrollFormula formula, CancellationToken ct = default)
        {
            var effectiveTo = formula.EffectiveTo ?? DateTime.MaxValue;

            return await _dbSet
                .Where(f => f.Id != formula.Id &&
                            f.IsActive &&
                            (f.Status == FormulaStatus.Active || f.Status == FormulaStatus.Approved) &&
                            f.FormulaCode == formula.FormulaCode &&
                            f.ContractType == formula.ContractType &&
                            f.PayBasis == formula.PayBasis &&
                            f.EmployeeType == formula.EmployeeType &&
                            f.DeptId == formula.DeptId &&
                            f.PositionId == formula.PositionId &&
                            f.JobLevelId == formula.JobLevelId &&
                            f.EffectiveFrom <= effectiveTo &&
                            ((f.EffectiveTo ?? DateTime.MaxValue) >= formula.EffectiveFrom))
                .OrderByDescending(f => f.EffectiveFrom)
                .ToListAsync(ct);
        }

        private IQueryable<PayrollFormula> BuildQuery()
        {
            return _dbSet
                .Include(f => f.Lines.OrderBy(l => l.CalculationOrder).ThenBy(l => l.Id))
                    .ThenInclude(l => l.SalaryComponentType);
        }
    }
}
