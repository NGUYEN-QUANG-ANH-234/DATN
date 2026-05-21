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
    }
}
