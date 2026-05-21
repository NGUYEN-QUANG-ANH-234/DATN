import axiosClient from "../../../core/api/axiosClient";
import type { BaseResponse } from "../types/salaryVariable";
import type { AttendanceConfig } from "../types/attendanceConfig";

const ENDPOINT = "/system/attendance-config";

export const attendanceConfigApi = {
  get: async (): Promise<BaseResponse<AttendanceConfig>> => {
    return await axiosClient.get(ENDPOINT);
  },

  update: async (payload: AttendanceConfig): Promise<BaseResponse<null>> => {
    return await axiosClient.put(ENDPOINT, payload);
  },
};
