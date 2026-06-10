using HRM.backend.src.HRM.Core.Models.System;

namespace HRM.backend.src.HRM.Application.Interfaces.System.Services
{
    public interface IPayrollSourceProvider
    {
        IReadOnlyCollection<PayrollSourceDefinition> GetSources();
    }
}
