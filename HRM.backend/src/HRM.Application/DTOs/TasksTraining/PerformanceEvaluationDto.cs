namespace HRM.backend.src.HRM.Application.DTOs.TasksTraining
{
    public class PerformanceEvaluationDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public string Period { get; set; } = string.Empty;
        public int TotalWeight { get; set; }
        public decimal SystemPenaltyPoint { get; set; }
        public decimal TotalScore { get; set; }
        public string? ScoringVersion { get; set; }
        public string? FinalRating { get; set; }
        public string? FinalComment { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<PerformanceDetailDto> Details { get; set; } = new();
    }

    public class PerformanceDetailDto
    {
        public int Id { get; set; }
        public string KpiCode { get; set; } = string.Empty;
        public string KpiName { get; set; } = string.Empty;
        public int WeightPercent { get; set; }
        public decimal? TargetValue { get; set; }
        public decimal? ActualValue { get; set; }
        public string? Unit { get; set; }
        public decimal EmployeeSelfPercent { get; set; }
        public decimal AchievedPercent { get; set; }
        public decimal ManagerScore { get; set; }
        public decimal SystemPenaltyPoint { get; set; }
        public string? SystemPenaltyReason { get; set; }
        public decimal ManualPenaltyPoint { get; set; }
        public string? ManualPenaltyReason { get; set; }
        public decimal PenaltyPoint { get; set; }
        public string? PenaltyReason { get; set; }
        public decimal FinalPoint { get; set; }
        public string? EmployeeComment { get; set; }
        public string? ManagerComment { get; set; }
        public string? EvidencePath { get; set; }
    }

    public class FinalizePerformanceDto
    {
        public bool IsApproved { get; set; }
        public string? FinalRating { get; set; }
        public string? FinalComment { get; set; }
        public List<FinalizePerformanceDetailDto> Details { get; set; } = new();
    }

    public class FinalizePerformanceDetailDto
    {
        public int DetailId { get; set; }
        public decimal ManagerScore { get; set; }
        public decimal ManualPenaltyPoint { get; set; }
        public string? ManualPenaltyReason { get; set; }
        public string? ManagerComment { get; set; }
    }

    public class PerformanceProgressUpdateDto
    {
        public List<PerformanceProgressDetailDto> Details { get; set; } = new();
    }

    public class PerformanceProgressDetailDto
    {
        public int DetailId { get; set; }
        public decimal EmployeeSelfPercent { get; set; }
        public decimal? ActualValue { get; set; }
        public string? EmployeeComment { get; set; }
    }
}
