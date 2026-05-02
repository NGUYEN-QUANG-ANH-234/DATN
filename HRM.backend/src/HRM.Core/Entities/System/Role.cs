using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.System
{
    [Table("roles")]
    public class Role
    {
        [Key] public int Id { get; set; }

        [StringLength(50)]
        public required string RoleName { get; set; }

        public string? Description { get; set; }

        // Navigation properties cho quan hệ 1-N (1 Role có nhiều Account và Permission)
        public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}