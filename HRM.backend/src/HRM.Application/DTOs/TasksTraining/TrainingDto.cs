namespace HRM.backend.src.HRM.Application.DTOs.TasksTraining
{
    public class TrainingSummaryDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public string? CourseName { get; set; }
        public string? TrainingType { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal? FinalScore { get; set; }
        public string? ManagerEvaluation { get; set; }
        public bool IsPassed { get; set; }
        public DateTime? EvaluationDeadline { get; set; }
        public List<TaskResponseDto> Tasks { get; set; } = new();
    }

    public class EvaluateTrainingDto
    {
        public int TrainingId { get; set; }
        public bool IsApproved { get; set; }
        public decimal? FinalScore { get; set; }
        public string? ManagerEvaluation { get; set; }
        public bool CreatePromotionRequest { get; set; } = true;
    }
}
