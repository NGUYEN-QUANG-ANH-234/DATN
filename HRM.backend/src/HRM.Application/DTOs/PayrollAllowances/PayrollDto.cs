using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.DTOs.PayrollAllowances
{
    public class PayrollPeriodDto
    {
        public byte Month { get; set; }
        public short Year { get; set; }
    }

    public class PayrollCalculationResultDto
    {
        public byte Month { get; set; }
        public short Year { get; set; }
        public int CreatedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<SalarySlipDto> Payrolls { get; set; } = new();
    }

    public class CreatePayrollAdjustmentDto
    {
        public int EmployeeId { get; set; }
        public PayrollAdjustmentType AdjustmentType { get; set; } = PayrollAdjustmentType.ManualCorrection;
        public byte RecognizedMonth { get; set; }
        public short RecognizedYear { get; set; }
        public string? EffectiveFromMonth { get; set; }
        public string? EffectiveToMonth { get; set; }
        public decimal Amount { get; set; }
        public bool IsTaxable { get; set; } = true;
        public bool IsInsuranceBased { get; set; }
        public bool IsDeduction { get; set; }
        public required string Reason { get; set; }
    }

    public class PayrollAdjustmentDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public PayrollAdjustmentType AdjustmentType { get; set; }
        public byte RecognizedMonth { get; set; }
        public short RecognizedYear { get; set; }
        public string RecognizedPayrollPeriod { get; set; } = string.Empty;
        public string? EffectiveFromMonth { get; set; }
        public string? EffectiveToMonth { get; set; }
        public decimal Amount { get; set; }
        public bool IsTaxable { get; set; }
        public bool IsInsuranceBased { get; set; }
        public bool IsDeduction { get; set; }
        public PayrollAdjustmentStatus Status { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class SalarySlipDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public string? PositionName { get; set; }
        public byte Month { get; set; }
        public short Year { get; set; }
        public string Period { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }
        public decimal BaseSalaryActual { get; set; }
        public decimal StandardWorkDays { get; set; }
        public decimal StandardWorkHours { get; set; }
        public decimal ActualWorkDays { get; set; }
        public decimal ActualWorkHours { get; set; }
        public decimal PayableWorkHours { get; set; }
        public int WorkedMinutes { get; set; }
        public int LateMinutes { get; set; }
        public int EarlyLeaveMinutes { get; set; }
        public decimal UnpaidLeaveWorkdays { get; set; }
        public decimal ServiceMonths { get; set; }
        public decimal ServiceYears { get; set; }
        public decimal SeniorityAllowance { get; set; }
        public decimal SeniorityRate { get; set; }
        public int ActualOtMinutes { get; set; }
        public decimal GrossIncome { get; set; }
        public decimal InsuranceSalary { get; set; }
        public decimal EmployeeInsuranceAmount { get; set; }
        public decimal EmployerContributionAmount { get; set; }
        public decimal TaxableGrossIncome { get; set; }
        public decimal TaxableIncome { get; set; }
        public decimal PitAmount { get; set; }
        public decimal OtherDeductions { get; set; }
        public decimal NetSalary { get; set; }
        public decimal TotalCompanyCost { get; set; }
        public PayrollStatus Status { get; set; }
        public DateTime? CalculatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? LockedAt { get; set; }
        public string? ReviewNote { get; set; }
        public List<SalarySlipDetailDto> Details { get; set; } = new();
    }

    public class PayrollRunReviewDto
    {
        public bool IsApproved { get; set; }
        public bool RequestRevision { get; set; }
        public string? Note { get; set; }
    }

    public class PayrollRunSummaryDto
    {
        public byte Month { get; set; }
        public short Year { get; set; }
        public string Period { get; set; } = string.Empty;
        public PayrollStatus Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public int SlipCount { get; set; }
        public decimal GrossIncome { get; set; }
        public decimal NetSalary { get; set; }
        public decimal TotalCompanyCost { get; set; }
        public DateTime? CalculatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? LockedAt { get; set; }
        public int? SubmittedByAccountId { get; set; }
        public int? ApprovedByAccountId { get; set; }
        public int? LockedByAccountId { get; set; }
        public string? ReviewNote { get; set; }
        public List<SalarySlipDto> Slips { get; set; } = new();
    }

    public class SalarySlipDetailDto
    {
        public int Id { get; set; }
        public string ComponentCode { get; set; } = string.Empty;
        public string ComponentName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal InsuranceBaseAmount { get; set; }
        public bool IsIncome { get; set; }
        public bool IsDeduction { get; set; }
        public bool IsTaxable { get; set; }
        public bool IsInsuranceBased { get; set; }
        public string? Note { get; set; }
        public List<ProjectBonusPayrollSourceDto> ProjectBonusSources { get; set; } = new();
        public List<ExternalTimesheetPayrollSourceDto> ExternalTimesheetSources { get; set; } = new();
    }

    public class ProjectBonusPayrollSourceDto
    {
        public int Id { get; set; }
        public int BatchId { get; set; }
        public string? FileName { get; set; }
        public string? PayrollPeriod { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string? EmployeeName { get; set; }
        public string ProjectCode { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public decimal BonusAmount { get; set; }
        public bool Taxable { get; set; }
        public bool InsuranceContributable { get; set; }
        public string? Reason { get; set; }
        public string? Note { get; set; }
    }

    public class ExternalTimesheetPayrollSourceDto
    {
        public int Id { get; set; }
        public int ImportId { get; set; }
        public string? FileName { get; set; }
        public string? PayrollPeriod { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? CollaboratorEmployeeId { get; set; }
        public string CollaboratorCode { get; set; } = string.Empty;
        public string? CollaboratorName { get; set; }
        public DateTime WorkDate { get; set; }
        public string ProjectCode { get; set; } = string.Empty;
        public string TaskCode { get; set; } = string.Empty;
        public decimal ApprovedHours { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
    }

    public class SalarySlipExportRequestDto
    {
        public List<int> SlipIds { get; set; } = new();
        public string Format { get; set; } = "CSV";
    }

    public class SalarySlipExportResultDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "text/csv; charset=utf-8";
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }
}
