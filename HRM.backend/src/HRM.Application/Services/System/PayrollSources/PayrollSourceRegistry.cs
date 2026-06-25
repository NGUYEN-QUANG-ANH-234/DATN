using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PayrollAllowances;
using HRM.backend.src.HRM.Core.Models.System;
using static HRM.backend.src.HRM.Application.Services.System.PayrollSources.PayrollSourceDefinitionFactory;

namespace HRM.backend.src.HRM.Application.Services.System.PayrollSources
{
    public class PayrollSourceRegistry : IPayrollSourceRegistry
    {
        private readonly IEnumerable<IPayrollSourceProvider> _providers;
        private readonly IPayrollRepository _payrollRepository;

        public PayrollSourceRegistry(
            IEnumerable<IPayrollSourceProvider> providers,
            IPayrollRepository payrollRepository)
        {
            _providers = providers;
            _payrollRepository = payrollRepository;
        }

        public async Task<IReadOnlyCollection<PayrollSourceDefinition>> GetSourcesAsync(CancellationToken ct = default)
        {
            var staticSources = _providers.SelectMany(provider => provider.GetSources());
            var componentSources = await BuildSalaryComponentSourcesAsync(ct);

            return staticSources
                .Concat(componentSources)
                .GroupBy(source => source.Code, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(source => source.Module)
                .ThenBy(source => source.DisplayName)
                .ToList();
        }

        private async Task<IReadOnlyCollection<PayrollSourceDefinition>> BuildSalaryComponentSourcesAsync(CancellationToken ct)
        {
            var componentTypes = await _payrollRepository.GetActiveSalaryComponentTypesAsync(DateTime.UtcNow, ct);
            return componentTypes
                .Where(IsEmployeeInputComponent)
                .Select(type => Source(
                    SafeVariableName(type.Code),
                    type.Name,
                    "Thành phần lương",
                    ResolveComponentDataType(type),
                    SalaryAggregationType.Sum,
                    true))
                .ToList();
        }

        private static bool IsEmployeeInputComponent(SalaryComponentType type)
        {
            return type.CalculationMethod != CalculationMethod.Formula;
        }

        private static SalaryVariableDataType ResolveComponentDataType(SalaryComponentType type)
        {
            return type.CalculationMethod == CalculationMethod.PercentOfBaseSalary
                ? SalaryVariableDataType.Percent
                : SalaryVariableDataType.Money;
        }

        private static string SafeVariableName(string value)
        {
            var chars = value.Trim().ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
            return new string(chars);
        }
    }

    public class ContractPayrollSourceProvider : IPayrollSourceProvider
    {
        public IReadOnlyCollection<PayrollSourceDefinition> GetSources() => new List<PayrollSourceDefinition>
        {
            Source("base_salary", "Lương cơ bản theo hợp đồng", "Hợp đồng", SalaryVariableDataType.Money),
            Source("salary_percentage", "Tỷ lệ hưởng lương theo hợp đồng", "Hợp đồng", SalaryVariableDataType.Percent),
            Source("monthly_base_salary", "Lương cơ bản tháng sau tỷ lệ", "Hợp đồng", SalaryVariableDataType.Money),
            Source("standard_workdays", "Số ngày công chuẩn", "Hợp đồng", SalaryVariableDataType.Days),
            Source("standard_hours_per_day", "Số giờ làm chuẩn mỗi ngày", "Hợp đồng", SalaryVariableDataType.Hours),
            Source("standard_working_hours", "Tổng số giờ làm chuẩn", "Hợp đồng", SalaryVariableDataType.Hours),
            Source("hourly_rate", "Đơn giá theo giờ", "Hợp đồng", SalaryVariableDataType.Money),
            Source("daily_rate", "Đơn giá theo ngày", "Hợp đồng", SalaryVariableDataType.Money),
            Source("contract_segment_count", "Số đoạn hợp đồng trong kỳ", "Hợp đồng", SalaryVariableDataType.Number, SalaryAggregationType.Count, true),
            Source("contract_segment_salary_amount", "Lương theo đoạn hợp đồng trong kỳ", "Hợp đồng", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true),
            Source("contract_segment_taxable_amount", "Thu nhập chịu thuế theo đoạn hợp đồng", "Hợp đồng", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true),
            Source("contract_segment_insurance_base_amount", "Mức đóng bảo hiểm theo đoạn hợp đồng", "Hợp đồng", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true),
            Source("contract_type_id", "Mã loại hợp đồng", "Hợp đồng", SalaryVariableDataType.Number),
            Source("pay_basis_id", "Mã hình thức trả lương", "Hợp đồng", SalaryVariableDataType.Number),
            Source("tax_method_id", "Mã phương pháp tính thuế", "Hợp đồng", SalaryVariableDataType.Number)
        };
    }

    public class AttendancePayrollSourceProvider : IPayrollSourceProvider
    {
        public IReadOnlyCollection<PayrollSourceDefinition> GetSources() => new List<PayrollSourceDefinition>
        {
            Source("actual_workdays", "Số ngày công thực trả", "Chấm công", SalaryVariableDataType.Days, SalaryAggregationType.MonthlyTotal, true),
            Source("actual_attendance_days", "Số ngày có chấm công thực tế", "Chấm công", SalaryVariableDataType.Days, SalaryAggregationType.MonthlyTotal, true),
            Source("actual_work_hours", "Số giờ làm thực tế", "Chấm công", SalaryVariableDataType.Hours, SalaryAggregationType.MonthlyTotal, true),
            Source("payable_work_hours", "Số giờ công được trả lương", "Chấm công", SalaryVariableDataType.Hours, SalaryAggregationType.MonthlyTotal, true),
            Source("worked_minutes", "Tổng phút làm việc", "Chấm công", SalaryVariableDataType.Number, SalaryAggregationType.MonthlyTotal, true),
            Source("late_minutes", "Tổng phút đi muộn", "Chấm công", SalaryVariableDataType.Number, SalaryAggregationType.MonthlyTotal, true),
            Source("early_leave_minutes", "Tổng phút về sớm", "Chấm công", SalaryVariableDataType.Number, SalaryAggregationType.MonthlyTotal, true),
            Source("unpaid_leave_workdays", "Số ngày nghỉ không lương", "Chấm công", SalaryVariableDataType.Days, SalaryAggregationType.MonthlyTotal, true),
            Source("paid_leave_workdays", "Số ngày nghỉ có lương", "Chấm công", SalaryVariableDataType.Days, SalaryAggregationType.MonthlyTotal, true),
            Source("maternity_leave_days", "Số ngày nghỉ thai sản", "Chấm công", SalaryVariableDataType.Days, SalaryAggregationType.MonthlyTotal, true),
            Source("sick_leave_days", "Số ngày nghỉ ốm", "Chấm công", SalaryVariableDataType.Days, SalaryAggregationType.MonthlyTotal, true)
        };
    }

    public class OvertimePayrollSourceProvider : IPayrollSourceProvider
    {
        public IReadOnlyCollection<PayrollSourceDefinition> GetSources() => new List<PayrollSourceDefinition>
        {
            Source("overtime_minutes", "Tổng phút tăng ca", "Tăng ca", SalaryVariableDataType.Number, SalaryAggregationType.MonthlyTotal, true),
            Source("overtime_hours", "Tổng giờ tăng ca", "Tăng ca", SalaryVariableDataType.Hours, SalaryAggregationType.MonthlyTotal, true),
            Source("overtime_base_amount", "Tiền gốc tăng ca", "Tăng ca", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true),
            Source("overtime_premium_amount", "Tiền phần hệ số tăng ca", "Tăng ca", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true),
            Source("external_timesheet_hours", "Số giờ cộng tác viên đã duyệt", "Lương", SalaryVariableDataType.Hours, SalaryAggregationType.MonthlyTotal, true),
            Source("external_timesheet_amount", "Thu nhập cộng tác viên đã duyệt", "Lương", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true)
        };
    }

    public class PerformancePayrollSourceProvider : IPayrollSourceProvider
    {
        public IReadOnlyCollection<PayrollSourceDefinition> GetSources() => new List<PayrollSourceDefinition>
        {
            Source("kpi_score", "Điểm KPI", "Hiệu suất", SalaryVariableDataType.Number, SalaryAggregationType.Latest, true),
            Source("kpi_bonus_amount", "Mức thưởng KPI tối đa", "Hiệu suất", SalaryVariableDataType.Money, SalaryAggregationType.Latest, true)
        };
    }

    public class ProjectBonusPayrollSourceProvider : IPayrollSourceProvider
    {
        public IReadOnlyCollection<PayrollSourceDefinition> GetSources() => new List<PayrollSourceDefinition>
        {
            Source("intern_allowance_amount", "Trợ cấp thực tập", "Lương", SalaryVariableDataType.Money, SalaryAggregationType.Latest, true),
            Source("project_bonus_amount", "Thưởng dự án", "Lương", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true)
        };
    }

    public class TaxInsurancePayrollSourceProvider : IPayrollSourceProvider
    {
        public IReadOnlyCollection<PayrollSourceDefinition> GetSources() => new List<PayrollSourceDefinition>
        {
            Source("dependent_count", "Số người phụ thuộc", "Thuế và bảo hiểm", SalaryVariableDataType.Number, SalaryAggregationType.Count, true),
            Source("personal_deduction", "Giảm trừ bản thân", "Thuế và bảo hiểm", SalaryVariableDataType.Money),
            Source("dependent_deduction", "Giảm trừ người phụ thuộc", "Thuế và bảo hiểm", SalaryVariableDataType.Money, SalaryAggregationType.Latest, true),
            Source("flat_tax_threshold", "Ngưỡng tính thuế khoán", "Thuế và bảo hiểm", SalaryVariableDataType.Money),
            Source("flat_tax_rate", "Tỷ lệ thuế khoán", "Thuế và bảo hiểm", SalaryVariableDataType.Percent),
            Source("non_resident_tax_rate", "Tỷ lệ thuế người không cư trú", "Thuế và bảo hiểm", SalaryVariableDataType.Percent),
            Source("employee_insurance_rate", "Tỷ lệ bảo hiểm người lao động đóng", "Thuế và bảo hiểm", SalaryVariableDataType.Percent, SalaryAggregationType.Latest, true),
            Source("employer_contribution_rate", "Tỷ lệ đóng góp của doanh nghiệp", "Thuế và bảo hiểm", SalaryVariableDataType.Percent, SalaryAggregationType.Latest, true),
            Source("insurance_contribution_enabled", "Có đóng bảo hiểm trong kỳ", "Thuế và bảo hiểm", SalaryVariableDataType.Number, SalaryAggregationType.Latest, true),
            Source("unemployment_insurance_contribution_enabled", "Có đóng bảo hiểm thất nghiệp trong kỳ", "Thuế và bảo hiểm", SalaryVariableDataType.Number, SalaryAggregationType.Latest, true),
            Source("gross_income", "Tổng thu nhập", "Thuế và bảo hiểm", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true),
            Source("insurance_salary", "Lương làm căn cứ bảo hiểm", "Thuế và bảo hiểm", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true),
            Source("employee_insurance_amount", "Tiền bảo hiểm người lao động đóng", "Thuế và bảo hiểm", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true),
            Source("employer_contribution_amount", "Tiền doanh nghiệp đóng", "Thuế và bảo hiểm", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true),
            Source("taxable_gross_income", "Tổng thu nhập chịu thuế", "Thuế và bảo hiểm", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true),
            Source("taxable_income", "Thu nhập tính thuế", "Thuế và bảo hiểm", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true),
            Source("pit_tax_base", "Cơ sở tính thuế TNCN", "Thuế và bảo hiểm", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true),
            Source("pit_amount", "Thuế TNCN", "Thuế và bảo hiểm", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true),
            Source("net_salary", "Lương thực nhận", "Thuế và bảo hiểm", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true)
        };
    }

    public class AdjustmentPayrollSourceProvider : IPayrollSourceProvider
    {
        public IReadOnlyCollection<PayrollSourceDefinition> GetSources() => new List<PayrollSourceDefinition>
        {
            Source("position_allowance", "Phụ cấp chức vụ", "Điều chỉnh lương", SalaryVariableDataType.Money),
            Source("responsibility_allowance", "Phụ cấp trách nhiệm", "Điều chỉnh lương", SalaryVariableDataType.Money),
            Source("meal_allowance_per_day", "Phụ cấp ăn ca theo ngày", "Điều chỉnh lương", SalaryVariableDataType.Money),
            Source("payroll_adjustment_taxable_insurance", "Điều chỉnh chịu thuế và tính bảo hiểm", "Điều chỉnh lương", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true),
            Source("payroll_adjustment_taxable", "Điều chỉnh chịu thuế", "Điều chỉnh lương", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true),
            Source("payroll_adjustment_nontaxable", "Điều chỉnh không chịu thuế", "Điều chỉnh lương", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true),
            Source("payroll_adjustment_deduction", "Khoản khấu trừ điều chỉnh", "Điều chỉnh lương", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true),
            Source("legacy_insurance_allowance", "Phụ cấp cũ tính bảo hiểm", "Điều chỉnh lương", SalaryVariableDataType.Money, SalaryAggregationType.Sum, true),
            Source("legacy_taxable_allowance", "Phụ cấp cũ chịu thuế", "Điều chỉnh lương", SalaryVariableDataType.Money, SalaryAggregationType.Sum, true),
            Source("legacy_nontaxable_allowance", "Phụ cấp cũ không chịu thuế", "Điều chỉnh lương", SalaryVariableDataType.Money, SalaryAggregationType.Sum, true),
            Source("other_deductions", "Các khoản khấu trừ khác", "Điều chỉnh lương", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true),
            Source("job_level_id", "Mã cấp bậc công việc", "Điều chỉnh lương", SalaryVariableDataType.Number)
        };
    }

    public class SeniorityPayrollSourceProvider : IPayrollSourceProvider
    {
        public IReadOnlyCollection<PayrollSourceDefinition> GetSources() => new List<PayrollSourceDefinition>
        {
            Source("service_months", "Số tháng thâm niên", "Thâm niên", SalaryVariableDataType.Number, SalaryAggregationType.Latest, true),
            Source("service_years", "Số năm thâm niên", "Thâm niên", SalaryVariableDataType.Number, SalaryAggregationType.Latest, true),
            Source("seniority_allowance", "Phụ cấp thâm niên", "Thâm niên", SalaryVariableDataType.Money, SalaryAggregationType.Latest, true),
            Source("seniority_allowance_prorated", "Phụ cấp thâm niên theo kỳ", "Thâm niên", SalaryVariableDataType.Money, SalaryAggregationType.MonthlyTotal, true),
            Source("seniority_rate", "Tỷ lệ thâm niên", "Thâm niên", SalaryVariableDataType.Percent, SalaryAggregationType.Latest, true)
        };
    }

    internal static class PayrollSourceDefinitionFactory
    {
        public static PayrollSourceDefinition Source(
            string code,
            string displayName,
            string module,
            SalaryVariableDataType dataType,
            SalaryAggregationType aggregationType = SalaryAggregationType.Latest,
            bool isPeriodBased = false)
        {
            return new PayrollSourceDefinition
            {
                Code = code,
                DisplayName = displayName,
                Module = module,
                DataType = dataType,
                AggregationType = aggregationType,
                IsPeriodBased = isPeriodBased
            };
        }
    }
}
