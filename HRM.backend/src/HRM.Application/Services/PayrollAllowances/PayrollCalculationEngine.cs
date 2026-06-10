using DynamicExpresso;
using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;
using HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Services;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.Services.PayrollAllowances
{
    public class PayrollCalculationEngine : IPayrollCalculationEngine
    {
        public PayrollCalculationOutput Calculate(PayrollCalculationSource source)
        {
            var variables = new Dictionary<string, decimal>(source.Variables, StringComparer.OrdinalIgnoreCase);
            var output = new PayrollCalculationOutput();

            foreach (var line in source.Formula.Lines.OrderBy(l => l.CalculationOrder).ThenBy(l => l.Id))
            {
                RefreshTotals(source, output, variables);
                var raw = EvaluateDecimal(line.Expression, variables, source);
                var amount = line.IsDeduction ? -Math.Abs(raw) : raw;
                var incomeAmount = !line.IsDeduction && line.IsGrossComponent ? Math.Max(0, amount) : 0;
                var taxableAmount = line.IsTaxable ? RoundMoney(incomeAmount) : 0;
                var insuranceBaseAmount = line.IsInsuranceBased ? RoundMoney(incomeAmount) : 0;
                if (IsCode(line.ComponentCode, "PROJECT_BONUS"))
                {
                    taxableAmount = RoundMoney(Math.Max(0, variables.GetValueOrDefault("project_bonus_taxable_amount")));
                    insuranceBaseAmount = RoundMoney(Math.Max(0, variables.GetValueOrDefault("project_bonus_insurance_base_amount")));
                }

                var result = new PayrollLineResult
                {
                    FormulaLine = line,
                    ComponentCode = line.ComponentCode,
                    ComponentName = ResolveComponentName(line.ComponentCode, line.SalaryComponentType?.Name),
                    ComponentGroup = line.SalaryComponentType?.ComponentGroup.ToString(),
                    RawAmount = RoundMoney(raw),
                    Amount = RoundMoney(amount),
                    TaxableAmount = taxableAmount,
                    InsuranceBaseAmount = insuranceBaseAmount,
                    IsIncome = !line.IsDeduction && line.IsGrossComponent,
                    IsDeduction = line.IsDeduction,
                    IsTaxable = line.IsTaxable,
                    IsInsuranceBased = line.IsInsuranceBased,
                    ProrationType = line.SalaryComponentType?.ProrationType.ToString(),
                    CalculationMethod = line.SalaryComponentType?.CalculationMethod.ToString(),
                    Note = line.Note
                };

                output.Lines.Add(result);
                variables[SafeVariableName(line.ComponentCode)] = result.Amount;
                variables[$"component_{SafeVariableName(line.ComponentCode)}"] = result.Amount;
                RefreshTotals(source, output, variables);
                result.VariablesAfterLine = new Dictionary<string, decimal>(variables, StringComparer.OrdinalIgnoreCase);
            }

            RefreshTotals(source, output, variables);
            output.FinalVariables = new Dictionary<string, decimal>(variables, StringComparer.OrdinalIgnoreCase);
            return output;
        }

        private static decimal EvaluateDecimal(string expression, Dictionary<string, decimal> variables, PayrollCalculationSource source)
        {
            var interpreter = new Interpreter(InterpreterOptions.DefaultCaseInsensitive);
            foreach (var variable in variables)
            {
                interpreter.SetVariable(variable.Key, variable.Value);
            }

            interpreter.SetFunction("min", (Func<decimal, decimal, decimal>)Math.Min);
            interpreter.SetFunction("max", (Func<decimal, decimal, decimal>)Math.Max);
            interpreter.SetFunction("abs", (Func<decimal, decimal>)Math.Abs);
            interpreter.SetFunction("round", (Func<decimal, decimal>)(value => RoundMoney(value)));
            interpreter.SetFunction("pit", (Func<decimal, decimal>)(taxBase => CalculatePit(source, taxBase)));

            var result = interpreter.Eval(expression);
            return result switch
            {
                decimal d => RoundMoney(d),
                double d => RoundMoney((decimal)d),
                float f => RoundMoney((decimal)f),
                int i => i,
                long l => l,
                _ => RoundMoney(Convert.ToDecimal(result))
            };
        }

        private static void RefreshTotals(PayrollCalculationSource source, PayrollCalculationOutput output, Dictionary<string, decimal> variables)
        {
            var grossIncome = output.Lines.Where(l => l.IsIncome).Sum(l => l.Amount);
            var insuranceSalary = output.Lines.Where(l => l.IsIncome).Sum(l => l.InsuranceBaseAmount);
            var taxableGrossIncome = output.Lines.Where(l => l.IsIncome).Sum(l => l.TaxableAmount);
            var employeeInsurance = Math.Abs(output.Lines
                .Where(l => IsCode(l.ComponentCode, "EMPLOYEE_INSURANCE"))
                .Sum(l => l.Amount));
            var pit = Math.Abs(output.Lines
                .Where(l => IsCode(l.ComponentCode, "PIT"))
                .Sum(l => l.Amount));
            var otherDeductions = Math.Abs(output.Lines
                .Where(l => l.IsDeduction && !IsCode(l.ComponentCode, "EMPLOYEE_INSURANCE") && !IsCode(l.ComponentCode, "PIT"))
                .Sum(l => l.Amount));

            var personalDeduction = variables.GetValueOrDefault("personal_deduction");
            var dependentDeduction = variables.GetValueOrDefault("dependent_deduction");
            var taxableIncome = source.TaxMethod == TaxMethod.Progressive
                ? Math.Max(0, taxableGrossIncome - employeeInsurance - personalDeduction - dependentDeduction)
                : Math.Max(0, taxableGrossIncome);
            var pitTaxBase = source.TaxMethod switch
            {
                TaxMethod.Progressive => taxableIncome,
                TaxMethod.Flat10Percent => taxableGrossIncome,
                TaxMethod.NonResident20Percent => taxableGrossIncome,
                _ => 0
            };
            var employerContribution = insuranceSalary * variables.GetValueOrDefault("employer_contribution_rate");
            var netSalary = grossIncome - employeeInsurance - pit - otherDeductions;

            output.BaseSalaryActual = RoundMoney(output.Lines.Where(l => IsCode(l.ComponentCode, "BASE_SALARY_ACTUAL")).Sum(l => l.Amount));
            output.GrossIncome = RoundMoney(grossIncome);
            output.TotalAllowance = RoundMoney(output.Lines
                .Where(l => l.IsIncome && IsAllowanceCode(l.ComponentCode))
                .Sum(l => l.Amount));
            output.TotalBonus = RoundMoney(output.Lines
                .Where(l => l.IsIncome && IsBonusCode(l.ComponentCode))
                .Sum(l => l.Amount));
            output.InsuranceSalary = RoundMoney(insuranceSalary);
            output.EmployeeInsuranceAmount = RoundMoney(employeeInsurance);
            output.EmployerContributionAmount = RoundMoney(employerContribution);
            output.TaxableGrossIncome = RoundMoney(taxableGrossIncome);
            output.TaxableIncome = RoundMoney(taxableIncome);
            output.PitAmount = RoundMoney(pit);
            output.OtherDeductions = RoundMoney(otherDeductions);
            output.NetSalary = RoundMoney(netSalary);
            output.TotalCompanyCost = RoundMoney(grossIncome + employerContribution);

            variables["gross_income"] = output.GrossIncome;
            variables["insurance_salary"] = output.InsuranceSalary;
            variables["employee_insurance_amount"] = output.EmployeeInsuranceAmount;
            variables["employer_contribution_amount"] = output.EmployerContributionAmount;
            variables["taxable_gross_income"] = output.TaxableGrossIncome;
            variables["taxable_income"] = output.TaxableIncome;
            variables["pit_tax_base"] = RoundMoney(pitTaxBase);
            variables["pit_amount"] = output.PitAmount;
            variables["other_deductions"] = output.OtherDeductions;
            variables["net_salary"] = output.NetSalary;
        }

        private static decimal CalculatePit(PayrollCalculationSource source, decimal taxBase)
        {
            if (taxBase <= 0 || source.TaxMethod == TaxMethod.None) return 0;

            return source.TaxMethod switch
            {
                TaxMethod.Flat10Percent => taxBase < source.TaxConfig.FlatTaxThreshold ? 0 : RoundMoney(taxBase * source.TaxConfig.FlatTaxRate),
                TaxMethod.NonResident20Percent => RoundMoney(taxBase * source.TaxConfig.NonResidentTaxRate),
                TaxMethod.Progressive => CalculateProgressivePit(source, taxBase),
                _ => 0
            };
        }

        private static decimal CalculateProgressivePit(PayrollCalculationSource source, decimal taxableIncome)
        {
            var bracket = source.PitBrackets
                .OrderBy(b => b.Level)
                .LastOrDefault(b => taxableIncome >= b.MinIncome && (!b.MaxIncome.HasValue || taxableIncome <= b.MaxIncome.Value));
            if (bracket == null) return 0;
            return RoundMoney(Math.Max(0, taxableIncome * bracket.TaxRate - bracket.QuickDeduction));
        }

        private static bool IsCode(string value, string expected)
        {
            return string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAllowanceCode(string code)
        {
            return code.Contains("ALLOWANCE", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBonusCode(string code)
        {
            return code.Contains("BONUS", StringComparison.OrdinalIgnoreCase) ||
                   code.Contains("KPI", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveComponentName(string code, string? configuredName)
        {
            if (!string.IsNullOrWhiteSpace(configuredName)) return configuredName;
            return code switch
            {
                "BASE_SALARY_ACTUAL" => "Luong co ban theo cong",
                "POSITION_ALLOWANCE" => "Phu cap chuc vu",
                "RESPONSIBILITY_ALLOWANCE" => "Phu cap trach nhiem",
                "MEAL_ALLOWANCE" => "Phu cap an ca",
                "LEGACY_INSURANCE_ALLOWANCE" => "Phu cap cu tinh BH",
                "LEGACY_TAXABLE_ALLOWANCE" => "Phu cap cu chiu thue",
                "LEGACY_NONTAXABLE_ALLOWANCE" => "Phụ cấp cũ không chịu thuế",
                "KPI_BONUS" => "Thưởng KPI thực nhận",
                "PROJECT_BONUS" => "Thưởng dự án",
                "OT_BASE" => "OT phan 100% chiu thue",
                "OT_PREMIUM" => "OT phan he so tang them",
                "PAYROLL_ADJUSTMENT_TAXABLE_INSURANCE" => "Truy linh chiu thue va tinh bao hiem",
                "PAYROLL_ADJUSTMENT_TAXABLE" => "Truy linh chiu thue",
                "PAYROLL_ADJUSTMENT_NONTAXABLE" => "Truy lĩnh không chịu thuế",
                "PAYROLL_ADJUSTMENT_DEDUCTION" => "Truy thu/dieu chinh khau tru",
                "EMPLOYEE_INSURANCE" => "Bảo hiểm người lao động đóng",
                "PIT" => "Thue TNCN",
                _ => code
            };
        }

        private static string SafeVariableName(string value)
        {
            var chars = value.Trim().ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
            return new string(chars);
        }

        private static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
