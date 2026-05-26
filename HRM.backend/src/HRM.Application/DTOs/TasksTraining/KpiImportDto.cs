using Microsoft.AspNetCore.Http;

namespace HRM.backend.src.HRM.Application.DTOs.TasksTraining
{
    public class KpiImportRequestDto
    {
        public IFormFile File { get; set; } = null!;
        public string? Period { get; set; }
        public int? DeptId { get; set; }
    }

    public class KpiImportRowDto
    {
        public int RowNumber { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string? KpiCode { get; set; }
        public string KpiName { get; set; } = string.Empty;
        public int WeightPercent { get; set; }
        public string? Description { get; set; }
        public decimal? TargetValue { get; set; }
        public string? Unit { get; set; }
    }

    public class KpiImportErrorDto
    {
        public int RowNumber { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class KpiImportResultDto
    {
        public int ImportBatchId { get; set; }
        public string Period { get; set; } = string.Empty;
        public int DeptId { get; set; }
        public int TotalRows { get; set; }
        public int SuccessRows { get; set; }
        public int ErrorRows { get; set; }
        public int CreatedOrUpdatedReviews { get; set; }
        public int CreatedDetails { get; set; }
        public int TotalAssignedWeight { get; set; }
        public List<KpiImportErrorDto> Errors { get; set; } = new();
    }
}
