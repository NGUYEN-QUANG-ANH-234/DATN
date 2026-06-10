namespace HRM.backend.src.HRM.Application.DTOs.Dashboard
{
    public class DashboardResponseDto
    {
        public string Role { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public List<DashboardWidgetDto> Widgets { get; set; } = new();
        public List<DashboardSectionDto> Sections { get; set; } = new();
        public List<DashboardActionDto> QuickActions { get; set; } = new();
        public List<DashboardRiskItemDto> Risks { get; set; } = new();
    }

    public class DashboardWidgetDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string Severity { get; set; } = "neutral";
        public string Scope { get; set; } = string.Empty;
        public int Order { get; set; }
        public DashboardDrilldownRefDto? Drilldown { get; set; }
        public List<DashboardMetricDto> Metrics { get; set; } = new();
        public List<DashboardActionDto> Actions { get; set; } = new();
    }

    public class DashboardMetricDto
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public decimal? NumericValue { get; set; }
        public string Severity { get; set; } = "neutral";
    }

    public class DashboardActionDto
    {
        public string Label { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string ActionType { get; set; } = "open";
        public string? Icon { get; set; }
    }

    public class DashboardDrilldownRefDto
    {
        public string Type { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public Dictionary<string, string?> Filters { get; set; } = new();
    }

    public class DashboardSectionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string Type { get; set; } = "table";
        public int Order { get; set; }
        public DashboardTableDto? Table { get; set; }
        public List<DashboardWidgetDto> Widgets { get; set; } = new();
    }

    public class DashboardTableDto
    {
        public List<string> Columns { get; set; } = new();
        public List<Dictionary<string, string?>> Rows { get; set; } = new();
    }

    public class DashboardRiskItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Severity { get; set; } = "neutral";
        public string? Route { get; set; }
    }

    public class DashboardDrilldownDto
    {
        public string Type { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<DashboardMetricDto> Metrics { get; set; } = new();
        public DashboardTableDto Table { get; set; } = new();
    }
}
