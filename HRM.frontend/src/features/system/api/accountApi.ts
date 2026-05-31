import axiosClient from "../../../core/api/axiosClient";
import type {
  CreateAccountDto,
  AccountStatus,
  CreateAccountResultDto,
  ResetPasswordResultDto,
} from "../types/account";

const ENDPOINT = "/accounts";

const accountStatusValue: Record<AccountStatus, number> = {
  Active: 0,
  Inactive: 1,
  Locked: 2,
  Suspended: 3,
};

export const accountApi = {
  // Giả định bạn đã (hoặc sẽ) có 1 hàm GET danh sách User ở Backend
  getAccounts: async () => await axiosClient.get(ENDPOINT),

  // YÊU CẦU 3: Lấy danh sách Role từ hệ thống
  getSystemRoles: async () => await axiosClient.get("/system/roles"),

  updateRole: async (id: number, roleId: number) => {
    return await axiosClient.patch(`${ENDPOINT}/${id}/role`, roleId);
  },

  createAccount: async (data: CreateAccountDto) => {
    return await axiosClient.post<unknown, { data?: CreateAccountResultDto }>(
      ENDPOINT,
      data,
    );
  },

  toggleStatus: async (id: number, status: AccountStatus) => {
    return await axiosClient.patch(`${ENDPOINT}/${id}/status`, {
      status: accountStatusValue[status],
    });
  },

  resetPassword: async (id: number) => {
    return await axiosClient.post<unknown, { data?: ResetPasswordResultDto }>(
      `${ENDPOINT}/${id}/reset-password`,
    );
  },
};
