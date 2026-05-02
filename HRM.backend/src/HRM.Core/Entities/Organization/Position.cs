using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
 using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;

namespace HRM.backend.src.HRM.Core.Entities.Organization
{
    [Table("positions")]
    public class Position
    {
        [Key] public int Id { get; set; }

        [StringLength(100)] public required string Title { get; set; }

        public int JobLevel { get; set; } = 1;

        // --- Navigation Properties (Quan hệ 1-N) ---
        // Một vị trí/chức vụ có thể được nắm giữ bởi nhiều nhân viên
         public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}