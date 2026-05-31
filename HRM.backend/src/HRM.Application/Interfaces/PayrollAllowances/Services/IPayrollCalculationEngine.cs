using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;

namespace HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Services
{
    public interface IPayrollCalculationEngine
    {
        PayrollCalculationOutput Calculate(PayrollCalculationSource source);
    }
}
