import axiosClient from "../../../core/api/axiosClient";
import type { BaseResponse } from "../types/salaryVariable";
import type { RoleWithPermissions, UpdateRolePermissions } from "../types/rbac";

const ENDPOINT = "/rbac";

export const rbacApi = {
  getRoles: async (): Promise<BaseResponse<RoleWithPermissions[]>> => {
    return await axiosClient.get(`${ENDPOINT}/roles`);
  },

  updatePermissions: async (
    payload: UpdateRolePermissions,
  ): Promise<BaseResponse<null>> => {
    return await axiosClient.put(`${ENDPOINT}/permissions`, payload);
  },

  getAllPermissions: async () => {
    return await axiosClient.get(`${ENDPOINT}/permissions/all`);
  },
};
