import axiosClient from "../../../core/api/axiosClient";
import type { DepartmentTree } from "../types/department";

const ENDPOINT = "/departments";

export const departmentApi = {
  getTree: async (): Promise<{ success: boolean; data: DepartmentTree[] }> => {
    return await axiosClient.get(`${ENDPOINT}/tree`);
  },

  updateStructure: async (id: number, newParentId: number | null) => {
    return await axiosClient.put(`${ENDPOINT}/${id}/structure`, {
      newParentId,
    });
  },

  deactivate: async (id: number) => {
    return await axiosClient.patch(`${ENDPOINT}/${id}/deactivate`);
  },

  create: async (data: {
    deptCode: string;
    deptName: string;
    parentDeptId: number | null;
  }) => {
    return await axiosClient.post(ENDPOINT, data);
  },
};
