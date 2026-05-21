import axiosClient from "../../../core/api/axiosClient";
import type { CreateAccountDto, AccountStatus } from "../types/account";

const ENDPOINT = "/accounts";

export const accountApi = {
  // Giả định bạn đã (hoặc sẽ) có 1 hàm GET danh sách User ở Backend
  getAccounts: async () => await axiosClient.get(ENDPOINT),

  // YÊU CẦU 3: Lấy danh sách Role từ hệ thống
  getSystemRoles: async () => await axiosClient.get("/system/roles"),

  updateRole: async (id: number, roleId: number) => {
    return await axiosClient.patch(`${ENDPOINT}/${id}/role`, roleId);
  },

  createAccount: async (data: CreateAccountDto) => {
    return await axiosClient.post(ENDPOINT, data);
  },

  toggleStatus: async (id: number, status: AccountStatus) => {
    return await axiosClient.patch(`${ENDPOINT}/${id}/status`, {
      status,
    });
  },

  resetPassword: async (id: number) => {
    return await axiosClient.post(`${ENDPOINT}/${id}/reset-password`);
  },
};
