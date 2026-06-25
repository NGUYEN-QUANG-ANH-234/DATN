import axiosClient from "../../../core/api/axiosClient";
import type { DepartmentTree, UpdateDepartmentPayload } from "../types/department";

const ENDPOINT = "/departments";

export const departmentApi = {
  getTree: async (includeInactive = false): Promise<{ success: boolean; data: DepartmentTree[] }> => {
    return await axiosClient.get(`${ENDPOINT}/tree`, {
      params: { includeInactive },
    });
  },

  updateStructure: async (id: number, newParentId: number | null) => {
    return await axiosClient.put(`${ENDPOINT}/${id}/structure`, {
      newParentId,
    });
  },

  update: async (id: number, data: UpdateDepartmentPayload) => {
    return await axiosClient.put(`${ENDPOINT}/${id}`, data);
  },

  deactivate: async (id: number) => {
    return await axiosClient.patch(`${ENDPOINT}/${id}/deactivate`);
  },

  activate: async (id: number) => {
    return await axiosClient.patch(`${ENDPOINT}/${id}/activate`);
  },

  delete: async (id: number) => {
    return await axiosClient.delete(`${ENDPOINT}/${id}`);
  },

  create: async (data: {
    deptCode: string;
    deptName: string;
    parentDeptId: number | null;
  }) => {
    return await axiosClient.post(ENDPOINT, data);
  },
};
