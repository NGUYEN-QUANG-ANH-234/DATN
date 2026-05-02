using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.EmployeeProfile
{
    [Table("dependents")]
    public class Dependent
    {
        [Key] public int Id { get; set; }

        public int? EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        [StringLength(100)] public string? FullName { get; set; }
        [StringLength(50)] public string? Relationship { get; set; }
        [StringLength(20)] public string? IdNumber { get; set; }

        public DateTime? BirthDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}