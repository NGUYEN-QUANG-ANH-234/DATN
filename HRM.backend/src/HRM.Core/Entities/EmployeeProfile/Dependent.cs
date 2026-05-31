using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.EmployeeProfile
{
    [Table("dependents")]
    public class Dependent
    {
        [Key] public int Id { get; set; }

        public int? EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        [StringLength(100)] public required string FullName { get; set; }
        public DependentRelation Relationship { get; set; }

        [StringLength(20)] public string? IdNumber { get; set; }
        [StringLength(20)] public string? TaxDependentCode { get; set; } // Mã số thuế phụ thuộc

        public DateTime? BirthDate { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public bool IsActive { get; set; } = true;
        public string? EvidenceUrl { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
