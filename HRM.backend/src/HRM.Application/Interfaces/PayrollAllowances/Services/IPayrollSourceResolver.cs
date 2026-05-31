using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;

namespace HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Services
{
    public interface IPayrollSourceResolver
    {
        Task<PayrollSourceBatch> ResolveAsync(PayrollPeriodDto period, CancellationToken ct = default);
    }
}
