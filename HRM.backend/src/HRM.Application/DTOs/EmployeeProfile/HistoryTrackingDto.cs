namespace HRM.backend.src.HRM.Application.DTOs.EmployeeProfile
{
    public class HistoryFilterDto
    {
        public int? Year { get; set; }
        public string Type { get; set; } = "ALL";
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;
    }

    public class ConsolidatedHistoryResponse
    {
        public DateTime Date { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? RefId { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }

    public class PaginatedHistoryResponse
    {
        public List<ConsolidatedHistoryResponse> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int Size { get; set; }
        public int TotalPages => Size <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)Size);
    }
}
