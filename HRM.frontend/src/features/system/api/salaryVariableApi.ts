import axiosClient from "../../../core/api/axiosClient";
import type {
  BaseResponse,
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

  setCatalogActive: async (
    id: number,
    isActive: boolean,
  ): Promise<BaseResponse<SourceCatalogItem>> => {
    return await axiosClient.patch(`${CATALOG_ENDPOINT}/${id}/active`, {
      isActive,
    });
  },

  deleteCatalog: async (id: number): Promise<BaseResponse<null>> => {
    return await axiosClient.delete(`${CATALOG_ENDPOINT}/${id}`);
  },

  define: async (payload: SalaryVariable): Promise<BaseResponse<null>> => {
    return await axiosClient.post(ENDPOINT, payload);
  },

  setActive: async (
    code: string,
    isActive: boolean,
  ): Promise<BaseResponse<null>> => {
    return await axiosClient.patch(`${ENDPOINT}/${encodeURIComponent(code)}/active`, {
      isActive,
    });
  },

  delete: async (code: string): Promise<BaseResponse<null>> => {
    return await axiosClient.delete(`${ENDPOINT}/${encodeURIComponent(code)}`);
  },
};
