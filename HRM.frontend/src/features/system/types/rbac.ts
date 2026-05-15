export interface RoleWithPermissions {
  roleId: number;
  roleName: string;
  permissions: string[];
}

export interface UpdateRolePermissions {
  roleId: number;
  permissionCodes: string[];
}
