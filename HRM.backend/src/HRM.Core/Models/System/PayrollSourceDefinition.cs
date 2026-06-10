using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Models.System
{
    public class PayrollSourceDefinition
    {
        public required string Code { get; set; }
        public required string DisplayName { get; set; }
        public required string Module { get; set; }
        public SalaryVariableDataType DataType { get; set; } = SalaryVariableDataType.Number;
        public SalaryAggregationType AggregationType { get; set; } = SalaryAggregationType.Latest;
        public bool IsPeriodBased { get; set; }
    }
}
