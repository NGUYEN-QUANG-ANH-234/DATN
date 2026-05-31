using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.DTOs.System
{
    public class PayrollPolicyDto
    {
        public int Id { get; set; }
        public PayrollPolicyType PolicyType { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public PayrollPolicyValueType ValueType { get; set; }
        public decimal? RatePercent { get; set; }
        public decimal? Amount { get; set; }
        public decimal? FromAmount { get; set; }
        public decimal? ToAmount { get; set; }
        public decimal? QuickDeduction { get; set; }
        public string? FormulaJson { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public int Version { get; set; }
        public bool IsActive { get; set; }
        public string? Description { get; set; }
    }

    public class PayrollPolicyFilterDto
    {
        public PayrollPolicyType? PolicyType { get; set; }
        public bool IncludeInactive { get; set; }
    }

    public class CreatePayrollPolicyDto
    {
        public PayrollPolicyType PolicyType { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public PayrollPolicyValueType ValueType { get; set; } = PayrollPolicyValueType.RatePercent;
        public decimal? RatePercent { get; set; }
        public decimal? Amount { get; set; }
        public decimal? FromAmount { get; set; }
        public decimal? ToAmount { get; set; }
        public decimal? QuickDeduction { get; set; }
        public string? FormulaJson { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public int Version { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public string? Description { get; set; }
    }

    public class UpdatePayrollPolicyDto : CreatePayrollPolicyDto
    {
    }
}
