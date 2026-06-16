namespace HRM.backend.src.HRM.Application.DTOs.System
{
    public class PendingApprovalDto
    {
        public int ApprovalRequestId { get; set; }
        public string ModuleCode { get; set; } = string.Empty;
        public int ReferenceId { get; set; }
        public int Level { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Polymorphic Data
        public string Title { get; set; } = string.Empty; // Job Title OR Candidate Name
        public string? Description { get; set; } 
        public string? DepartmentName { get; set; }
        public string? PositionName { get; set; }
        public int? Quantity { get; set; }
        public DateTime? Deadline { get; set; }
        public string? CvFilePath { get; set; } // Only for Candidates
        public string? Status { get; set; }
        public string? StatusLabel { get; set; }
        public string? DetailRoute { get; set; }
        public string? DetailTitle { get; set; }
        public List<PendingApprovalActionDto> Actions { get; set; } = new();
        public List<PendingApprovalDetailFieldDto> DetailFields { get; set; } = new();
    }

    public class PendingApprovalActionDto
    {
        public string Kind { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Tone { get; set; } = "secondary";
        public bool RequiresNote { get; set; }
        public string? Endpoint { get; set; }
        public string Method { get; set; } = "POST";
    }

    public class PendingApprovalDetailFieldDto
    {
        public string Label { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
}
