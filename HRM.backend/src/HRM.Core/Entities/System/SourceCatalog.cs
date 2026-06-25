using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.System
{
    [Table("source_catalogs")]
    public class SourceCatalog
    {
        [Key] public int Id { get; set; }

        [StringLength(100)]
        public required string DisplayName { get; set; }

        [StringLength(100)]
        public required string SourcePath { get; set; }

        [StringLength(50)]
        public required string Module { get; set; }

        public SalaryVariableDataType DataType { get; set; } = SalaryVariableDataType.Number;
        public SalaryAggregationType AggregationType { get; set; } = SalaryAggregationType.Latest;
        public bool IsPeriodBased { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
    }
}
