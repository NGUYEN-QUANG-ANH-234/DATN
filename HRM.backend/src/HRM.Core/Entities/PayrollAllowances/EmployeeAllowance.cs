using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
 using HRM.backend.src.HRM.Core.Entities.EmployeeProfile; // Mở comment nếu Employee ở module khác

namespace HRM.backend.src.HRM.Core.Entities.PayrollAllowances
{
    [Table("employee_allowances")]
    public class EmployeeAllowance
    {
        [Key] public int Id { get; set; }

        public int? EmployeeId { get; set; }
         [ForeignKey("EmployeeId")] public virtual Employee? Employee { get; set; }

        public int? AllowanceTypeId { get; set; }
        [ForeignKey("AllowanceTypeId")] public virtual AllowanceType? AllowanceType { get; set; }

        [Column(TypeName = "decimal(15,2)")]
        public decimal? Amount { get; set; }
    }
}