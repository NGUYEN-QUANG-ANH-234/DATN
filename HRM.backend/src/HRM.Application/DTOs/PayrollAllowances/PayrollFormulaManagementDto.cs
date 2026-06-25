using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.DTOs.PayrollAllowances
{
    public class PayrollFormulaLineDto
    {
        public int Id { get; set; }
        public int? SalaryComponentTypeId { get; set; }
        public string ComponentCode { get; set; } = string.Empty;
        public string? ComponentName { get; set; }
        public string Expression { get; set; } = string.Empty;
        public int CalculationOrder { get; set; }
        public bool IsGrossComponent { get; set; }
        public bool IsTaxable { get; set; }
        public bool IsInsuranceBased { get; set; }
        public bool IsDeduction { get; set; }
        public bool IsSnapshotRequired { get; set; } = true;
        public string? Note { get; set; }
    }

    public class PayrollFormulaDto
    {
        public int Id { get; set; }
        public string FormulaCode { get; set; } = string.Empty;
        public string FormulaName { get; set; } = string.Empty;
        public string? Expression { get; set; }
        public bool IsActive { get; set; }
        public ContractType? ContractType { get; set; }
        public PayBasis? PayBasis { get; set; }
        public EmployeeType? EmployeeType { get; set; }
        public int? DeptId { get; set; }
        public int? PositionId { get; set; }
        public int? JobLevelId { get; set; }
        public int Version { get; set; }
        public string? VersionCode { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public FormulaStatus Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public DateTime? DeadlineAt { get; set; }
        public int? CreatedByAccountId { get; set; }
        public string? CreatedByName { get; set; }
        public int? SubmittedByAccountId { get; set; }
        public string? SubmittedByName { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int? ApprovedByAccountId { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? ActivatedByAccountId { get; set; }
        public string? ActivatedByName { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public int? ArchivedByAccountId { get; set; }
        public string? ArchivedByName { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public string? RejectReason { get; set; }
        public string? ReviewNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<PayrollFormulaLineDto> Lines { get; set; } = new();
    }

    public class UpsertPayrollFormulaDto
    {
        public string FormulaCode { get; set; } = string.Empty;
        public string FormulaName { get; set; } = string.Empty;
        public string? Expression { get; set; }
        public ContractType? ContractType { get; set; }
        public PayBasis? PayBasis { get; set; }
        public EmployeeType? EmployeeType { get; set; }
        public int? DeptId { get; set; }
        public int? PositionId { get; set; }
        public int? JobLevelId { get; set; }
        public string? VersionCode { get; set; }
        public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow.Date;
        public DateTime? EffectiveTo { get; set; }
        public List<PayrollFormulaLineDto> Lines { get; set; } = new();
    }

    public class PayrollFormulaReviewDto
    {
        public bool IsApproved { get; set; }
        public bool RequestRevision { get; set; }
        public string? Note { get; set; }
    }

    public class PayrollFormulaActionNoteDto
    {
        public string? Note { get; set; }
    }

    public class PayrollFormulaValidationResultDto
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class PayrollFormulaVariableDto
    {
        public string Code { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Module { get; set; }
        public string? DataType { get; set; }
        public string? AggregationType { get; set; }
        public bool IsPeriodBased { get; set; }
        public bool IsActive { get; set; }
    }
}
