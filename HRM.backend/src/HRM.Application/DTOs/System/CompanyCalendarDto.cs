using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.DTOs.System
{
    public class CompanyCalendarDto
    {
        public int Id { get; set; }
        public short Year { get; set; }
        public string VersionCode { get; set; } = string.Empty;
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public PolicyVersionStatus Status { get; set; }
        public string? SourceRef { get; set; }
        public bool LockedAfterUsed { get; set; }
        public string? Note { get; set; }
        public List<CompanyCalendarDayDto> Days { get; set; } = new();
    }

    public class CompanyCalendarDayDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public CompanyCalendarDayType DayType { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsPaid { get; set; } = true;
        public bool IsOvertimeHoliday { get; set; }
        public bool IsWorkingDayOverride { get; set; }
        public string? Description { get; set; }
    }

    public class SaveCompanyCalendarDto
    {
        public int? Id { get; set; }
        public string? VersionCode { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public PolicyVersionStatus Status { get; set; } = PolicyVersionStatus.Active;
        public string? SourceRef { get; set; }
        public string? Note { get; set; }
        public List<SaveCompanyCalendarDayDto> Days { get; set; } = new();
    }

    public class SaveCompanyCalendarDayDto
    {
        public DateTime Date { get; set; }
        public CompanyCalendarDayType DayType { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsPaid { get; set; } = true;
        public bool IsOvertimeHoliday { get; set; }
        public bool IsWorkingDayOverride { get; set; }
        public string? Description { get; set; }
    }
}
