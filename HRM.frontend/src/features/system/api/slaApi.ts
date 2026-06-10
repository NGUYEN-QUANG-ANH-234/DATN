import axiosClient from "../../../core/api/axiosClient";
import type { SlaConfig, SlaStatusRequest, SlaUpdateRequest } from "../types/sla.ts";

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

  setActive: async (
    moduleCode: string,
    payload: SlaStatusRequest,
  ): Promise<BaseResponse<null>> => {
    return await axiosClient.patch(
      `${ENDPOINT}/${encodeURIComponent(moduleCode)}/active`,
      payload,
    );
  },
};
