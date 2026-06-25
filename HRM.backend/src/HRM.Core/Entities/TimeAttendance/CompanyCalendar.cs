using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.TimeAttendance
{
    [Table("company_calendars")]
    public class CompanyCalendar
    {
        [Key] public int Id { get; set; }

        public short Year { get; set; }

        [StringLength(80)]
        public string VersionCode { get; set; } = string.Empty;

        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public PolicyVersionStatus Status { get; set; } = PolicyVersionStatus.Draft;

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

        public virtual ICollection<CompanyCalendarDay> Days { get; set; } = new List<CompanyCalendarDay>();
    }
}
