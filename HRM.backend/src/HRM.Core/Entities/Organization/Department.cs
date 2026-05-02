using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRM.backend.src.HRM.Core.Enums;
 using HRM.backend.src.HRM.Core.Entities.EmployeeProfile; // Bỏ comment khi bạn đã tạo file Employee.cs

namespace HRM.backend.src.HRM.Core.Entities.Organization
{
    [Table("departments")]
    public class Department
    {
        [Key] public int Id { get; set; }

        [StringLength(20)] public required string DeptCode { get; set; }

        [StringLength(100)] public required string DeptName { get; set; }

        public int? ParentDeptId { get; set; }
        [ForeignKey("ParentDeptId")]
        public virtual Department? ParentDepartment { get; set; }

        public int? ManagerId { get; set; }
        // [ForeignKey("ManagerId")] 
         public virtual Employee? Manager { get; set; }

        public DeptStatus Status { get; set; } = DeptStatus.Active;

        // --- Navigation Properties (Quan hệ 1-N) ---
        // 1. Một phòng ban có thể có nhiều phòng ban con (Self-referencing)
        public virtual ICollection<Department> SubDepartments { get; set; } = new List<Department>();

        // 2. Một phòng ban có nhiều nhân viên
         public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}