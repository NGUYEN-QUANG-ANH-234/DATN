using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.PayrollAllowances
{
    public interface IPayrollFormulaRepository : IBaseRepository<PayrollFormula>
    {
        Task<List<PayrollFormula>> GetListAsync(FormulaStatus? status, CancellationToken ct = default);
        Task<PayrollFormula?> GetDetailAsync(int id, CancellationToken ct = default);
        Task<PayrollFormula?> GetTrackedDetailAsync(int id, CancellationToken ct = default);
        Task<int> GetNextVersionAsync(string formulaCode, CancellationToken ct = default);
        Task<List<PayrollFormula>> GetOverlappingActiveAsync(PayrollFormula formula, CancellationToken ct = default);
    }
}
