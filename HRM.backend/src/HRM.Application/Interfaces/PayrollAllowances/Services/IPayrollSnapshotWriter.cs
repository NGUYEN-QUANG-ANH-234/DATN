using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;
using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;

namespace HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Services
{
    public interface IPayrollSnapshotWriter
    {
        Payroll CreateSnapshot(PayrollCalculationSource source, PayrollCalculationOutput output, int actorAccountId);
    }
}
