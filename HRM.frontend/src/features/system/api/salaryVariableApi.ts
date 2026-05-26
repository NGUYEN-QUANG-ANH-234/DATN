import axiosClient from "../../../core/api/axiosClient"; // Điều chỉnh theo cấu hình thực tế
import type {
  BaseResponse,
  CreateSourceCatalogPayload,
  SalaryVariable,
  SourceCatalogItem,
} from "../types/salaryVariable";

const ENDPOINT = "/system/salary-variables";
const CATALOG_ENDPOINT = "/system/source-catalogs";

export const salaryVariableApi = {
  getAll: async (): Promise<BaseResponse<SalaryVariable[]>> => {
    return await axiosClient.get(ENDPOINT);
  },

  getCatalogs: async (): Promise<BaseResponse<SourceCatalogItem[]>> => {
    return await axiosClient.get(CATALOG_ENDPOINT);
  },

  createCatalog: async (
    payload: CreateSourceCatalogPayload,
  ): Promise<BaseResponse<SourceCatalogItem>> => {
    return await axiosClient.post(CATALOG_ENDPOINT, payload);
  },

  define: async (payload: SalaryVariable): Promise<BaseResponse<null>> => {
    return await axiosClient.post(ENDPOINT, payload);
  },
};
