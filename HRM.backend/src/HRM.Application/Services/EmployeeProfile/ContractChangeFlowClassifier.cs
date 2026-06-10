using HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Services;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.Services.EmployeeProfile
{
    public class ContractChangeFlowClassifier : IContractChangeFlowClassifier
    {
        private static readonly IReadOnlyDictionary<ContractChangeType, ContractChangeFlowType> FlowMap =
            new Dictionary<ContractChangeType, ContractChangeFlowType>
            {
                [ContractChangeType.BasicSalaryChange] = ContractChangeFlowType.Addendum,
                [ContractChangeType.InsuranceSalaryChange] = ContractChangeFlowType.Addendum,
                [ContractChangeType.DepartmentChangePermanent] = ContractChangeFlowType.Addendum,
                [ContractChangeType.PositionChangePermanent] = ContractChangeFlowType.Addendum,
                [ContractChangeType.JobLevelChangePermanent] = ContractChangeFlowType.Addendum,
                [ContractChangeType.EmployeeTypeChangePermanent] = ContractChangeFlowType.Addendum,
                [ContractChangeType.ContractEndDateChange] = ContractChangeFlowType.NewContract,
                [ContractChangeType.ContractStartDateChange] = ContractChangeFlowType.NewContract,
                [ContractChangeType.ContractTypeChange] = ContractChangeFlowType.NewContract,
                [ContractChangeType.RenewExpiredContract] = ContractChangeFlowType.NewContract,
                [ContractChangeType.KpiFormulaChange] = ContractChangeFlowType.PolicyUpdate,
                [ContractChangeType.PayrollPolicyChange] = ContractChangeFlowType.PolicyUpdate,
                [ContractChangeType.TemporaryDepartmentAssignment] = ContractChangeFlowType.TemporaryDecision,
                [ContractChangeType.TemporaryPositionAssignment] = ContractChangeFlowType.TemporaryDecision,
                [ContractChangeType.Unknown] = ContractChangeFlowType.NotAllowed
            };

        public ContractChangeFlowType Classify(ContractChangeType changeType)
        {
            return FlowMap.TryGetValue(changeType, out var flowType)
                ? flowType
                : ContractChangeFlowType.NotAllowed;
        }

        public ContractChangeFlowType Classify(string? changeCode)
        {
            if (string.IsNullOrWhiteSpace(changeCode))
                return ContractChangeFlowType.NotAllowed;

            return Enum.TryParse<ContractChangeType>(changeCode.Trim(), true, out var changeType)
                ? Classify(changeType)
                : ContractChangeFlowType.NotAllowed;
        }

        public PersonnelChangeContractFlowType ToPersonnelChangeContractFlowType(ContractChangeFlowType flowType)
        {
            return flowType switch
            {
                ContractChangeFlowType.Addendum => PersonnelChangeContractFlowType.ContractAddendum,
                ContractChangeFlowType.NewContract => PersonnelChangeContractFlowType.NewContract,
                _ => PersonnelChangeContractFlowType.None
            };
        }

        public void EnsureAllowedForAddendum(IEnumerable<ContractChangeType> changeTypes)
        {
            var invalid = changeTypes
                .Where(changeType => Classify(changeType) != ContractChangeFlowType.Addendum)
                .ToList();

            if (invalid.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Nhung thay doi sau khong duoc xu ly bang phu luc: {string.Join(", ", invalid)}.");
            }
        }
    }
}
