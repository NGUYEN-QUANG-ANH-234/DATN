using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.TasksTraining
{
    [Table("kpi_import_batches")]
    public class KpiImportBatch
    {
        [Key] public int Id { get; set; }

        [StringLength(10)] public required string Period { get; set; }

        public int DeptId { get; set; }
        [ForeignKey("DeptId")] public virtual Department? Department { get; set; }

        public int ImportedByAccountId { get; set; }
        [ForeignKey("ImportedByAccountId")] public virtual Account? ImportedByAccount { get; set; }

        [StringLength(255)] public string? FileName { get; set; }

        public int TotalRows { get; set; }
        public int SuccessRows { get; set; }
        public int ErrorRows { get; set; }

        public ImportBatchStatus Status { get; set; } = ImportBatchStatus.Processing;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<PerformanceReview> Reviews { get; set; } = new List<PerformanceReview>();
    }
}
