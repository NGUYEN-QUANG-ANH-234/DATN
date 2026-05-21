import axiosClient from "../../../core/api/axiosClient";
import type {
  HistoryEventType,
  MyProfileDto,
  MyContractDto,
  PaginatedHistoryResponse,
} from "../types/myProfile";

export const myProfileApi = {
  getProfile: async (): Promise<{ success: boolean; data: MyProfileDto }> => {
    return await axiosClient.get("/employees/me/profile");
  },

  getContracts: async (): Promise<{
    success: boolean;
    data: MyContractDto[];
  }> => {
    return await axiosClient.get("/employees/me/contracts");
  },

  getHistory: async (params: {
    year?: number | "";
    type?: HistoryEventType;
    page?: number;
    size?: number;
  }): Promise<{ success: boolean; data: PaginatedHistoryResponse }> => {
    return await axiosClient.get("/employees/me/history", { params });
  },
};
