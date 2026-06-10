using HRM.backend.src.HRM.Core.Enums;
using Microsoft.AspNetCore.Http;

namespace HRM.backend.src.HRM.Application.DTOs.PayrollAllowances
{
    public class ProjectBonusImportRequestDto
    {
        public IFormFile File { get; set; } = null!;
        public byte PeriodMonth { get; set; }
        public short PeriodYear { get; set; }
        public bool Overwrite { get; set; }
        public string? Note { get; set; }
    }

    public class ReviewProjectBonusImportDto
    {
        public bool IsApproved { get; set; }
        public string? Note { get; set; }
    }

    public class CancelProjectBonusImportDto
    {
        public string? Note { get; set; }
    }

    public class ProjectBonusImportPreviewDto
    {
        public byte PeriodMonth { get; set; }
        public short PeriodYear { get; set; }
        public string PayrollPeriod { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public bool Overwrite { get; set; }
        public bool CanSave { get; set; }
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int ErrorRows { get; set; }
        public decimal TotalAmount { get; set; }
        public List<string> GlobalErrors { get; set; } = new();
        public List<ProjectBonusImportLineDto> Lines { get; set; } = new();
    }

    public class ProjectBonusImportBatchDto
    {
        public int Id { get; set; }
        public byte PeriodMonth { get; set; }
        public short PeriodYear { get; set; }
        public string PayrollPeriod { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public ProjectBonusImportStatus Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int ErrorRows { get; set; }
        public decimal TotalAmount { get; set; }
        public int UploadedByAccountId { get; set; }
        public string? UploadedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ApprovedByAccountId { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? Note { get; set; }
        public List<ProjectBonusImportLineDto> Lines { get; set; } = new();
    }

    public class ProjectBonusImportLineDto
    {
        public int Id { get; set; }
        public int RowNumber { get; set; }
        public int? EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string? EmployeeName { get; set; }
        public string ProjectCode { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public decimal BonusAmount { get; set; }
        public bool Taxable { get; set; }
        public bool InsuranceContributable { get; set; }
        public string? Reason { get; set; }
        public string? Note { get; set; }
        public ProjectBonusLineValidationStatus ValidationStatus { get; set; }
        public bool IsValid => ValidationStatus == ProjectBonusLineValidationStatus.Valid;
        public string? ErrorMessage { get; set; }
    }
}
