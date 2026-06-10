using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Services
{
    public interface IContractChangeFlowClassifier
    {
        ContractChangeFlowType Classify(ContractChangeType changeType);
        ContractChangeFlowType Classify(string? changeCode);
        PersonnelChangeContractFlowType ToPersonnelChangeContractFlowType(ContractChangeFlowType flowType);
        void EnsureAllowedForAddendum(IEnumerable<ContractChangeType> changeTypes);
    }
}
