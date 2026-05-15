using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.System
{
    [Table("source_catalogs")]
    public class SourceCatalog
    {
        [Key] public int Id { get; set; }

        [StringLength(100)]
        public required string DisplayName { get; set; } // Ví dụ: "Lương cơ bản (Hợp đồng)"

        [StringLength(100)]
        public required string SourcePath { get; set; }  // Ví dụ: "Contract.BasicSalary"

        [StringLength(50)]
        public required string Module { get; set; }      // Ví dụ: "Hồ sơ", "Chấm công", "Công việc"
    }
}
