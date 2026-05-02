using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.System
{
    [Table("configurations")]
    public class Configuration
    {
        [Key] public int Id { get; set; }

        [StringLength(50)]
        public required string ParamKey { get; set; }

        public required string ParamValue { get; set; }
        public string? Description { get; set; }
    }
}