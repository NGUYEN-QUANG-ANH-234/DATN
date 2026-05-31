using System.Text.RegularExpressions;
using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;
using HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Services;

namespace HRM.backend.src.HRM.Application.Services.PayrollAllowances
{
    public class PayrollFormulaValidator : IPayrollFormulaValidator
    {
        private static readonly Regex IdentifierRegex = new(@"\b[A-Za-z_][A-Za-z0-9_]*\b", RegexOptions.Compiled);
        private static readonly string[] ForbiddenTokens =
        {
            ";", "\"", "'", "[", "]", "{", "}", "=>", "new ", "typeof", "System", "DateTime", "File", "Process", "Environment"
        };

        private static readonly HashSet<string> AllowedFunctions = new(StringComparer.OrdinalIgnoreCase)
        {
            "min", "max", "round", "abs", "pit"
        };

        public void Validate(PayrollCalculationSource source)
        {
            if (source.Formula.Lines.Count == 0)
                throw new InvalidOperationException($"Formula {source.Formula.FormulaCode} chưa có dòng tính lương.");

            var duplicatedComponent = source.Formula.Lines
                .GroupBy(l => l.ComponentCode, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicatedComponent != null)
                throw new InvalidOperationException($"Formula {source.Formula.FormulaCode} bi trung component {duplicatedComponent.Key}.");

            var allowedIdentifiers = new HashSet<string>(source.Variables.Keys, StringComparer.OrdinalIgnoreCase);
            foreach (var function in AllowedFunctions) allowedIdentifiers.Add(function);
            foreach (var line in source.Formula.Lines)
            {
                allowedIdentifiers.Add(SafeVariableName(line.ComponentCode));
                allowedIdentifiers.Add($"component_{SafeVariableName(line.ComponentCode)}");
            }

            foreach (var line in source.Formula.Lines.OrderBy(l => l.CalculationOrder))
            {
                if (string.IsNullOrWhiteSpace(line.ComponentCode))
                    throw new InvalidOperationException("Formula line thieu ComponentCode.");
                if (string.IsNullOrWhiteSpace(line.Expression))
                    throw new InvalidOperationException($"Formula line {line.ComponentCode} thieu Expression.");
                if (line.Expression.Length > 1000)
                    throw new InvalidOperationException($"Formula line {line.ComponentCode} qua dai.");

                foreach (var token in ForbiddenTokens)
                {
                    if (line.Expression.Contains(token, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"Formula line {line.ComponentCode} có token không được phép: {token.Trim()}.");
                }

                var identifiers = IdentifierRegex.Matches(line.Expression)
                    .Select(m => m.Value)
                    .Where(v => !decimal.TryParse(v, out _))
                    .Where(v => !string.Equals(v, "true", StringComparison.OrdinalIgnoreCase))
                    .Where(v => !string.Equals(v, "false", StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var identifier in identifiers)
                {
                    if (!allowedIdentifiers.Contains(identifier))
                        throw new InvalidOperationException($"Formula line {line.ComponentCode} dùng biến chưa được phép: {identifier}.");
                }
            }
        }

        private static string SafeVariableName(string value)
        {
            var chars = value.Trim().ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
            return new string(chars);
        }
    }
}
