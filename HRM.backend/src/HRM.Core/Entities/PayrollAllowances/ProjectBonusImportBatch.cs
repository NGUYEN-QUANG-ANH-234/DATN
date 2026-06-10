using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.PayrollAllowances
{
    [Table("project_bonus_import_batches")]
    public class ProjectBonusImportBatch
    {
        [Key] public int Id { get; set; }

        public byte PeriodMonth { get; set; }
        public short PeriodYear { get; set; }
        [StringLength(7)] public required string PayrollPeriod { get; set; }

        [StringLength(255)] public required string FileName { get; set; }

        public int UploadedByAccountId { get; set; }
        [ForeignKey(nameof(UploadedByAccountId))] public virtual Account UploadedByAccount { get; set; } = null!;

        public ProjectBonusImportStatus Status { get; set; } = ProjectBonusImportStatus.Draft;

        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int ErrorRows { get; set; }
        [Column(TypeName = "decimal(15,2)")] public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? ApprovedByAccountId { get; set; }
        [ForeignKey(nameof(ApprovedByAccountId))] public virtual Account? ApprovedByAccount { get; set; }
        public DateTime? ApprovedAt { get; set; }

        [StringLength(1000)] public string? Note { get; set; }

        public virtual ICollection<ProjectBonusImportLine> Lines { get; set; } = new List<ProjectBonusImportLine>();
    }
}
