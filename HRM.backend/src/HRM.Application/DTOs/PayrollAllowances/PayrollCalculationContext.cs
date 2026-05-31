using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.DTOs.PayrollAllowances
{
    public class PayrollSourceBatch
    {
        public byte Month { get; set; }
        public short Year { get; set; }
        public string Period { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<PayrollCalculationSource> Sources { get; set; } = new();
    }

    public class PayrollCalculationSource
    {
        public required AttendanceSummary Attendance { get; set; }
        public required Employee Employee { get; set; }
        public required Contract Contract { get; set; }
        public required PayrollFormula Formula { get; set; }
        public required TaxConfig TaxConfig { get; set; }
        public required InsuranceConfig InsuranceConfig { get; set; }

        public PositionJobLevelPolicy? PositionJobLevelPolicy { get; set; }
        public PerformanceReview? PerformanceReview { get; set; }
        public List<EmployeeAllowance> LegacyAllowances { get; set; } = new();
        public List<EmployeeSalaryComponent> SalaryComponents { get; set; } = new();
        public List<PITTaxBracket> PitBrackets { get; set; } = new();
        public List<OvertimeSegment> OvertimeSegments { get; set; } = new();
        public List<AttendanceDailySummary> DailySummaries { get; set; } = new();
        public MonthlyInsuranceStatus? MonthlyInsuranceStatus { get; set; }
        public List<PayrollAdjustment> Adjustments { get; set; } = new();
        public List<PayrollContractSegment> ContractSegments { get; set; } = new();
        public List<OvertimeRateConfig> OvertimeRateConfigs { get; set; } = new();
        public List<ExternalTimesheetLine> ExternalTimesheetLines { get; set; } = new();
        public List<EmploymentServicePeriod> EmploymentServicePeriods { get; set; } = new();
        public List<PayrollPolicy> SeniorityPolicies { get; set; } = new();

        public int DependentCount { get; set; }
        public TaxMethod TaxMethod { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string Period { get; set; } = string.Empty;

        public Dictionary<string, decimal> Variables { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, object?> Snapshot { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class PayrollLineResult
    {
        public required PayrollFormulaLine FormulaLine { get; set; }
        public string ComponentCode { get; set; } = string.Empty;
        public string ComponentName { get; set; } = string.Empty;
        public string? ComponentGroup { get; set; }
        public decimal RawAmount { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal InsuranceBaseAmount { get; set; }
        public bool IsIncome { get; set; }
        public bool IsDeduction { get; set; }
        public bool IsTaxable { get; set; }
        public bool IsInsuranceBased { get; set; }
        public string? ProrationType { get; set; }
        public string? CalculationMethod { get; set; }
        public string? Note { get; set; }
        public Dictionary<string, decimal> VariablesAfterLine { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class PayrollCalculationOutput
    {
        public List<PayrollLineResult> Lines { get; set; } = new();
        public Dictionary<string, decimal> FinalVariables { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public decimal BaseSalaryActual { get; set; }
        public decimal GrossIncome { get; set; }
        public decimal TotalAllowance { get; set; }
        public decimal TotalBonus { get; set; }
        public decimal InsuranceSalary { get; set; }
        public decimal EmployeeInsuranceAmount { get; set; }
        public decimal EmployerContributionAmount { get; set; }
        public decimal TaxableGrossIncome { get; set; }
        public decimal TaxableIncome { get; set; }
        public decimal PitAmount { get; set; }
        public decimal OtherDeductions { get; set; }
        public decimal NetSalary { get; set; }
        public decimal TotalCompanyCost { get; set; }
    }
}
