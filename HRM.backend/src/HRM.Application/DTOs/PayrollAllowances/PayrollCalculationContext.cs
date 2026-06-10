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

    public class PayrollLegalPolicySet
    {
        public byte Month { get; set; }
        public short Year { get; set; }
        public string Period { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        public required TaxConfig TaxConfig { get; set; }
        public List<PITTaxBracket> PitBrackets { get; set; } = new();
        public required InsuranceConfig InsuranceConfig { get; set; }
        public List<OvertimeRateConfig> OvertimeRateConfigs { get; set; } = new();
        public List<PayrollPolicy> AllowanceTaxPolicies { get; set; } = new();
        public List<PayrollPolicy> SeniorityPolicies { get; set; } = new();
        public List<PayrollPolicy> MinimumWagePolicies { get; set; } = new();
        public List<WorkCalendarConfig> WorkCalendars { get; set; } = new();
    }

    public class PayrollFeatureToggleDto
    {
        public bool EnableInsurance { get; set; } = true;
        public bool EnableOvertime { get; set; } = true;
        public bool EnableMealAllowance { get; set; } = true;
        public bool EnableExternalTimesheetPay { get; set; } = true;

        public static PayrollFeatureToggleDto Default() => new();
    }

    public class PayrollPreflightDto
    {
        public byte Month { get; set; }
        public short Year { get; set; }
        public string Period { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public bool CanCalculate { get; set; }
        public PayrollFeatureToggleDto FeatureToggles { get; set; } = PayrollFeatureToggleDto.Default();
        public List<PayrollPreflightPolicyDto> Policies { get; set; } = new();
        public List<PayrollDependencyImpactDto> DependencyImpacts { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class PayrollPreflightPolicyDto
    {
        public string Area { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Version { get; set; }
        public string? VersionCode { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsApplied { get; set; } = true;
        public string? Note { get; set; }
    }

    public class PayrollDependencyImpactDto
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public List<string> Impacts { get; set; } = new();
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
        public List<ProjectBonusImportLine> ProjectBonusLines { get; set; } = new();
        public List<EmploymentServicePeriod> EmploymentServicePeriods { get; set; } = new();
        public List<PayrollPolicy> AllowanceTaxPolicies { get; set; } = new();
        public List<PayrollPolicy> SeniorityPolicies { get; set; } = new();
        public PayrollFeatureToggleDto FeatureToggles { get; set; } = PayrollFeatureToggleDto.Default();

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
