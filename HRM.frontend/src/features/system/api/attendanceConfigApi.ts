import axiosClient from "../../../core/api/axiosClient";
import type { BaseResponse } from "../types/salaryVariable";
import type { AttendanceConfig } from "../types/attendanceConfig";

const ENDPOINT = "/system/attendance-config";

export const attendanceConfigApi = {
  get: async (): Promise<BaseResponse<AttendanceConfig>> => {
    const response = await axiosClient.get(ENDPOINT);
    return response.data;
  },

  update: async (payload: AttendanceConfig): Promise<BaseResponse<null>> => {
    const response = await axiosClient.put(ENDPOINT, payload);
    return response.data || response;
  },
};
