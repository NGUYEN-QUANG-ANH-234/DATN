using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;
using HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Services;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PayrollAllowances;
using System.Text.Json;

namespace HRM.backend.src.HRM.Application.Services.PayrollAllowances
{
    public class PayrollSourceResolver : IPayrollSourceResolver
    {
        private const decimal DefaultStandardWorkdays = 22m;
        private const decimal DefaultStandardHoursPerDay = 8m;

        private readonly IPayrollRepository _payrollRepo;

        public PayrollSourceResolver(IPayrollRepository payrollRepo)
        {
            _payrollRepo = payrollRepo;
        }

        public async Task<PayrollSourceBatch> ResolveAsync(PayrollPeriodDto period, CancellationToken ct = default)
        {
            var periodStart = new DateTime(period.Year, period.Month, 1);
            var periodEndExclusive = periodStart.AddMonths(1);
            var periodEnd = periodEndExclusive.AddTicks(-1);
            var periodText = $"{period.Month:00}/{period.Year}";

            var batch = new PayrollSourceBatch
            {
                Month = period.Month,
                Year = period.Year,
                Period = periodText,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd
            };

            var attendanceInputs = await _payrollRepo.GetAttendanceInputsAsync(period.Month, period.Year, ct);
            var externalTimesheetLines = await _payrollRepo.GetApprovedExternalTimesheetLinesAsync(periodStart, periodEnd, ct);
            var externalEmployeeIds = externalTimesheetLines
                .Where(l => l.CollaboratorEmployeeId.HasValue)
                .Select(l => l.CollaboratorEmployeeId!.Value)
                .Distinct()
                .ToList();

            if (attendanceInputs.Count == 0 && externalEmployeeIds.Count == 0)
            {
                batch.Warnings.Add("Chưa có bảng công tổng hợp cho kỳ lương này.");
                return batch;
            }

            if (attendanceInputs.Count == 0)
                batch.Warnings.Add("Chưa có bảng công tổng hợp; hệ thống chỉ xử lý các dòng timesheet ngoài đã duyệt trong kỳ.");

            var employeeIds = attendanceInputs
                .Select(a => a.EmployeeId)
                .Concat(externalEmployeeIds)
                .Distinct()
                .ToList();
            var contracts = await _payrollRepo.GetActiveContractsAsync(employeeIds, periodStart, periodEnd, ct);
            var contractsByEmployee = contracts
                .Where(c => c.EmployeeId.HasValue)
                .GroupBy(c => c.EmployeeId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(c => c.StartDate).ThenBy(c => c.Version).ToList());
            AppendExternalTimesheetAttendanceInputs(attendanceInputs, externalTimesheetLines, contractsByEmployee, period, batch);
            employeeIds = attendanceInputs.Select(a => a.EmployeeId).Distinct().ToList();

            var dailySummaries = await _payrollRepo.GetApprovedDailySummariesAsync(employeeIds, periodStart, periodEnd, ct);
            var legacyAllowances = await _payrollRepo.GetEmployeeAllowancesAsync(employeeIds, ct);
            var salaryComponents = await _payrollRepo.GetEmployeeSalaryComponentsAsync(employeeIds, periodStart, periodEnd, ct);
            var reviews = await _payrollRepo.GetPerformanceReviewsAsync(employeeIds, periodText, ct);
            var dependentCounts = await _payrollRepo.GetActiveDependentCountsAsync(employeeIds, periodEnd, ct);
            var overtimeSegments = await _payrollRepo.GetOvertimeSegmentsAsync(employeeIds, periodStart, periodEndExclusive, ct);
            var formulas = await _payrollRepo.GetApprovedPayrollFormulasAsync(periodEnd, ct);
            var taxConfig = await _payrollRepo.GetActiveTaxConfigAsync(periodEnd, ct) ?? DefaultTaxConfig();
            var pitBrackets = await _payrollRepo.GetActivePitTaxBracketsAsync(periodEnd, ct);
            if (pitBrackets.Count == 0) pitBrackets = DefaultPitBrackets();
            var insuranceConfig = await _payrollRepo.GetActiveInsuranceConfigAsync(periodEnd, ct) ?? DefaultInsuranceConfig();
            var insuranceStatuses = await _payrollRepo.GetMonthlyInsuranceStatusesAsync(employeeIds, period.Month, period.Year, ct);
            var payrollAdjustments = await _payrollRepo.GetApprovedPayrollAdjustmentsAsync(employeeIds, period.Month, period.Year, ct);
            var overtimeRateConfigs = await _payrollRepo.GetActiveOvertimeRateConfigsAsync(periodEnd, ct);
            var seniorityPolicies = await _payrollRepo.GetActivePayrollPoliciesAsync(PayrollPolicyType.Seniority, periodEnd, ct);
            var servicePeriods = await _payrollRepo.GetEmploymentServicePeriodsAsync(employeeIds, periodEnd, ct);

            var jobLevelIds = attendanceInputs
                .Select(a => ResolveJobLevelId(a.Employee))
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();
            var positionIds = attendanceInputs
                .Where(a => a.Employee?.PositionId != null)
                .Select(a => a.Employee.PositionId!.Value)
                .Distinct()
                .ToList();
            var positionPolicies = await _payrollRepo.GetPositionJobLevelPoliciesAsync(positionIds, jobLevelIds, periodEnd, ct);

            var dailyByEmployee = dailySummaries.GroupBy(s => s.EmployeeId).ToDictionary(g => g.Key, g => g.OrderBy(s => s.WorkDate).ToList());
            var legacyByEmployee = legacyAllowances
                .Where(a => a.EmployeeId.HasValue)
                .GroupBy(a => a.EmployeeId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());
            var componentsByEmployee = salaryComponents.GroupBy(c => c.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());
            var reviewsByEmployee = reviews.GroupBy(r => r.EmployeeId).ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.FinalizedAt ?? r.CreatedAt).First());
            var insuranceByEmployee = insuranceStatuses.GroupBy(s => s.EmployeeId).ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt).First());
            var adjustmentsByEmployee = payrollAdjustments.GroupBy(a => a.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());
            var servicePeriodsByEmployee = servicePeriods.GroupBy(p => p.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());
            var overtimeByEmployee = overtimeSegments
                .Where(s => s.OvertimeRequest != null)
                .GroupBy(s => s.OvertimeRequest.EmployeeId)
                .ToDictionary(g => g.Key, g => g.ToList());
            var externalTimesheetByEmployee = externalTimesheetLines
                .Where(l => l.CollaboratorEmployeeId.HasValue)
                .GroupBy(l => l.CollaboratorEmployeeId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var attendance in attendanceInputs)
            {
                var employee = attendance.Employee;
                if (employee == null)
                {
                    batch.Warnings.Add($"Nhân viên Id {attendance.EmployeeId} không có hồ sơ hợp lệ.");
                    continue;
                }

                if (!contractsByEmployee.TryGetValue(attendance.EmployeeId, out var employeeContracts) || employeeContracts.Count == 0)
                {
                    batch.Warnings.Add($"{employee.EmployeeCode} - {employee.FullName}: chưa có hợp đồng Active trong kỳ.");
                    continue;
                }

                var contract = SelectPrimaryContract(employeeContracts, periodEnd);

                var jobLevelId = ResolveJobLevelId(employee);
                var policy = positionPolicies
                    .Where(p => p.PositionId == employee.PositionId && jobLevelId.HasValue && p.JobLevelId == jobLevelId.Value)
                    .OrderByDescending(p => p.EffectiveFrom)
                    .ThenByDescending(p => p.Version)
                    .FirstOrDefault();
                var source = new PayrollCalculationSource
                {
                    Attendance = attendance,
                    Employee = employee,
                    Contract = contract,
                    Formula = SelectFormula(formulas, contract, employee, jobLevelId, periodEnd),
                    TaxConfig = taxConfig,
                    InsuranceConfig = insuranceConfig,
                    PositionJobLevelPolicy = policy,
                    PerformanceReview = reviewsByEmployee.GetValueOrDefault(attendance.EmployeeId),
                    LegacyAllowances = legacyByEmployee.GetValueOrDefault(attendance.EmployeeId) ?? new List<EmployeeAllowance>(),
                    SalaryComponents = componentsByEmployee.GetValueOrDefault(attendance.EmployeeId) ?? new List<EmployeeSalaryComponent>(),
                    PitBrackets = pitBrackets,
                    OvertimeSegments = overtimeByEmployee.GetValueOrDefault(attendance.EmployeeId) ?? new List<OvertimeSegment>(),
                    DailySummaries = dailyByEmployee.GetValueOrDefault(attendance.EmployeeId) ?? new List<AttendanceDailySummary>(),
                    MonthlyInsuranceStatus = insuranceByEmployee.GetValueOrDefault(attendance.EmployeeId),
                    Adjustments = adjustmentsByEmployee.GetValueOrDefault(attendance.EmployeeId) ?? new List<PayrollAdjustment>(),
                    OvertimeRateConfigs = overtimeRateConfigs,
                    ExternalTimesheetLines = externalTimesheetByEmployee.GetValueOrDefault(attendance.EmployeeId) ?? new List<ExternalTimesheetLine>(),
                    EmploymentServicePeriods = servicePeriodsByEmployee.GetValueOrDefault(attendance.EmployeeId) ?? new List<EmploymentServicePeriod>(),
                    SeniorityPolicies = seniorityPolicies,
                    DependentCount = dependentCounts.GetValueOrDefault(attendance.EmployeeId),
                    TaxMethod = ResolveTaxMethod(employee, contract),
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    Period = periodText
                };

                source.ContractSegments = BuildContractSegments(source, employeeContracts);
                source.Variables = BuildVariables(source, jobLevelId);
                source.Snapshot = BuildSourceSnapshot(source, jobLevelId);
                batch.Sources.Add(source);
            }

            return batch;
        }

        private static void AppendExternalTimesheetAttendanceInputs(
            List<AttendanceSummary> attendanceInputs,
            List<ExternalTimesheetLine> externalTimesheetLines,
            Dictionary<int, List<Contract>> contractsByEmployee,
            PayrollPeriodDto period,
            PayrollSourceBatch batch)
        {
            var existingEmployeeIds = attendanceInputs.Select(a => a.EmployeeId).ToHashSet();
            var externalOnlyGroups = externalTimesheetLines
                .Where(l => l.CollaboratorEmployeeId.HasValue && !existingEmployeeIds.Contains(l.CollaboratorEmployeeId.Value))
                .GroupBy(l => l.CollaboratorEmployeeId!.Value);

            foreach (var group in externalOnlyGroups)
            {
                if (!contractsByEmployee.TryGetValue(group.Key, out var contracts) || contracts.Count == 0)
                {
                    batch.Warnings.Add($"Nhân viên/CTV Id {group.Key} có timesheet ngoài nhưng chưa có hợp đồng Active trong kỳ.");
                    continue;
                }

                var contract = SelectPrimaryContract(contracts, new DateTime(period.Year, period.Month, 1).AddMonths(1).AddTicks(-1));
                if (contract.Employee == null)
                {
                    batch.Warnings.Add($"Nhân viên/CTV Id {group.Key} có timesheet ngoài nhưng hợp đồng chưa nạp hồ sơ nhân sự.");
                    continue;
                }

                var approvedHours = group.Sum(l => l.ApprovedHours);
                attendanceInputs.Add(new AttendanceSummary
                {
                    EmployeeId = group.Key,
                    Employee = contract.Employee,
                    Month = period.Month,
                    Year = period.Year,
                    WorkDays = 0,
                    WorkedMinutes = (int)Math.Round(approvedHours * 60m, MidpointRounding.AwayFromZero),
                    PayableWorkHours = approvedHours,
                    GeneratedAt = DateTime.UtcNow
                });
            }
        }

        private static Contract SelectPrimaryContract(List<Contract> contracts, DateTime periodEnd)
        {
            return contracts
                .Where(c => c.StartDate.Date <= periodEnd.Date)
                .OrderByDescending(c => c.StartDate)
                .ThenByDescending(c => c.Version)
                .First();
        }

        private static PayrollFormula SelectFormula(List<PayrollFormula> formulas, Contract contract, Employee employee, int? jobLevelId, DateTime effectiveDate)
        {
            var explicitFormula = formulas
                .Where(f => contract.PayrollFormulaId.HasValue && f.Id == contract.PayrollFormulaId.Value && f.Lines.Count > 0)
                .OrderByDescending(f => f.Version)
                .FirstOrDefault();
            if (explicitFormula != null) return explicitFormula;

            var selected = formulas
                .Where(f => f.Lines.Count > 0 &&
                            ScopeMatch(f.ContractType, contract.ContractType) &&
                            ScopeMatch(f.PayBasis, contract.PayBasis) &&
                            ScopeMatch(f.EmployeeType, employee.Type) &&
                            ScopeMatch(f.DeptId, employee.DeptId) &&
                            ScopeMatch(f.PositionId, employee.PositionId) &&
                            ScopeMatch(f.JobLevelId, jobLevelId))
                .OrderByDescending(f => ScopeScore(f, employee, contract, jobLevelId))
                .ThenByDescending(f => f.EffectiveFrom)
                .ThenByDescending(f => f.Version)
                .FirstOrDefault();

            return selected ?? DefaultFormula(effectiveDate);
        }

        private static bool ScopeMatch<T>(T? expected, T? actual) where T : struct
        {
            return !expected.HasValue || (actual.HasValue && EqualityComparer<T>.Default.Equals(expected.Value, actual.Value));
        }

        private static int ScopeScore(PayrollFormula formula, Employee employee, Contract contract, int? jobLevelId)
        {
            var score = 0;
            if (formula.ContractType == contract.ContractType) score++;
            if (formula.PayBasis == contract.PayBasis) score++;
            if (formula.EmployeeType == employee.Type) score++;
            if (formula.DeptId == employee.DeptId) score++;
            if (formula.PositionId == employee.PositionId) score++;
            if (jobLevelId.HasValue && formula.JobLevelId == jobLevelId.Value) score++;
            return score;
        }

        private static Dictionary<string, decimal> BuildVariables(PayrollCalculationSource source, int? jobLevelId)
        {
            var contract = source.Contract;
            var attendance = source.Attendance;
            var policy = source.PositionJobLevelPolicy;
            var monthlyBase = RoundMoney(contract.BasicSalary * contract.SalaryPercentage / 100m);
            var standardWorkdays = contract.StandardWorkdaysSnapshot > 0 ? contract.StandardWorkdaysSnapshot : DefaultStandardWorkdays;
            var standardHours = contract.StandardHoursPerDaySnapshot > 0 ? contract.StandardHoursPerDaySnapshot : DefaultStandardHoursPerDay;
            var metrics = ResolveAttendanceMetrics(source, standardWorkdays, standardHours);
            var seniority = ResolveSeniorityMetrics(source, monthlyBase, standardWorkdays, metrics.Workdays);
            var actualWorkdays = metrics.Workdays;
            var hourlyRate = ResolveHourlyRate(contract, monthlyBase, standardWorkdays, standardHours);
            var contractSegmentSalary = source.ContractSegments.Count > 0
                ? source.ContractSegments.Sum(s => s.SalaryAmount)
                : RoundMoney(standardWorkdays > 0 ? monthlyBase / standardWorkdays * actualWorkdays : 0);
            var contractSegmentTaxable = source.ContractSegments.Count > 0
                ? source.ContractSegments.Sum(s => s.TaxableAmount)
                : contractSegmentSalary;
            var contractSegmentInsuranceBase = source.ContractSegments.Count > 0
                ? source.ContractSegments.Sum(s => s.InsuranceBaseAmount)
                : (contract.IsInsuranceEligible ? contractSegmentSalary : 0);

            var employeeComponentAmounts = source.SalaryComponents
                .GroupBy(c => c.SalaryComponentType.Code, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Sum(c => c.Amount), StringComparer.OrdinalIgnoreCase);

            var legacyInsuranceTaxableAllowance = source.LegacyAllowances
                .Where(a => a.AllowanceType?.IsInsuranceBase == true)
                .Sum(a => a.Amount ?? 0);
            var legacyTaxableAllowance = source.LegacyAllowances
                .Where(a => a.AllowanceType?.IsInsuranceBase != true && a.AllowanceType?.IsTaxable == true)
                .Sum(a => a.Amount ?? 0);
            var legacyNonTaxableAllowance = source.LegacyAllowances
                .Where(a => a.AllowanceType?.IsInsuranceBase != true && a.AllowanceType?.IsTaxable != true)
                .Sum(a => a.Amount ?? 0);

            var overtimeBase = 0m;
            var overtimePremium = 0m;
            var overtimeMinutes = 0;
            foreach (var segment in source.OvertimeSegments)
            {
                if (segment.TaxableAmountSnapshot != 0 || segment.TaxExemptAmountSnapshot != 0)
                {
                    overtimeBase += segment.TaxableAmountSnapshot;
                    overtimePremium += segment.TaxExemptAmountSnapshot;
                }
                else
                {
                    var hours = segment.Minutes / 60m;
                    var baseAmount = hourlyRate * hours;
                    var totalAmount = baseAmount * segment.RateMultiplierSnapshot;
                    overtimeBase += baseAmount;
                    overtimePremium += Math.Max(0, totalAmount - baseAmount);
                }

                overtimeMinutes += segment.Minutes;
            }

            var taxableInsuranceAdjustment = source.Adjustments
                .Where(a => !a.IsDeduction && a.IsTaxable && a.IsInsuranceBased)
                .Sum(a => a.Amount);
            var taxableNonInsuranceAdjustment = source.Adjustments
                .Where(a => !a.IsDeduction && a.IsTaxable && !a.IsInsuranceBased)
                .Sum(a => a.Amount);
            var nonTaxableAdjustment = source.Adjustments
                .Where(a => !a.IsDeduction && !a.IsTaxable)
                .Sum(a => a.Amount);
            var adjustmentDeduction = source.Adjustments
                .Where(a => a.IsDeduction)
                .Sum(a => Math.Abs(a.Amount));
            var externalTimesheetHours = source.ExternalTimesheetLines.Sum(l => l.ApprovedHours);
            var externalTimesheetAmount = source.ExternalTimesheetLines.Sum(l => l.Amount);

            var insuranceContributionEnabled = IsInsuranceContributionEnabled(source, metrics.UnpaidLeaveWorkdays);
            var unemploymentContributionEnabled = insuranceContributionEnabled && IsUnemploymentContributionEnabled(source);
            var employeeInsuranceRate = insuranceContributionEnabled
                ? source.InsuranceConfig.SocialInsuranceEmployeeRate +
                  source.InsuranceConfig.HealthInsuranceEmployeeRate
                : 0m;
            if (unemploymentContributionEnabled)
                employeeInsuranceRate += source.InsuranceConfig.UnemploymentInsuranceEmployeeRate;

            var employerContributionRate = insuranceContributionEnabled
                ? source.InsuranceConfig.SocialInsuranceEmployerRate +
                  source.InsuranceConfig.HealthInsuranceEmployerRate +
                  source.InsuranceConfig.UnionFeeEmployerRate
                : 0m;
            if (unemploymentContributionEnabled)
                employerContributionRate += source.InsuranceConfig.UnemploymentInsuranceEmployerRate;

            var variables = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["base_salary"] = contract.BasicSalary,
                ["salary_percentage"] = contract.SalaryPercentage,
                ["monthly_base_salary"] = monthlyBase,
                ["standard_workdays"] = standardWorkdays,
                ["standard_hours_per_day"] = standardHours,
                ["standard_working_hours"] = standardWorkdays * standardHours,
                ["actual_workdays"] = actualWorkdays,
                ["actual_attendance_days"] = metrics.AttendanceDays,
                ["actual_work_hours"] = metrics.WorkedMinutes / 60m,
                ["payable_work_hours"] = metrics.PayableWorkHours,
                ["worked_minutes"] = metrics.WorkedMinutes,
                ["late_minutes"] = metrics.LateMinutes,
                ["early_leave_minutes"] = metrics.EarlyLeaveMinutes,
                ["unpaid_leave_workdays"] = metrics.UnpaidLeaveWorkdays,
                ["maternity_leave_days"] = metrics.MaternityLeaveDays,
                ["sick_leave_days"] = metrics.SickLeaveDays,
                ["paid_leave_workdays"] = metrics.PaidLeaveWorkdays,
                ["service_months"] = seniority.ServiceMonths,
                ["service_years"] = seniority.ServiceYears,
                ["seniority_allowance"] = seniority.Allowance,
                ["seniority_allowance_prorated"] = seniority.ProratedAllowance,
                ["seniority_rate"] = seniority.RatePercent,
                ["hourly_rate"] = hourlyRate,
                ["daily_rate"] = contract.DailyRate ?? (standardWorkdays > 0 ? monthlyBase / standardWorkdays : 0),
                ["contract_segment_count"] = source.ContractSegments.Count,
                ["contract_segment_salary_amount"] = RoundMoney(contractSegmentSalary),
                ["contract_segment_taxable_amount"] = RoundMoney(contractSegmentTaxable),
                ["contract_segment_insurance_base_amount"] = RoundMoney(contractSegmentInsuranceBase),
                ["overtime_minutes"] = overtimeMinutes,
                ["overtime_hours"] = overtimeMinutes / 60m,
                ["overtime_base_amount"] = RoundMoney(overtimeBase),
                ["overtime_premium_amount"] = RoundMoney(overtimePremium),
                ["external_timesheet_hours"] = externalTimesheetHours,
                ["external_timesheet_amount"] = externalTimesheetAmount,
                ["position_allowance"] = ResolveComponentOrPolicy(employeeComponentAmounts, "POSITION_ALLOWANCE", policy?.PositionAllowance ?? 0),
                ["responsibility_allowance"] = ResolveComponentOrPolicy(employeeComponentAmounts, "RESPONSIBILITY_ALLOWANCE", policy?.ResponsibilityAllowance ?? 0),
                ["meal_allowance_per_day"] = employeeComponentAmounts.GetValueOrDefault("MEAL_ALLOWANCE"),
                ["kpi_bonus_amount"] = employeeComponentAmounts.GetValueOrDefault("KPI_BONUS"),
                ["kpi_score"] = source.PerformanceReview?.TotalScore ?? 0,
                ["dependent_count"] = source.DependentCount,
                ["personal_deduction"] = source.TaxConfig.PersonalDeduction,
                ["dependent_deduction"] = source.TaxConfig.DependentDeduction * source.DependentCount,
                ["flat_tax_threshold"] = source.TaxConfig.FlatTaxThreshold,
                ["flat_tax_rate"] = source.TaxConfig.FlatTaxRate,
                ["non_resident_tax_rate"] = source.TaxConfig.NonResidentTaxRate,
                ["employee_insurance_rate"] = employeeInsuranceRate,
                ["employer_contribution_rate"] = employerContributionRate,
                ["insurance_contribution_enabled"] = insuranceContributionEnabled ? 1 : 0,
                ["unemployment_insurance_contribution_enabled"] = unemploymentContributionEnabled ? 1 : 0,
                ["payroll_adjustment_taxable_insurance"] = taxableInsuranceAdjustment,
                ["payroll_adjustment_taxable"] = taxableNonInsuranceAdjustment,
                ["payroll_adjustment_nontaxable"] = nonTaxableAdjustment,
                ["payroll_adjustment_deduction"] = adjustmentDeduction,
                ["legacy_insurance_allowance"] = legacyInsuranceTaxableAllowance,
                ["legacy_taxable_allowance"] = legacyTaxableAllowance,
                ["legacy_nontaxable_allowance"] = legacyNonTaxableAllowance,
                ["gross_income"] = 0,
                ["insurance_salary"] = 0,
                ["employee_insurance_amount"] = 0,
                ["employer_contribution_amount"] = 0,
                ["taxable_gross_income"] = 0,
                ["taxable_income"] = 0,
                ["pit_tax_base"] = 0,
                ["pit_amount"] = 0,
                ["other_deductions"] = 0,
                ["net_salary"] = 0
            };

            foreach (var component in employeeComponentAmounts)
            {
                variables[SafeVariableName(component.Key)] = component.Value;
                variables[$"component_{SafeVariableName(component.Key)}"] = component.Value;
            }

            variables["job_level_id"] = jobLevelId ?? 0;
            variables["contract_type_id"] = (int)contract.ContractType;
            variables["pay_basis_id"] = (int)contract.PayBasis;
            variables["tax_method_id"] = (int)source.TaxMethod;
            return variables;
        }

        private static Dictionary<string, object?> BuildSourceSnapshot(PayrollCalculationSource source, int? jobLevelId)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["employeeId"] = source.Employee.Id,
                ["employeeCode"] = source.Employee.EmployeeCode,
                ["contractId"] = source.Contract.Id,
                ["contractType"] = source.Contract.ContractType.ToString(),
                ["payBasis"] = source.Contract.PayBasis.ToString(),
                ["taxMethod"] = source.TaxMethod.ToString(),
                ["jobLevelId"] = jobLevelId,
                ["taxConfigCode"] = source.TaxConfig.Code,
                ["insuranceConfigCode"] = source.InsuranceConfig.Code,
                ["formulaCode"] = source.Formula.FormulaCode,
                ["formulaVersion"] = source.Formula.Version,
                ["dependentCount"] = source.DependentCount,
                ["dailySummaryCount"] = source.DailySummaries.Count,
                ["employmentServicePeriodCount"] = source.EmploymentServicePeriods.Count,
                ["seniorityPolicyCodes"] = source.SeniorityPolicies.Select(p => p.Code).ToList(),
                ["contractSegmentCount"] = source.ContractSegments.Count,
                ["adjustmentCount"] = source.Adjustments.Count,
                ["adjustmentAmount"] = source.Adjustments.Sum(a => a.IsDeduction ? -Math.Abs(a.Amount) : a.Amount),
                ["externalTimesheetLineCount"] = source.ExternalTimesheetLines.Count,
                ["externalTimesheetAmount"] = source.ExternalTimesheetLines.Sum(l => l.Amount),
                ["monthlyInsuranceStatus"] = source.MonthlyInsuranceStatus?.Status.ToString(),
                ["isSocialInsuranceContributed"] = source.MonthlyInsuranceStatus?.IsSocialInsuranceContributed,
                ["isUnemploymentInsuranceContributed"] = source.MonthlyInsuranceStatus?.IsUnemploymentInsuranceContributed
            };
        }

        private static PayrollFormula DefaultFormula(DateTime effectiveDate)
        {
            return new PayrollFormula
            {
                Id = 0,
                FormulaCode = "DEFAULT_PAYROLL_V2",
                FormulaName = "Default payroll formula v2",
                Status = FormulaStatus.Approved,
                IsActive = true,
                Version = 1,
                EffectiveFrom = effectiveDate.Date,
                Lines = new List<PayrollFormulaLine>
                {
                    Line("BASE_SALARY_ACTUAL", "contract_segment_salary_amount", 10, true, true, true, false),
                    Line("POSITION_ALLOWANCE", "position_allowance / standard_workdays * actual_workdays", 20, true, true, true, false),
                    Line("RESPONSIBILITY_ALLOWANCE", "responsibility_allowance / standard_workdays * actual_workdays", 30, true, true, true, false),
                    Line("SENIORITY_ALLOWANCE", "seniority_allowance_prorated", 35, true, true, true, false),
                    Line("MEAL_ALLOWANCE", "meal_allowance_per_day * actual_attendance_days", 40, true, false, false, false),
                    Line("LEGACY_INSURANCE_ALLOWANCE", "legacy_insurance_allowance", 50, true, true, true, false),
                    Line("LEGACY_TAXABLE_ALLOWANCE", "legacy_taxable_allowance", 60, true, true, false, false),
                    Line("LEGACY_NONTAXABLE_ALLOWANCE", "legacy_nontaxable_allowance", 70, true, false, false, false),
                    Line("KPI_BONUS", "kpi_bonus_amount", 80, true, true, false, false),
                    Line("EXTERNAL_TIMESHEET_PAY", "external_timesheet_amount", 85, true, true, false, false),
                    Line("OT_BASE", "overtime_base_amount", 90, true, true, false, false),
                    Line("OT_PREMIUM", "overtime_premium_amount", 100, true, false, false, false),
                    Line("PAYROLL_ADJUSTMENT_TAXABLE_INSURANCE", "payroll_adjustment_taxable_insurance", 110, true, true, true, false),
                    Line("PAYROLL_ADJUSTMENT_TAXABLE", "payroll_adjustment_taxable", 120, true, true, false, false),
                    Line("PAYROLL_ADJUSTMENT_NONTAXABLE", "payroll_adjustment_nontaxable", 130, true, false, false, false),
                    Line("EMPLOYEE_INSURANCE", "insurance_salary * employee_insurance_rate", 200, false, false, false, true),
                    Line("PIT", "pit(pit_tax_base)", 210, false, false, false, true),
                    Line("PAYROLL_ADJUSTMENT_DEDUCTION", "payroll_adjustment_deduction", 220, false, false, false, true)
                }
            };
        }

        private static PayrollFormulaLine Line(string code, string expression, int order, bool gross, bool taxable, bool insurance, bool deduction)
        {
            return new PayrollFormulaLine
            {
                ComponentCode = code,
                Expression = expression,
                CalculationOrder = order,
                IsGrossComponent = gross,
                IsTaxable = taxable,
                IsInsuranceBased = insurance,
                IsDeduction = deduction,
                IsSnapshotRequired = true
            };
        }

        private static int? ResolveJobLevelId(Employee employee)
        {
            return employee.JobLevelId ?? (employee.Position?.JobLevel > 0 ? employee.Position.JobLevel : null);
        }

        private static decimal ResolveHourlyRate(Contract contract, decimal monthlyBase, decimal standardWorkdays, decimal standardHours)
        {
            if (contract.HourlyRate.HasValue && contract.HourlyRate.Value > 0) return contract.HourlyRate.Value;
            if (contract.DailyRate.HasValue && contract.DailyRate.Value > 0 && standardHours > 0) return contract.DailyRate.Value / standardHours;
            return standardWorkdays > 0 && standardHours > 0 ? monthlyBase / standardWorkdays / standardHours : 0;
        }

        private static decimal ResolveComponentOrPolicy(Dictionary<string, decimal> components, string code, decimal policyAmount)
        {
            return components.TryGetValue(code, out var componentAmount) && componentAmount > 0 ? componentAmount : policyAmount;
        }

        private static bool IsInsuranceContributionEnabled(PayrollCalculationSource source, decimal unpaidLeaveWorkdays)
        {
            if (source.MonthlyInsuranceStatus != null)
            {
                return source.MonthlyInsuranceStatus.IsSocialInsuranceContributed &&
                       source.MonthlyInsuranceStatus.Status != InsuranceContributionStatus.NotContributed;
            }

            if (!source.Contract.IsInsuranceEligible) return false;
            if (source.InsuranceConfig.UnpaidLeaveNoContributionThresholdDays > 0 &&
                unpaidLeaveWorkdays >= source.InsuranceConfig.UnpaidLeaveNoContributionThresholdDays)
                return false;

            if (source.InsuranceConfig.MinContractMonthsForContribution <= 0) return true;
            if (!source.Contract.EndDate.HasValue) return true;

            var durationMonths = ((source.Contract.EndDate.Value.Year - source.Contract.StartDate.Year) * 12) +
                                 source.Contract.EndDate.Value.Month - source.Contract.StartDate.Month + 1;
            return durationMonths >= source.InsuranceConfig.MinContractMonthsForContribution;
        }

        private static bool IsUnemploymentContributionEnabled(PayrollCalculationSource source)
        {
            if (source.MonthlyInsuranceStatus != null)
                return source.MonthlyInsuranceStatus.IsUnemploymentInsuranceContributed;

            return source.Contract.IsInsuranceEligible;
        }

        private static List<PayrollContractSegment> BuildContractSegments(PayrollCalculationSource source, List<Contract> contracts)
        {
            var segments = new List<PayrollContractSegment>();
            var totalCalendarDays = (source.PeriodEnd.Date - source.PeriodStart.Date).Days + 1;
            var totalStandardWorkdays = source.Contract.StandardWorkdaysSnapshot > 0 ? source.Contract.StandardWorkdaysSnapshot : DefaultStandardWorkdays;
            var totalStandardHours = source.Contract.StandardHoursPerDaySnapshot > 0 ? source.Contract.StandardHoursPerDaySnapshot : DefaultStandardHoursPerDay;
            var totalWorkdays = ResolveAttendanceMetrics(source, totalStandardWorkdays, totalStandardHours).Workdays;

            foreach (var contract in contracts.OrderBy(c => c.StartDate).ThenBy(c => c.Version))
            {
                var segmentStart = contract.StartDate.Date > source.PeriodStart.Date ? contract.StartDate.Date : source.PeriodStart.Date;
                var contractEnd = contract.EndDate?.Date ?? source.PeriodEnd.Date;
                var segmentEnd = contractEnd < source.PeriodEnd.Date ? contractEnd : source.PeriodEnd.Date;
                if (segmentStart > segmentEnd) continue;

                var standardWorkdays = contract.StandardWorkdaysSnapshot > 0 ? contract.StandardWorkdaysSnapshot : DefaultStandardWorkdays;
                var actualWorkdays = source.DailySummaries.Count > 0
                    ? source.DailySummaries
                        .Where(d => d.WorkDate.Date >= segmentStart && d.WorkDate.Date <= segmentEnd)
                        .Sum(d => d.WorkdayValue)
                    : AllocateWorkdaysByCalendar(totalWorkdays, segmentStart, segmentEnd, totalCalendarDays);
                actualWorkdays = Math.Min(Math.Max(0, actualWorkdays), standardWorkdays);

                var monthlyBase = RoundMoney(contract.BasicSalary * contract.SalaryPercentage / 100m);
                var salaryAmount = RoundMoney(standardWorkdays > 0 ? monthlyBase / standardWorkdays * actualWorkdays : 0);
                var insuranceBase = contract.IsInsuranceEligible
                    ? RoundMoney(standardWorkdays > 0 ? (contract.InsuranceSalary > 0 ? contract.InsuranceSalary : monthlyBase) / standardWorkdays * actualWorkdays : 0)
                    : 0;

                segments.Add(new PayrollContractSegment
                {
                    EmployeeId = source.Employee.Id,
                    ContractId = contract.Id,
                    StartDate = segmentStart,
                    EndDate = segmentEnd,
                    ContractType = contract.ContractType,
                    PayBasis = contract.PayBasis,
                    TaxMethod = ResolveTaxMethod(source.Employee, contract),
                    IsInsuranceEligible = contract.IsInsuranceEligible,
                    SegmentType = PayrollContractSegmentType.Contract,
                    BaseSalary = contract.BasicSalary,
                    SalaryPercentage = contract.SalaryPercentage,
                    StandardWorkdays = standardWorkdays,
                    ActualWorkdays = actualWorkdays,
                    SalaryAmount = salaryAmount,
                    TaxableAmount = salaryAmount,
                    InsuranceBaseAmount = insuranceBase,
                    SnapshotJson = JsonSerializer.Serialize(new
                    {
                        contract.Id,
                        contract.ContractNumber,
                        segmentStart,
                        segmentEnd,
                        contract.ContractType,
                        contract.PayBasis,
                        contract.BasicSalary,
                        contract.SalaryPercentage,
                        contract.InsuranceSalary,
                        actualWorkdays
                    })
                });
            }

            return segments;
        }

        private static AttendanceMetrics ResolveAttendanceMetrics(PayrollCalculationSource source, decimal standardWorkdays, decimal standardHours)
        {
            if (source.DailySummaries.Count == 0)
            {
                return new AttendanceMetrics
                {
                    Workdays = Math.Min(Math.Max(0, source.Attendance.WorkDays), standardWorkdays),
                    AttendanceDays = Math.Min(Math.Max(0, source.Attendance.WorkDays), standardWorkdays),
                    WorkedMinutes = source.Attendance.WorkedMinutes,
                    PayableWorkHours = source.Attendance.PayableWorkHours,
                    LateMinutes = source.Attendance.LateMinutes,
                    EarlyLeaveMinutes = source.Attendance.EarlyLeaveMinutes
                };
            }

            var daily = source.DailySummaries;
            var workedMinutes = daily.Sum(d => d.WorkingMinutes);
            var workdays = Math.Min(Math.Max(0, daily.Sum(d => d.WorkdayValue)), standardWorkdays);
            var payableWorkHours = RoundHours(workdays * standardHours);
            return new AttendanceMetrics
            {
                Workdays = workdays,
                AttendanceDays = Math.Min(daily.Count(IsActualAttendanceDay), standardWorkdays),
                WorkedMinutes = workedMinutes,
                PayableWorkHours = payableWorkHours,
                LateMinutes = daily.Sum(d => d.LateMinutes),
                EarlyLeaveMinutes = daily.Sum(d => d.EarlyLeaveMinutes),
                UnpaidLeaveWorkdays = daily.Where(d => d.AttendanceStatus == AttendanceDailyStatus.UnpaidLeave).Sum(ResolveNonPayableLeaveWorkday),
                PaidLeaveWorkdays = daily.Where(d => d.AttendanceStatus == AttendanceDailyStatus.PaidLeave).Sum(d => d.WorkdayValue),
                MaternityLeaveDays = daily.Count(d => d.AttendanceStatus == AttendanceDailyStatus.MaternityLeave),
                SickLeaveDays = daily.Count(d => d.AttendanceStatus == AttendanceDailyStatus.SickLeave)
            };
        }

        private static bool IsActualAttendanceDay(AttendanceDailySummary summary)
        {
            if (summary.WorkdayValue <= 0) return false;
            return summary.AttendanceStatus is AttendanceDailyStatus.Present
                or AttendanceDailyStatus.HalfDay
                or AttendanceDailyStatus.ManualAdjusted;
        }

        private static decimal ResolveNonPayableLeaveWorkday(AttendanceDailySummary summary)
        {
            return summary.WorkdayValue > 0 ? summary.WorkdayValue : 1m;
        }

        private static SeniorityMetrics ResolveSeniorityMetrics(
            PayrollCalculationSource source,
            decimal monthlyBase,
            decimal standardWorkdays,
            decimal actualWorkdays)
        {
            var serviceMonths = ResolveServiceMonths(source);
            var serviceYears = Math.Floor(serviceMonths / 12m);
            var policy = SelectSeniorityPolicy(source.SeniorityPolicies, serviceMonths);
            if (policy == null || serviceMonths <= 0)
            {
                return new SeniorityMetrics
                {
                    ServiceMonths = serviceMonths,
                    ServiceYears = serviceYears
                };
            }

            var allowance = ResolveSeniorityAllowance(policy, monthlyBase, serviceYears, out var ratePercent);
            var proratedAllowance = standardWorkdays > 0
                ? RoundMoney(allowance / standardWorkdays * actualWorkdays)
                : allowance;

            return new SeniorityMetrics
            {
                ServiceMonths = serviceMonths,
                ServiceYears = serviceYears,
                Allowance = RoundMoney(allowance),
                ProratedAllowance = proratedAllowance,
                RatePercent = ratePercent
            };
        }

        private static decimal ResolveServiceMonths(PayrollCalculationSource source)
        {
            var start = source.Employee.JoinedDate?.Date ?? source.Contract.StartDate.Date;
            var end = source.PeriodEnd.Date;
            if (start > end) return 0;

            var baseDays = (end - start).Days + 1;
            var excludedDays = MergeDateRanges(source.EmploymentServicePeriods
                    .Where(IsExcludedFromSeniority)
                    .Select(p => (
                        Start: p.PeriodStart.Date < start ? start : p.PeriodStart.Date,
                        End: p.PeriodEnd.Date > end ? end : p.PeriodEnd.Date))
                    .Where(r => r.Start <= r.End))
                .Sum(r => (r.End - r.Start).Days + 1);

            var effectiveDays = Math.Max(0, baseDays - excludedDays);
            return Math.Floor(effectiveDays / 30.436875m);
        }

        private static bool IsExcludedFromSeniority(EmploymentServicePeriod period)
        {
            return !period.IsActualWorkingTime ||
                   period.PeriodType is EmploymentServicePeriodType.UnpaidLeave
                       or EmploymentServicePeriodType.Suspension
                       or EmploymentServicePeriodType.PriorSeverancePaid;
        }

        private static List<(DateTime Start, DateTime End)> MergeDateRanges(IEnumerable<(DateTime Start, DateTime End)> ranges)
        {
            var ordered = ranges.OrderBy(r => r.Start).ThenBy(r => r.End).ToList();
            if (ordered.Count <= 1) return ordered;

            var merged = new List<(DateTime Start, DateTime End)> { ordered[0] };
            foreach (var range in ordered.Skip(1))
            {
                var last = merged[^1];
                if (range.Start <= last.End.AddDays(1))
                {
                    merged[^1] = (last.Start, range.End > last.End ? range.End : last.End);
                    continue;
                }

                merged.Add(range);
            }

            return merged;
        }

        private static PayrollPolicy? SelectSeniorityPolicy(IEnumerable<PayrollPolicy> policies, decimal serviceMonths)
        {
            return policies
                .Where(p => IsSeniorityPolicyMatched(p, serviceMonths))
                .OrderByDescending(p => p.ValueType == PayrollPolicyValueType.Bracket ? 1 : 0)
                .ThenByDescending(p => p.FromAmount ?? 0)
                .ThenByDescending(p => p.EffectiveFrom)
                .ThenByDescending(p => p.Version)
                .FirstOrDefault();
        }

        private static bool IsSeniorityPolicyMatched(PayrollPolicy policy, decimal serviceMonths)
        {
            if (policy.ValueType != PayrollPolicyValueType.Bracket &&
                !policy.FromAmount.HasValue &&
                !policy.ToAmount.HasValue)
                return true;

            var from = policy.FromAmount ?? 0;
            var to = policy.ToAmount ?? decimal.MaxValue;
            return serviceMonths >= from && serviceMonths < to;
        }

        private static decimal ResolveSeniorityAllowance(PayrollPolicy policy, decimal monthlyBase, decimal serviceYears, out decimal ratePercent)
        {
            ratePercent = 0;
            return policy.ValueType switch
            {
                PayrollPolicyValueType.Amount => policy.Amount ?? 0,
                PayrollPolicyValueType.Bracket => ResolveBracketSeniorityAllowance(policy, monthlyBase, out ratePercent),
                PayrollPolicyValueType.RatePercent => ResolveRateSeniorityAllowance(policy, monthlyBase, serviceYears, out ratePercent),
                PayrollPolicyValueType.Formula => ResolveFormulaSeniorityAllowance(policy, monthlyBase, serviceYears, out ratePercent),
                _ => 0
            };
        }

        private static decimal ResolveBracketSeniorityAllowance(PayrollPolicy policy, decimal monthlyBase, out decimal ratePercent)
        {
            ratePercent = policy.RatePercent ?? 0;
            if (policy.Amount.HasValue) return policy.Amount.Value;
            return monthlyBase * ratePercent / 100m;
        }

        private static decimal ResolveRateSeniorityAllowance(PayrollPolicy policy, decimal monthlyBase, decimal serviceYears, out decimal ratePercent)
        {
            var annualRate = policy.RatePercent ?? 0;
            var totalRate = annualRate * serviceYears;
            if (policy.ToAmount.HasValue)
                totalRate = Math.Min(totalRate, policy.ToAmount.Value);

            ratePercent = totalRate;
            var amount = monthlyBase * totalRate / 100m;
            if (policy.Amount.HasValue && policy.Amount.Value > 0)
                amount = Math.Min(amount, policy.Amount.Value);

            return amount;
        }

        private static decimal ResolveFormulaSeniorityAllowance(PayrollPolicy policy, decimal monthlyBase, decimal serviceYears, out decimal ratePercent)
        {
            ratePercent = 0;
            if (string.IsNullOrWhiteSpace(policy.FormulaJson)) return 0;

            try
            {
                using var document = JsonDocument.Parse(policy.FormulaJson);
                var root = document.RootElement;
                var amountPerYear = ReadDecimal(root, "amountPerYear");
                var fixedAmount = ReadDecimal(root, "fixedAmount");
                var ratePerYear = ReadDecimal(root, "ratePerYear");
                var maxRate = ReadDecimal(root, "maxRate");
                var capAmount = ReadDecimal(root, "capAmount");

                if (fixedAmount > 0) return ApplyAmountCap(fixedAmount, capAmount);
                if (amountPerYear > 0) return ApplyAmountCap(amountPerYear * serviceYears, capAmount);

                ratePercent = ratePerYear * serviceYears;
                if (maxRate > 0) ratePercent = Math.Min(ratePercent, maxRate);
                return ApplyAmountCap(monthlyBase * ratePercent / 100m, capAmount);
            }
            catch (JsonException)
            {
                return 0;
            }
        }

        private static decimal ReadDecimal(JsonElement root, string propertyName)
        {
            return root.TryGetProperty(propertyName, out var property) && property.TryGetDecimal(out var value)
                ? value
                : 0;
        }

        private static decimal ApplyAmountCap(decimal amount, decimal capAmount)
        {
            return capAmount > 0 ? Math.Min(amount, capAmount) : amount;
        }

        private static decimal AllocateWorkdaysByCalendar(decimal totalWorkdays, DateTime segmentStart, DateTime segmentEnd, int totalCalendarDays)
        {
            if (totalCalendarDays <= 0) return 0;
            var segmentDays = (segmentEnd.Date - segmentStart.Date).Days + 1;
            return Math.Round(totalWorkdays * segmentDays / totalCalendarDays, 2, MidpointRounding.AwayFromZero);
        }

        private static TaxMethod ResolveTaxMethod(Employee employee, Contract contract)
        {
            if (contract.TaxMethodOverride.HasValue) return contract.TaxMethodOverride.Value;
            if (employee.ResidenceStatus == ResidenceStatus.NonResident) return TaxMethod.NonResident20Percent;

            if (contract.EndDate.HasValue)
            {
                var durationDays = (contract.EndDate.Value.Date - contract.StartDate.Date).TotalDays + 1;
                if (durationDays > 0 && durationDays < 90) return TaxMethod.Flat10Percent;
            }

            if (contract.PayBasis is PayBasis.Hourly or PayBasis.Daily && contract.ContractType != ContractType.Definite && contract.ContractType != ContractType.Indefinite)
                return TaxMethod.Flat10Percent;

            return TaxMethod.Progressive;
        }

        private static TaxConfig DefaultTaxConfig()
        {
            return new TaxConfig
            {
                Code = "VN_PERSONAL_INCOME_TAX_2020",
                Name = "Default PIT config",
                PersonalDeduction = 11_000_000m,
                DependentDeduction = 4_400_000m,
                FlatTaxThreshold = 2_000_000m,
                FlatTaxRate = 0.10m,
                NonResidentTaxRate = 0.20m,
                EffectiveFrom = new DateTime(2020, 7, 1),
                Version = 1,
                IsActive = true
            };
        }

        private static InsuranceConfig DefaultInsuranceConfig()
        {
            return new InsuranceConfig
            {
                Code = "VN_STANDARD_INSURANCE_2025",
                Name = "Default insurance config",
                SocialInsuranceEmployeeRate = 0.08m,
                HealthInsuranceEmployeeRate = 0.015m,
                UnemploymentInsuranceEmployeeRate = 0.01m,
                SocialInsuranceEmployerRate = 0.175m,
                HealthInsuranceEmployerRate = 0.03m,
                UnemploymentInsuranceEmployerRate = 0.01m,
                UnionFeeEmployerRate = 0.02m,
                UnpaidLeaveNoContributionThresholdDays = 14,
                MinContractMonthsForContribution = 1,
                EffectiveFrom = new DateTime(2025, 7, 1),
                Version = 1,
                IsActive = true
            };
        }

        private static List<PITTaxBracket> DefaultPitBrackets()
        {
            var effective = new DateTime(2020, 7, 1);
            return new List<PITTaxBracket>
            {
                Bracket(1, 0, 5_000_000m, 0.05m, 0, effective),
                Bracket(2, 5_000_000m, 10_000_000m, 0.10m, 250_000m, effective),
                Bracket(3, 10_000_000m, 18_000_000m, 0.15m, 750_000m, effective),
                Bracket(4, 18_000_000m, 32_000_000m, 0.20m, 1_650_000m, effective),
                Bracket(5, 32_000_000m, 52_000_000m, 0.25m, 3_250_000m, effective),
                Bracket(6, 52_000_000m, 80_000_000m, 0.30m, 5_850_000m, effective),
                Bracket(7, 80_000_000m, null, 0.35m, 9_850_000m, effective)
            };
        }

        private static PITTaxBracket Bracket(int level, decimal min, decimal? max, decimal rate, decimal quickDeduction, DateTime effective)
        {
            return new PITTaxBracket
            {
                Code = "VN_PROGRESSIVE_PIT_2020",
                Level = level,
                MinIncome = min,
                MaxIncome = max,
                TaxRate = rate,
                QuickDeduction = quickDeduction,
                EffectiveFrom = effective,
                Version = 1,
                IsActive = true
            };
        }

        private static string SafeVariableName(string value)
        {
            var chars = value.Trim().ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
            return new string(chars);
        }

        private sealed class AttendanceMetrics
        {
            public decimal Workdays { get; set; }
            public decimal AttendanceDays { get; set; }
            public int WorkedMinutes { get; set; }
            public decimal PayableWorkHours { get; set; }
            public int LateMinutes { get; set; }
            public int EarlyLeaveMinutes { get; set; }
            public decimal UnpaidLeaveWorkdays { get; set; }
            public decimal PaidLeaveWorkdays { get; set; }
            public decimal MaternityLeaveDays { get; set; }
            public decimal SickLeaveDays { get; set; }
        }

        private sealed class SeniorityMetrics
        {
            public decimal ServiceMonths { get; set; }
            public decimal ServiceYears { get; set; }
            public decimal Allowance { get; set; }
            public decimal ProratedAllowance { get; set; }
            public decimal RatePercent { get; set; }
        }

        private static decimal RoundHours(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
        private static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
