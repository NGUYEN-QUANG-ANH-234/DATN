using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;

namespace HRM.backend.src.HRM.Core.Entities.PayrollAllowances
{
    [Table("employee_salary_components")]
    public class EmployeeSalaryComponent
    {
        [Key] public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey(nameof(EmployeeId))] public virtual Employee Employee { get; set; } = null!;

        public int SalaryComponentTypeId { get; set; }
        [ForeignKey(nameof(SalaryComponentTypeId))] public virtual SalaryComponentType SalaryComponentType { get; set; } = null!;

        [Column(TypeName = "decimal(15,2)")] public decimal Amount { get; set; }

        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; } = true;
        [StringLength(500)] public string? SourceReference { get; set; }
        [StringLength(1000)] public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
