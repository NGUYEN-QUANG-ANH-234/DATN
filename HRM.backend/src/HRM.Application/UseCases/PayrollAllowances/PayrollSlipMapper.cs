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
                SubmittedAt = payroll.SubmittedAt,
                ApprovedAt = payroll.ApprovedAt,
                LockedAt = payroll.LockedAt,
                ReviewNote = payroll.ReviewNote,
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
                            Note = d.Note,
                            ProjectBonusSources = ExtractProjectBonusSources(d.SnapshotJson),
                            ExternalTimesheetSources = ExtractExternalTimesheetSources(d.SnapshotJson)
                        })
                        .ToList()
                    : new List<SalarySlipDetailDto>()
            };
        }

        private static List<ExternalTimesheetPayrollSourceDto> ExtractExternalTimesheetSources(string? detailSnapshotJson)
        {
            if (string.IsNullOrWhiteSpace(detailSnapshotJson))
                return new List<ExternalTimesheetPayrollSourceDto>();

            try
            {
                using var document = JsonDocument.Parse(detailSnapshotJson);
                if (!document.RootElement.TryGetProperty("externalTimesheetSources", out var sources) ||
                    sources.ValueKind != JsonValueKind.Array)
                    return new List<ExternalTimesheetPayrollSourceDto>();

                var result = new List<ExternalTimesheetPayrollSourceDto>();
                foreach (var source in sources.EnumerateArray())
                {
                    result.Add(new ExternalTimesheetPayrollSourceDto
                    {
                        Id = GetInt(source, "id"),
                        ImportId = GetInt(source, "importId"),
                        FileName = GetString(source, "fileName"),
                        PayrollPeriod = GetString(source, "payrollPeriod"),
                        ApprovedAt = GetNullableDateTime(source, "approvedAt"),
                        CollaboratorEmployeeId = GetNullableInt(source, "collaboratorEmployeeId"),
                        CollaboratorCode = GetString(source, "collaboratorCode") ?? string.Empty,
                        CollaboratorName = GetString(source, "collaboratorNameSnapshot"),
                        WorkDate = GetNullableDateTime(source, "workDate") ?? DateTime.MinValue,
                        ProjectCode = GetString(source, "projectCode") ?? string.Empty,
                        TaskCode = GetString(source, "taskCode") ?? string.Empty,
                        ApprovedHours = GetDecimal(source, "approvedHours"),
                        HourlyRate = GetDecimal(source, "hourlyRate"),
                        Amount = GetDecimal(source, "amount"),
                        Note = GetString(source, "note")
                    });
                }

                return result;
            }
            catch (JsonException)
            {
                return new List<ExternalTimesheetPayrollSourceDto>();
            }
        }

        private static List<ProjectBonusPayrollSourceDto> ExtractProjectBonusSources(string? detailSnapshotJson)
        {
            if (string.IsNullOrWhiteSpace(detailSnapshotJson))
                return new List<ProjectBonusPayrollSourceDto>();

            try
            {
                using var document = JsonDocument.Parse(detailSnapshotJson);
                if (!document.RootElement.TryGetProperty("projectBonusSources", out var sources) ||
                    sources.ValueKind != JsonValueKind.Array)
                    return new List<ProjectBonusPayrollSourceDto>();

                var result = new List<ProjectBonusPayrollSourceDto>();
                foreach (var source in sources.EnumerateArray())
                {
                    result.Add(new ProjectBonusPayrollSourceDto
                    {
                        Id = GetInt(source, "id"),
                        BatchId = GetInt(source, "batchId"),
                        FileName = GetString(source, "fileName"),
                        PayrollPeriod = GetString(source, "payrollPeriod"),
                        ApprovedAt = GetNullableDateTime(source, "approvedAt"),
                        EmployeeCode = GetString(source, "employeeCodeSnapshot") ?? string.Empty,
                        EmployeeName = GetString(source, "employeeNameSnapshot"),
                        ProjectCode = GetString(source, "projectCode") ?? string.Empty,
                        ProjectName = GetString(source, "projectName") ?? string.Empty,
                        BonusAmount = GetDecimal(source, "bonusAmount"),
                        Taxable = GetBool(source, "taxable"),
                        InsuranceContributable = GetBool(source, "insuranceContributable"),
                        Reason = GetString(source, "reason"),
                        Note = GetString(source, "note")
                    });
                }

                return result;
            }
            catch (JsonException)
            {
                return new List<ProjectBonusPayrollSourceDto>();
            }
        }

        private static string? GetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
                ? property.GetString()
                : null;
        }

        private static int GetInt(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value)
                ? value
                : 0;
        }

        private static int? GetNullableInt(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var property) &&
                   property.ValueKind != JsonValueKind.Null &&
                   property.TryGetInt32(out var value)
                ? value
                : null;
        }

        private static decimal GetDecimal(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var property) && property.TryGetDecimal(out var value)
                ? value
                : 0;
        }

        private static bool GetBool(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var property) &&
                   property.ValueKind == JsonValueKind.True;
        }

        private static DateTime? GetNullableDateTime(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var property) &&
                   property.ValueKind != JsonValueKind.Null &&
                   property.TryGetDateTime(out var value)
                ? value
                : null;
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
