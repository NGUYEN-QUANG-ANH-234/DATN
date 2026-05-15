namespace HRM.backend.src.HRM.Application.DTOs.System
{
    public class AuditLogFilterDto
    {
        public int? AccountId { get; set; }
        public string? Module { get; set; } // Sẽ được map với Whitelist ở Backend
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class AuditLogResponseDto
    {
        public int Id { get; set; }
        public int? AccountId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // THÊM MỚI: DTO cấu trúc dữ liệu trả về cho Dashboard
    public class AuditStatisticsResponseDto
    {
        public IEnumerable<ModuleStatDto> ModuleStats { get; set; } = new List<ModuleStatDto>();
        public IEnumerable<ActionStatDto> ActionStats { get; set; } = new List<ActionStatDto>();
    }

    public class ModuleStatDto
    {
        public string? Module { get; set; }
        public int Count { get; set; }
    }

    public class ActionStatDto
    {
        public string? Action { get; set; }
        public int Count { get; set; }
    }
}