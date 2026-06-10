namespace HRM.backend.src.HRM.Application.DTOs.EmployeeProfile
{
    public class ContractDocumentPreviewDto
    {
        public int ReferenceId { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public string TemplateCode { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Html { get; set; } = string.Empty;
        public string? DocFilePath { get; set; }
        public string? PdfFilePath { get; set; }
        public bool CanDownloadPdf => !string.IsNullOrWhiteSpace(PdfFilePath);
    }

    public class IssueContractDocumentDto
    {
        public string? LegalDocumentNumber { get; set; }
        public string? DocumentTemplateCode { get; set; }
        public DateTime? IssuedAt { get; set; }
        public DateTime? EmployeeSignedAt { get; set; }
        public DateTime? EmployerSignedAt { get; set; }
    }

    public class ContractDocumentDownloadDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/msword; charset=utf-8";
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }
}
