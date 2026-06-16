using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.PayrollAllowances
{
    [Table("external_timesheet_imports")]
    public class ExternalTimesheetImport
    {
        [Key] public int Id { get; set; }

        [StringLength(100)] public required string SourceSystem { get; set; }
        public byte ImportMonth { get; set; }
        public short ImportYear { get; set; }
        [StringLength(7)] public required string PayrollPeriod { get; set; }
        [StringLength(255)] public string? FileName { get; set; }

        public int ImportedByAccountId { get; set; }
        [ForeignKey("ImportedByAccountId")] public virtual Account ImportedByAccount { get; set; } = null!;

        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
        public ExternalTimesheetImportStatus Status { get; set; } = ExternalTimesheetImportStatus.Draft;

        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int ErrorRows { get; set; }
        [Column(TypeName = "decimal(9,2)")] public decimal TotalHours { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal TotalAmount { get; set; }

        public int? ApprovedByAccountId { get; set; }
        [ForeignKey("ApprovedByAccountId")] public virtual Account? ApprovedByAccount { get; set; }
        public DateTime? ApprovedAt { get; set; }

        [StringLength(500)] public string? FileUrl { get; set; }
        [StringLength(1000)] public string? Note { get; set; }

        public virtual ICollection<ExternalTimesheetLine> Lines { get; set; } = new List<ExternalTimesheetLine>();
    }
}
