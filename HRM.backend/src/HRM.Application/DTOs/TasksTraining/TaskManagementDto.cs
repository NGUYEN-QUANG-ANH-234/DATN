using Microsoft.AspNetCore.Http;

namespace HRM.backend.src.HRM.Application.DTOs.TasksTraining
{
    public class TaskProgressUpdateDto
    {
        public int ProgressPercent { get; set; }
        public string? Note { get; set; }
        public IFormFile? EvidenceFile { get; set; }
    }

    public class TaskFeedbackDto
    {
        public string? Content { get; set; }
    }

    public class TaskResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string TaskType { get; set; } = string.Empty;
        public int? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? DepartmentName { get; set; }
        public int ProgressPercent { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? EvidencePath { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime? ReviewDeadline { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public List<TaskProgressResponseDto> Progresses { get; set; } = new();
        public List<TaskFeedbackResponseDto> Feedbacks { get; set; } = new();
    }

    public class TaskProgressResponseDto
    {
        public int Id { get; set; }
        public int ProgressPercent { get; set; }
        public string? Note { get; set; }
        public string? EvidencePath { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class TaskFeedbackResponseDto
    {
        public int Id { get; set; }
        public string FeedbackType { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string? ReviewerName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
