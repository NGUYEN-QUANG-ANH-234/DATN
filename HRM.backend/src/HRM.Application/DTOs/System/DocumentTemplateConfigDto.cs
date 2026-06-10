using System.ComponentModel.DataAnnotations;

namespace HRM.backend.src.HRM.Application.DTOs.System
{
    public class DocumentTemplateConfigDto
    {
        [Required]
        public string TemplateKey { get; set; } = string.Empty;

        [Required]
        public string DisplayName { get; set; } = string.Empty;

        public string Category { get; set; } = "Biểu mẫu";
        public string DocumentTitle { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public string NumberPrefix { get; set; } = "DOC";
        public string SignerTitle { get; set; } = "Người lập";
        public string DataScope { get; set; } = "SELF";
        public List<string> AllowedRoles { get; set; } = new();
        public List<string> AllowedOutputs { get; set; } = new() { "HTML", "DOC" };
        public List<DocumentTemplateFieldDto> Fields { get; set; } = new();
        public string HeaderHtml { get; set; } = string.Empty;
        public string BodyHtml { get; set; } = string.Empty;
        public string FooterHtml { get; set; } = string.Empty;
        public DocumentTemplateLayoutDto Layout { get; set; } = new();
    }

    public class DocumentTemplateLayoutDto
    {
        public string PageSize { get; set; } = "A4";
        public string Orientation { get; set; } = "portrait";
        public string Margin { get; set; } = "20mm";
        public string FontFamily { get; set; } = "Times New Roman";
        public string FontSize { get; set; } = "12pt";
    }

    public class DocumentTemplateFieldDto
    {
        [Required]
        [RegularExpression(@"^[a-z][a-z0-9_]{1,79}$", ErrorMessage = "Mã field phải bắt đầu bằng chữ thường và chỉ gồm chữ thường, số, gạch dưới.")]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string Label { get; set; } = string.Empty;

        public string BindingType { get; set; } = "Manual";
        public string? SourcePath { get; set; }
        public string? ResolverKey { get; set; }
        public string DataType { get; set; } = "Text";
        public bool Required { get; set; }
        public string? DefaultValue { get; set; }
        public List<string> Options { get; set; } = new();
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public string Placeholder => $"{{{Code}}}";
    }

    public class DocumentFieldCatalogDto
    {
        public string Code { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string SourcePath { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string DataType { get; set; } = "Text";
        public bool IsActive { get; set; } = true;
    }

    public class DocumentTemplateValidationResultDto
    {
        public List<string> InvalidPlaceholders { get; set; } = new();
        public List<string> MissingFields { get; set; } = new();
        public List<string> UnusedFields { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public bool IsValid => InvalidPlaceholders.Count == 0 && MissingFields.Count == 0;
    }

    public class DocumentTemplatePreviewRequestDto
    {
        public DocumentTemplateConfigDto TemplateConfig { get; set; } = new();
        public string PreviewMode { get; set; } = "Sample";
        public int? EmployeeId { get; set; }
        public Dictionary<string, string> ManualValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class DocumentTemplatePreviewResultDto
    {
        public string Html { get; set; } = string.Empty;
        public Dictionary<string, string> ResolvedValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> MissingFields { get; set; } = new();
        public List<string> InvalidPlaceholders { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class DocumentFormTemplateSummaryDto
    {
        public string TemplateKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string DocumentTitle { get; set; } = string.Empty;
        public string NumberPrefix { get; set; } = string.Empty;
        public string DataScope { get; set; } = "SELF";
    }

    public class DocumentFormFieldPrepareDto
    {
        public string Code { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string BindingType { get; set; } = "Manual";
        public string DataType { get; set; } = "Text";
        public bool Required { get; set; }
        public string Value { get; set; } = string.Empty;
        public bool ReadOnly { get; set; }
        public List<string> Options { get; set; } = new();
        public int SortOrder { get; set; }
    }

    public class DocumentFormPrepareResultDto
    {
        public string TemplateKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string DocumentTitle { get; set; } = string.Empty;
        public string NumberPrefix { get; set; } = string.Empty;
        public List<DocumentFormFieldPrepareDto> Fields { get; set; } = new();
        public string PreviewHtml { get; set; } = string.Empty;
        public Dictionary<string, string> ResolvedValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Warnings { get; set; } = new();
    }

    public class DocumentFormGenerateRequestDto
    {
        public int? EmployeeId { get; set; }
        public string OutputType { get; set; } = "HTML";
        public Dictionary<string, string> ManualValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class DocumentFormGenerateResultDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "text/html; charset=utf-8";
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, string> ResolvedValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Warnings { get; set; } = new();
    }

    public class DocumentActorContextDto
    {
        public int AccountId { get; set; }
        public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
    }
}
