namespace HRM.backend.src.HRM.Core.Models.History
{
    public class HistoryFilterCriteria
    {
        public int? Year { get; set; }
        public string Type { get; set; } = "ALL";
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;
    }

    public class ConsolidatedHistoryRecord
    {
        public DateTime Date { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? RefId { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int Size { get; set; }
    }
}
