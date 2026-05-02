using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Core.Entities.System
{
    [Table("role_permissions")]
    [PrimaryKey(nameof(RoleId), nameof(PermissionId))] // EF Core 7+ hỗ trợ Composite Key bằng Attribute
    public class RolePermission
    {
        public int RoleId { get; set; }
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!;

        public int PermissionId { get; set; }
        [ForeignKey("PermissionId")]
        public virtual Permission Permission { get; set; } = null!;
    }
}