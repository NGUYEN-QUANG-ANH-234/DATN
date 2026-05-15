using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.EmployeeProfile
{
    [Table("contracts")]
    public class Contract
    {
        [Key] public int Id { get; set; }

        public int? EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        [StringLength(50)] public required string ContractNumber { get; set; }

        public ContractType ContractType { get; set; } = ContractType.Probation;

        [Column(TypeName = "decimal(15,2)")] public decimal BasicSalary { get; set; }

        [Column(TypeName = "decimal(5,2)")] public decimal SalaryPercentage { get; set; } = 100.0m;
        public int? PayrollFormulaId { get; set; }

        [ForeignKey("PayrollFormulaId")] public virtual PayrollFormula? PayrollFormula { get; set; }

        [Column(TypeName = "decimal(15,2)")] 
        public decimal InsuranceSalary { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }


        public ContractStatus Status { get; set; } = ContractStatus.Draft;
    }
}