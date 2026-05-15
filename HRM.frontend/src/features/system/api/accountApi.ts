import axiosClient from "../../../core/api/axiosClient";
import type { CreateAccountDto, AccountStatus } from "../types/account";

const ENDPOINT = "/accounts";

export const accountApi = {
  // Giả định bạn đã (hoặc sẽ) có 1 hàm GET danh sách User ở Backend
  getAccounts: async () => (await axiosClient.get(ENDPOINT)).data,

  // YÊU CẦU 3: Lấy danh sách Role từ hệ thống
  getSystemRoles: async () => (await axiosClient.get("/system/roles")).data,

  updateRole: async (id: number, roleId: number) => {
    return (await axiosClient.patch(`${ENDPOINT}/${id}/role`, roleId)).data;
  },

  createAccount: async (data: CreateAccountDto) => {
    const response = await axiosClient.post(ENDPOINT, data);
    return response.data;
  },

  toggleStatus: async (id: number, status: AccountStatus) => {
    const response = await axiosClient.patch(`${ENDPOINT}/${id}/status`, {
      status,
    });
    return response.data;
  },

  resetPassword: async (id: number) => {
    const response = await axiosClient.post(`${ENDPOINT}/${id}/reset-password`);
    return response.data;
  },
};
