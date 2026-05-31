using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;
using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using System.Text.Json;

namespace HRM.backend.src.HRM.Application.UseCases.PayrollAllowances
{
    internal static class PayrollSlipMapper
    {
        public static SalarySlipDto Map(Payroll payroll, bool includeDetails = true)
        {
            var variables = ExtractFinalVariables(payroll.PolicySnapshotJson);
            return new SalarySlipDto
            {
                Id = payroll.Id,
                EmployeeId = payroll.EmployeeId ?? 0,
                EmployeeCode = payroll.Employee?.EmployeeCode ?? string.Empty,
                EmployeeName = payroll.Employee?.FullName ?? string.Empty,
                DepartmentName = payroll.Employee?.Department?.DeptName,
                PositionName = payroll.Employee?.Position?.Title,
                Month = payroll.Month ?? 0,
                Year = payroll.Year ?? 0,
                Period = payroll.Period ?? $"{payroll.Month:00}/{payroll.Year}",
                BaseSalary = payroll.BaseSalary ?? 0,
                BaseSalaryActual = payroll.BaseSalaryActual ?? 0,
                StandardWorkDays = variables.GetValueOrDefault("standard_workdays"),
                StandardWorkHours = variables.GetValueOrDefault("standard_working_hours"),
                ActualWorkDays = payroll.ActualWorkDays ?? 0,
                ActualWorkHours = payroll.ActualWorkHours ?? 0,
                PayableWorkHours = variables.GetValueOrDefault("payable_work_hours"),
                WorkedMinutes = (int)variables.GetValueOrDefault("worked_minutes"),
                LateMinutes = (int)variables.GetValueOrDefault("late_minutes"),
                EarlyLeaveMinutes = (int)variables.GetValueOrDefault("early_leave_minutes"),
                UnpaidLeaveWorkdays = variables.GetValueOrDefault("unpaid_leave_workdays"),
                ServiceMonths = variables.GetValueOrDefault("service_months"),
                ServiceYears = variables.GetValueOrDefault("service_years"),
                SeniorityAllowance = variables.GetValueOrDefault("seniority_allowance_prorated"),
                SeniorityRate = variables.GetValueOrDefault("seniority_rate"),
                ActualOtMinutes = payroll.ActualOtMinutes ?? 0,
                GrossIncome = payroll.GrossIncome ?? payroll.GrossSalary ?? 0,
                InsuranceSalary = payroll.InsuranceSalary ?? 0,
                EmployeeInsuranceAmount = payroll.EmployeeInsuranceAmount ?? payroll.InsuranceDeduction ?? 0,
                EmployerContributionAmount = payroll.EmployerContributionAmount ?? 0,
                TaxableGrossIncome = payroll.TaxableGrossIncome ?? 0,
                TaxableIncome = payroll.TaxableIncome ?? 0,
                PitAmount = payroll.PitAmount ?? 0,
                OtherDeductions = payroll.OtherDeductions ?? 0,
                NetSalary = payroll.NetSalary ?? 0,
                TotalCompanyCost = payroll.TotalCompanyCost ?? 0,
                Status = payroll.Status,
                CalculatedAt = payroll.CalculatedAt,
                LockedAt = payroll.LockedAt,
                Details = includeDetails
                    ? payroll.Details
                        .OrderBy(d => d.Id)
                        .Select(d => new SalarySlipDetailDto
                        {
                            Id = d.Id,
                            ComponentCode = d.ComponentCode,
                            ComponentName = d.ComponentName,
                            Amount = d.Amount,
                            TaxableAmount = d.TaxableAmount,
                            InsuranceBaseAmount = d.InsuranceBaseAmount,
                            IsIncome = d.IsIncome,
                            IsDeduction = d.IsDeduction,
                            IsTaxable = d.IsTaxable,
                            IsInsuranceBased = d.IsInsuranceBased,
                            Note = d.Note
                        })
                        .ToList()
                    : new List<SalarySlipDetailDto>()
            };
        }

        private static Dictionary<string, decimal> ExtractFinalVariables(string? policySnapshotJson)
        {
            if (string.IsNullOrWhiteSpace(policySnapshotJson))
                return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using var document = JsonDocument.Parse(policySnapshotJson);
                if (!document.RootElement.TryGetProperty("finalVariables", out var variables) ||
                    variables.ValueKind != JsonValueKind.Object)
                    return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

                var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in variables.EnumerateObject())
                {
                    if (property.Value.TryGetDecimal(out var value))
                        result[property.Name] = value;
                }

                return result;
            }
            catch (JsonException)
            {
                return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
