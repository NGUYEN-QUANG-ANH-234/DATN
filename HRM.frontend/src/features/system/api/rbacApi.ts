import axiosClient from "../../../core/api/axiosClient";
import type { BaseResponse } from "../types/salaryVariable";
import type { RoleWithPermissions, UpdateRolePermissions } from "../types/rbac";

const ENDPOINT = "/rbac";

export const rbacApi = {
  getRoles: async (): Promise<BaseResponse<RoleWithPermissions[]>> => {
    const response = await axiosClient.get(`${ENDPOINT}/roles`);
    return response.data;
  },

  updatePermissions: async (
    payload: UpdateRolePermissions,
  ): Promise<BaseResponse<null>> => {
    const response = await axiosClient.put(`${ENDPOINT}/permissions`, payload);
    return response.data || response;
  },

  getAllPermissions: async () => {
    const response = await axiosClient.get(`${ENDPOINT}/permissions/all`);
    return response.data;
  },
};
