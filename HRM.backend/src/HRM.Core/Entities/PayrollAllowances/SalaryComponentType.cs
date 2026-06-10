using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Entities.PayrollAllowances
{
    [Table("salary_component_types")]
    public class SalaryComponentType
    {
        [Key] public int Id { get; set; }

        [StringLength(80)] public required string Code { get; set; }
        [StringLength(200)] public required string Name { get; set; }

        public SalaryComponentGroup ComponentGroup { get; set; } = SalaryComponentGroup.Allowance;

        public bool IsIncome { get; set; } = true;
        public bool IsDeduction { get; set; }
        public bool IsTaxable { get; set; } = true;
        public bool IsInsuranceBased { get; set; }
        public bool IsFixed { get; set; }
        public bool IsAllowance { get; set; }
        public bool IsBonus { get; set; }
        public bool IsOvertime { get; set; }

        public ProrationType ProrationType { get; set; } = ProrationType.None;
        public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.FixedAmount;

        [Column(TypeName = "decimal(15,2)")] public decimal? TaxExemptCap { get; set; }

        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public int Version { get; set; } = 1;
        [StringLength(80)] public string? VersionCode { get; set; }
        public PolicyVersionStatus Status { get; set; } = PolicyVersionStatus.Active;
        public bool IsActive { get; set; } = true;
        [StringLength(1000)] public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<EmployeeSalaryComponent> EmployeeSalaryComponents { get; set; } = new List<EmployeeSalaryComponent>();
    }
}
