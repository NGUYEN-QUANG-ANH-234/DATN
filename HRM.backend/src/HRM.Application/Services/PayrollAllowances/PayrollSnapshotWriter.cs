using System.Text.Json;
using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;
using HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Services;
using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.Services.PayrollAllowances
{
    public class PayrollSnapshotWriter : IPayrollSnapshotWriter
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public Payroll CreateSnapshot(PayrollCalculationSource source, PayrollCalculationOutput output, int actorAccountId)
        {
            var variables = output.FinalVariables;
            var payroll = new Payroll
            {
                EmployeeId = source.Employee.Id,
                Month = source.Attendance.Month,
                Year = source.Attendance.Year,
                Period = source.Period,
                BaseSalary = variables.GetValueOrDefault("monthly_base_salary"),
                BaseSalaryActual = output.BaseSalaryActual,
                GrossIncome = output.GrossIncome,
                GrossSalary = output.GrossIncome,
                TotalAllowance = output.TotalAllowance,
                TotalBonus = output.TotalBonus,
                InsuranceSalary = output.InsuranceSalary,
                InsuranceDeduction = output.EmployeeInsuranceAmount,
                EmployeeInsuranceAmount = output.EmployeeInsuranceAmount,
                EmployerContributionAmount = output.EmployerContributionAmount,
                TaxDeductionPersonal = variables.GetValueOrDefault("personal_deduction"),
                TaxDeductionFamily = variables.GetValueOrDefault("dependent_deduction"),
                TaxableGrossIncome = output.TaxableGrossIncome,
                TaxableIncome = output.TaxableIncome,
                PitAmount = output.PitAmount,
                OtherDeductions = output.OtherDeductions,
                NetSalary = output.NetSalary,
                TotalCompanyCost = output.TotalCompanyCost,
                ActualWorkDays = variables.GetValueOrDefault("actual_workdays"),
                ActualWorkHours = variables.GetValueOrDefault("actual_work_hours"),
                ActualOtMinutes = (int)variables.GetValueOrDefault("overtime_minutes"),
                Status = PayrollStatus.Calculated,
                CreatedAt = DateTime.UtcNow,
                CalculatedAt = DateTime.UtcNow,
                CalculatedByAccountId = actorAccountId,
                FormulaSnapshotJson = BuildFormulaSnapshot(source, output),
                PolicySnapshotJson = BuildPolicySnapshot(source, output)
            };

            foreach (var line in output.Lines.Where(l => l.FormulaLine.IsSnapshotRequired || l.Amount != 0))
            {
                payroll.Details.Add(new PayrollDetail
                {
                    ComponentCode = line.ComponentCode,
                    ComponentName = line.ComponentName,
                    Amount = line.Amount,
                    TaxableAmount = line.TaxableAmount,
                    InsuranceBaseAmount = line.InsuranceBaseAmount,
                    IsIncome = line.IsIncome,
                    IsDeduction = line.IsDeduction,
                    IsTaxable = line.IsTaxable,
                    IsInsuranceBased = line.IsInsuranceBased,
                    ProrationType = line.ProrationType,
                    CalculationMethod = line.CalculationMethod,
                    Note = line.Note,
                    SnapshotJson = JsonSerializer.Serialize(new
                    {
                        formulaLineId = line.FormulaLine.Id,
                        line.FormulaLine.ComponentCode,
                        line.FormulaLine.Expression,
                        line.FormulaLine.CalculationOrder,
                        line.RawAmount,
                        line.Amount,
                        line.TaxableAmount,
                        line.InsuranceBaseAmount
                    }, JsonOptions),
                    CreatedAt = DateTime.UtcNow
                });
            }

            foreach (var segment in source.ContractSegments)
            {
                payroll.ContractSegments.Add(new PayrollContractSegment
                {
                    EmployeeId = segment.EmployeeId,
                    ContractId = segment.ContractId,
                    StartDate = segment.StartDate,
                    EndDate = segment.EndDate,
                    ContractType = segment.ContractType,
                    PayBasis = segment.PayBasis,
                    TaxMethod = segment.TaxMethod,
                    IsInsuranceEligible = segment.IsInsuranceEligible,
                    SegmentType = segment.SegmentType,
                    BaseSalary = segment.BaseSalary,
                    SalaryPercentage = segment.SalaryPercentage,
                    StandardWorkdays = segment.StandardWorkdays,
                    ActualWorkdays = segment.ActualWorkdays,
                    SalaryAmount = segment.SalaryAmount,
                    TaxableAmount = segment.TaxableAmount,
                    InsuranceBaseAmount = segment.InsuranceBaseAmount,
                    SnapshotJson = segment.SnapshotJson,
                    CreatedAt = DateTime.UtcNow
                });
            }

            return payroll;
        }

        private static string BuildFormulaSnapshot(PayrollCalculationSource source, PayrollCalculationOutput output)
        {
            return JsonSerializer.Serialize(new
            {
                source.Formula.Id,
                source.Formula.FormulaCode,
                source.Formula.FormulaName,
                source.Formula.Version,
                source.Formula.EffectiveFrom,
                source.Formula.EffectiveTo,
                lines = output.Lines.Select(l => new
                {
                    l.ComponentCode,
                    l.FormulaLine.Expression,
                    l.FormulaLine.CalculationOrder,
                    l.IsIncome,
                    l.IsDeduction,
                    l.IsTaxable,
                    l.IsInsuranceBased,
                    l.RawAmount,
                    l.Amount,
                    l.TaxableAmount,
                    l.InsuranceBaseAmount
                })
            }, JsonOptions);
        }

        private static string BuildPolicySnapshot(PayrollCalculationSource source, PayrollCalculationOutput output)
        {
            return JsonSerializer.Serialize(new
            {
                source.Snapshot,
                sourceVariables = source.Variables,
                finalVariables = output.FinalVariables,
                tax = new
                {
                    source.TaxConfig.Code,
                    source.TaxConfig.Version,
                    source.TaxConfig.EffectiveFrom,
                    source.TaxConfig.PersonalDeduction,
                    source.TaxConfig.DependentDeduction,
                    source.TaxConfig.FlatTaxThreshold,
                    source.TaxConfig.FlatTaxRate,
                    source.TaxConfig.NonResidentTaxRate,
                    pitBracketCode = source.PitBrackets.FirstOrDefault()?.Code
                },
                insurance = new
                {
                    source.InsuranceConfig.Code,
                    source.InsuranceConfig.Version,
                    source.InsuranceConfig.EffectiveFrom,
                    employeeRate = output.FinalVariables.GetValueOrDefault("employee_insurance_rate"),
                    employerRate = output.FinalVariables.GetValueOrDefault("employer_contribution_rate"),
                    source.InsuranceConfig.UnpaidLeaveNoContributionThresholdDays,
                    source.InsuranceConfig.MinContractMonthsForContribution
                },
                seniority = new
                {
                    serviceMonths = output.FinalVariables.GetValueOrDefault("service_months"),
                    serviceYears = output.FinalVariables.GetValueOrDefault("service_years"),
                    allowance = output.FinalVariables.GetValueOrDefault("seniority_allowance"),
                    proratedAllowance = output.FinalVariables.GetValueOrDefault("seniority_allowance_prorated"),
                    rate = output.FinalVariables.GetValueOrDefault("seniority_rate"),
                    policies = source.SeniorityPolicies.Select(p => new
                    {
                        p.Id,
                        p.Code,
                        p.Name,
                        p.ValueType,
                        p.RatePercent,
                        p.Amount,
                        p.FromAmount,
                        p.ToAmount,
                        p.EffectiveFrom,
                        p.Version
                    })
                },
                monthlyInsuranceStatus = source.MonthlyInsuranceStatus == null ? null : new
                {
                    source.MonthlyInsuranceStatus.Status,
                    source.MonthlyInsuranceStatus.IsSocialInsuranceContributed,
                    source.MonthlyInsuranceStatus.IsUnemploymentInsuranceContributed,
                    source.MonthlyInsuranceStatus.NonContributionReason,
                    source.MonthlyInsuranceStatus.UnpaidLeaveWorkingDays,
                    source.MonthlyInsuranceStatus.MaternityLeaveDays,
                    source.MonthlyInsuranceStatus.SickLeaveDays,
                    source.MonthlyInsuranceStatus.OfficialContractWorkingDays,
                    source.MonthlyInsuranceStatus.UnemploymentInsuranceAmount
                },
                externalTimesheets = source.ExternalTimesheetLines.Select(l => new
                {
                    l.Id,
                    l.ImportId,
                    l.CollaboratorEmployeeId,
                    l.CollaboratorCode,
                    l.WorkDate,
                    l.ProjectCode,
                    l.TaskCode,
                    l.ApprovedHours,
                    l.HourlyRate,
                    l.Amount
                }),
                attendanceDaily = new
                {
                    count = source.DailySummaries.Count,
                    approvedWorkdays = source.DailySummaries.Sum(d => d.WorkdayValue),
                    workedMinutes = source.DailySummaries.Sum(d => d.WorkingMinutes),
                    lateMinutes = source.DailySummaries.Sum(d => d.LateMinutes),
                    earlyLeaveMinutes = source.DailySummaries.Sum(d => d.EarlyLeaveMinutes)
                },
                adjustments = source.Adjustments.Select(a => new
                {
                    a.Id,
                    a.AdjustmentType,
                    a.Amount,
                    a.IsTaxable,
                    a.IsInsuranceBased,
                    a.IsDeduction,
                    a.Reason
                }),
                overtimeSegments = source.OvertimeSegments.Select(s => new
                {
                    s.Id,
                    s.OvertimeRequestId,
                    s.OvertimeType,
                    s.SegmentStartAt,
                    s.SegmentEndAt,
                    s.Minutes,
                    s.PolicyCode,
                    s.RateMultiplierSnapshot,
                    s.TaxableAmountSnapshot,
                    s.TaxExemptAmountSnapshot
                }),
                contractSegments = source.ContractSegments.Select(s => new
                {
                    s.ContractId,
                    s.StartDate,
                    s.EndDate,
                    s.ContractType,
                    s.PayBasis,
                    s.TaxMethod,
                    s.ActualWorkdays,
                    s.SalaryAmount,
                    s.TaxableAmount,
                    s.InsuranceBaseAmount
                })
            }, JsonOptions);
        }
    }
}
