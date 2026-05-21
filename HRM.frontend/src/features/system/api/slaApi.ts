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
    return await axiosClient.get(ENDPOINT);
  },

  update: async (payload: SlaUpdateRequest): Promise<BaseResponse<null>> => {
    return await axiosClient.put(ENDPOINT, payload);
  },
};
