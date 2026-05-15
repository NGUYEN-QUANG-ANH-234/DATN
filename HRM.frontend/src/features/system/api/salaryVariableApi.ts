import axiosClient from "../../../core/api/axiosClient"; // Điều chỉnh theo cấu hình thực tế
import type {
  BaseResponse,
  SalaryVariable,
  SourceCatalogItem,
} from "../types/salaryVariable";

const ENDPOINT = "/system/salary-variables";
const CATALOG_ENDPOINT = "/system/source-catalogs";

export const salaryVariableApi = {
  getAll: async (): Promise<BaseResponse<SalaryVariable[]>> => {
    const response = await axiosClient.get(ENDPOINT);
    return response.data;
  },

  getCatalogs: async (): Promise<BaseResponse<SourceCatalogItem[]>> => {
    const response = await axiosClient.get(CATALOG_ENDPOINT);
    return response.data;
  },

  define: async (payload: SalaryVariable): Promise<BaseResponse<null>> => {
    const response = await axiosClient.post(ENDPOINT, payload);
    return response.data || response;
  },
};
