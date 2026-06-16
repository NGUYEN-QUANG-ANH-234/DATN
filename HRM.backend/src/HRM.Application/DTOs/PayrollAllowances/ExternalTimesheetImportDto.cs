using HRM.backend.src.HRM.Core.Enums;
using Microsoft.AspNetCore.Http;

namespace HRM.backend.src.HRM.Application.DTOs.PayrollAllowances
{
    public class ExternalTimesheetImportRequestDto
    {
        public IFormFile File { get; set; } = null!;
        public byte ImportMonth { get; set; }
        public short ImportYear { get; set; }
        public string? SourceSystem { get; set; }
        public bool Overwrite { get; set; }
        public string? Note { get; set; }
    }

    public class ReviewExternalTimesheetImportDto
    {
        public bool IsApproved { get; set; }
        public string? Note { get; set; }
    }

    public class CancelExternalTimesheetImportDto
    {
        public string? Note { get; set; }
    }

    public class ExternalTimesheetImportPreviewDto
    {
        public byte ImportMonth { get; set; }
        public short ImportYear { get; set; }
        public string PayrollPeriod { get; set; } = string.Empty;
        public string SourceSystem { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public bool Overwrite { get; set; }
        public bool CanSave { get; set; }
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int ErrorRows { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
        public List<string> GlobalErrors { get; set; } = new();
        public List<ExternalTimesheetImportLineDto> Lines { get; set; } = new();
    }

    public class ExternalTimesheetImportBatchDto
    {
        public int Id { get; set; }
        public byte ImportMonth { get; set; }
        public short ImportYear { get; set; }
        public string PayrollPeriod { get; set; } = string.Empty;
        public string SourceSystem { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public ExternalTimesheetImportStatus Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int ErrorRows { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
        public int ImportedByAccountId { get; set; }
        public string? ImportedByName { get; set; }
        public DateTime ImportedAt { get; set; }
        public int? ApprovedByAccountId { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? Note { get; set; }
        public List<ExternalTimesheetImportLineDto> Lines { get; set; } = new();
    }

    public class ExternalTimesheetImportLineDto
    {
        public int Id { get; set; }
        public int RowNumber { get; set; }
        public int? CollaboratorEmployeeId { get; set; }
        public string CollaboratorCode { get; set; } = string.Empty;
        public string? CollaboratorName { get; set; }
        public DateTime? WorkDate { get; set; }
        public string WorkDateText { get; set; } = string.Empty;
        public string ProjectCode { get; set; } = string.Empty;
        public string TaskCode { get; set; } = string.Empty;
        public decimal ApprovedHours { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
        public ProjectBonusLineValidationStatus ValidationStatus { get; set; }
        public bool IsValid => ValidationStatus == ProjectBonusLineValidationStatus.Valid;
        public string? ErrorMessage { get; set; }
    }
}
