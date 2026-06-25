using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.TimeAttendance
{
    [Table("work_calendar_configs")]
    public class WorkCalendarConfig
    {
        [Key] public int Id { get; set; }

        public int DeptId { get; set; }
        [ForeignKey("DeptId")] public virtual Department? Department { get; set; }

        public byte Month { get; set; }
        public short Year { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal StandardWorkDays { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal StandardHoursPerDay { get; set; } = 8m;

        public bool IncludePaidLeaveInWorkDays { get; set; } = true;

        [StringLength(50)]
        public string? WorkingDaysOfWeek { get; set; }

        public int? CompanyCalendarId { get; set; }
        [ForeignKey("CompanyCalendarId")] public virtual CompanyCalendar? CompanyCalendar { get; set; }

        public string? HolidayDatesJson { get; set; }
        public TimeSpan? HolidayWorkingStartTime { get; set; }
        public TimeSpan? HolidayWorkingEndTime { get; set; }
        public bool IsLocked { get; set; }

        [StringLength(80)]
        public string? VersionCode { get; set; }

        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public PolicyVersionStatus Status { get; set; } = PolicyVersionStatus.Active;

        [StringLength(200)]
        public string? SourceRef { get; set; }

        public int? SupersedesVersionId { get; set; }
        public int? CreatedByAccountId { get; set; }
        [ForeignKey(nameof(CreatedByAccountId))] public virtual Account? CreatedByAccount { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public bool LockedAfterUsed { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedByAccountId { get; set; }
        [ForeignKey(nameof(UpdatedByAccountId))] public virtual Account? UpdatedByAccount { get; set; }
    }
}
