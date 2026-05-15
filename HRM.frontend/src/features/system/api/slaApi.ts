import axiosClient from "../../../core/api/axiosClient";
import type { SlaConfig, SlaUpdateRequest } from "../types/sla.ts";

type BaseResponse<T> = {
  data: T;
  success?: boolean;
  message?: string;
};

const ENDPOINT = "/system/sla";

export const slaApi = {
  getAll: async (): Promise<BaseResponse<SlaConfig[]>> => {
    const response = await axiosClient.get(ENDPOINT);
    return response.data;
  },

  update: async (payload: SlaUpdateRequest): Promise<BaseResponse<null>> => {
    const response = await axiosClient.put(ENDPOINT, payload);
    return response.data || response;
  },
};
