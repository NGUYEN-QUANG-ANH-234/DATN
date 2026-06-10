using HRM.backend.src.HRM.Core.Models.System;

namespace HRM.backend.src.HRM.Application.Interfaces.System.Services
{
    public interface ISlaProcessRegistry
    {
        IReadOnlyCollection<SlaProcessDefinition> GetProcesses();
        IReadOnlyCollection<SlaProcessAlias> GetAliases();
        SlaProcessDefinition? FindByCode(string code);
        string? ResolveCanonicalCode(string code);
    }
}
