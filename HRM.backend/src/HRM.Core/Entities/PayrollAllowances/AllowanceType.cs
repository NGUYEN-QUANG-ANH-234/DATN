using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.PayrollAllowances
{
    [Table("allowance_types")]
    public class AllowanceType
    {
        [Key] public int Id { get; set; }

        [StringLength(100)] public string? TypeName { get; set; }

        public bool IsTaxable { get; set; } = true;
        public bool IsInsuranceBase { get; set; } = false;

        // Navigation Property (1 Loại phụ cấp có thể được cấp cho nhiều nhân viên)
        public virtual ICollection<EmployeeAllowance> EmployeeAllowances { get; set; } = new List<EmployeeAllowance>();
    }
}