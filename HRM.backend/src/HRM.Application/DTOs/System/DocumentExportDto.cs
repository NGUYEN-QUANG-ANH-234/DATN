namespace HRM.backend.src.HRM.Application.DTOs.System
{
    public class DocumentTemplateSummaryDto
    {
        public string TemplateKey { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ActiveLayoutVersion { get; set; } = string.Empty;
        public List<string> AllowedOutputs { get; set; } = new();
        public List<DocumentLayoutVersionDto> LayoutVersions { get; set; } = new();
    }

    public class DocumentLayoutVersionDto
    {
        public string Version { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class DocumentExportResultDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "text/html; charset=utf-8";
        public string Content { get; set; } = string.Empty;
    }
}
