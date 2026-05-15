namespace HRM.backend.src.HRM.Application.DTOs.System
{
    public class RoleWithPermissionsDto
    {
        public int RoleId { get; set; }
        public required string RoleName { get; set; } // Map thẳng vào RoleName của Entity
        public List<string> Permissions { get; set; } = new();
    }

    public class UpdateRolePermissionsDto
    {
        public int RoleId { get; set; }
        public required List<string> PermissionCodes { get; set; }
    }

    public class PermissionGroupDto
    {
        public required string Group { get; set; }
        public List<PermissionItemDto> Codes { get; set; } = new();
    }

    public class PermissionItemDto
    {
        public required string Code { get; set; }
        public string? Desc { get; set; }
    }
}
