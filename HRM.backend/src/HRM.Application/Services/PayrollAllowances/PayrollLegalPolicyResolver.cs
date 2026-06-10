using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;
using HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Services;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PayrollAllowances;

namespace HRM.backend.src.HRM.Application.Services.PayrollAllowances
{
    public class PayrollLegalPolicyResolver : IPayrollLegalPolicyResolver
    {
        private static readonly OvertimeType[] RequiredOvertimeTypes =
        {
            OvertimeType.Weekday,
            OvertimeType.Weekend,
            OvertimeType.WeekdayNight,
            OvertimeType.WeekendNight,
            OvertimeType.Holiday,
            OvertimeType.HolidayNight
        };

        private readonly IPayrollRepository _payrollRepo;

        public PayrollLegalPolicyResolver(IPayrollRepository payrollRepo)
        {
            _payrollRepo = payrollRepo;
        }

        public async Task<PayrollLegalPolicySet> ResolvePayrollPoliciesAsync(
            PayrollPeriodDto period,
            PayrollFeatureToggleDto featureToggles,
            CancellationToken ct = default)
        {
            var periodStart = new DateTime(period.Year, period.Month, 1);
            var periodEnd = periodStart.AddMonths(1).AddTicks(-1);
            var periodText = $"{period.Month:00}/{period.Year}";

            var taxConfig = await _payrollRepo.GetActiveTaxConfigAsync(periodEnd, ct)
                ?? throw MissingPolicy("thuế TNCN", periodText);

            var pitBrackets = await _payrollRepo.GetActivePitTaxBracketsAsync(periodEnd, ct);
            if (pitBrackets.Count == 0)
                throw MissingPolicy("bậc thuế TNCN lũy tiến", periodText);

            var insuranceConfig = featureToggles.EnableInsurance
                ? await _payrollRepo.GetActiveInsuranceConfigAsync(periodEnd, ct)
                    ?? throw MissingPolicy("bảo hiểm", periodText)
                : BuildNotAppliedInsuranceConfig(periodEnd);

            var overtimeRateConfigs = featureToggles.EnableOvertime
                ? await _payrollRepo.GetActiveOvertimeRateConfigsAsync(periodEnd, ct)
                : new List<Core.Entities.PayrollAllowances.OvertimeRateConfig>();
            if (featureToggles.EnableOvertime)
            {
                var missingOvertimeTypes = RequiredOvertimeTypes
                    .Where(type => overtimeRateConfigs.All(config => config.OvertimeType != type))
                    .Select(type => type.ToString())
                    .ToList();
                if (missingOvertimeTypes.Count > 0)
                    throw MissingPolicy($"OT ({string.Join(", ", missingOvertimeTypes)})", periodText);
            }

            var allowanceTaxPolicies = await _payrollRepo.GetActivePayrollPoliciesAsync(PayrollPolicyType.Allowance, periodEnd, ct);
            var seniorityPolicies = await _payrollRepo.GetActivePayrollPoliciesAsync(PayrollPolicyType.Seniority, periodEnd, ct);
            var minimumWagePolicies = await _payrollRepo.GetActivePayrollPoliciesAsync(PayrollPolicyType.MinimumWage, periodEnd, ct);
            var calendars = await _payrollRepo.GetWorkCalendarConfigsAsync(period.Month, period.Year, ct);

            return new PayrollLegalPolicySet
            {
                Month = period.Month,
                Year = period.Year,
                Period = periodText,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                TaxConfig = taxConfig,
                PitBrackets = pitBrackets,
                InsuranceConfig = insuranceConfig,
                OvertimeRateConfigs = overtimeRateConfigs,
                AllowanceTaxPolicies = allowanceTaxPolicies,
                SeniorityPolicies = seniorityPolicies,
                MinimumWagePolicies = minimumWagePolicies,
                WorkCalendars = calendars
            };
        }

        private static InvalidOperationException MissingPolicy(string policyName, string period)
        {
            return new InvalidOperationException($"Thiếu cấu hình {policyName} cho kỳ {period}. Vui lòng cấu hình phiên bản chính sách đang áp dụng trước khi tính lương.");
        }

        private static Core.Entities.PayrollAllowances.InsuranceConfig BuildNotAppliedInsuranceConfig(DateTime effectiveDate)
        {
            return new Core.Entities.PayrollAllowances.InsuranceConfig
            {
                Code = "INSURANCE_NOT_APPLIED",
                Name = "Không áp dụng bảo hiểm",
                Version = 0,
                VersionCode = "INSURANCE_NOT_APPLIED",
                Status = PolicyVersionStatus.Active,
                SourceRef = "PAYROLL_FEATURE_TOGGLE",
                EffectiveFrom = effectiveDate.Date,
                IsActive = true
            };
        }
    }
}
