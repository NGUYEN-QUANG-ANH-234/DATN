using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.backend.src.HRM.Core.Entities.System
{
    [Table("permissions")]
    public class Permission
    {
        [Key] public int Id { get; set; }

        [StringLength(50)]
        public required string PermissionCode { get; set; }

        public string? Description { get; set; }

        // Navigation property
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}