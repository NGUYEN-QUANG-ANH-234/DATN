import { useState, useEffect, useCallback } from "react";
import { rbacApi } from "../api/rbacApi";
import type { RoleWithPermissions, UpdateRolePermissions } from "../types/rbac";

export const useRbac = () => {
  const [roles, setRoles] = useState<RoleWithPermissions[]>([]);
  const [availableModules, setAvailableModules] = useState<unknown[]>([]);
  const [loading, setLoading] = useState<boolean>(false);

  const fetchAllData = useCallback(async () => {
    setLoading(true);
    try {
      const [rolesRes, permsRes] = await Promise.all([
        rbacApi.getRoles(),
        rbacApi.getAllPermissions(),
      ]);

      const parsedRoles = Array.isArray(rolesRes)
        ? rolesRes
        : ((rolesRes as { data?: RoleWithPermissions[] }).data ?? []);
      const parsedModules = Array.isArray(permsRes)
        ? permsRes
        : ((permsRes as { data?: unknown[] }).data ?? []);

      setRoles(parsedRoles);
      setAvailableModules(parsedModules);
    } catch (error) {
      console.error("Lỗi tải dữ liệu RBAC:", error);
    } finally {
      setLoading(false);
    }
  }, []);

  const updatePermissions = async (payload: UpdateRolePermissions) => {
    try {
      const res = (await rbacApi.updatePermissions(payload)) as unknown;
      await fetchAllData();
      return res;
    } catch (error: unknown) {
      throw (
        (error as { response?: { data?: { message?: string } } }).response?.data
          ?.message || "Lỗi hệ thống khi cập nhật quyền"
      );
    }
  };

  useEffect(() => {
    fetchAllData();
  }, [fetchAllData]);

  return { roles, availableModules, loading, updatePermissions };
};
