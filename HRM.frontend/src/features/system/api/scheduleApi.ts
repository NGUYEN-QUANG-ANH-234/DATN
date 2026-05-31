import axiosClient from "../../../core/api/axiosClient";
import type {
  ConfiguredScheduleItem,
  ConfigureWorkScheduleDto,
  LeaveTypeSelect,
  ScheduleChangeHistoryItem,
} from "../types/scheduleConfig";

const WORK_SHIFT_ENDPOINT = "/system/work-shifts";
const LEAVE_TYPE_ENDPOINT = "/system/leave-types";

export const scheduleApi = {
  configureSchedule: async (data: ConfigureWorkScheduleDto) => {
    return await axiosClient.post(`${WORK_SHIFT_ENDPOINT}`, data);
  },

  getLeaveTypes: async (): Promise<{
    success: boolean;
    data: LeaveTypeSelect[];
  }> => {
    return await axiosClient.get(`${LEAVE_TYPE_ENDPOINT}`);
  },

  getDepartments: async () => {
    return await axiosClient.get("/departments/tree");
  },

  getConfiguredSchedules: async (): Promise<{
    success: boolean;
    data: ConfiguredScheduleItem[];
  }> => {
    return await axiosClient.get(`${WORK_SHIFT_ENDPOINT}/configs`);
  },

  getScheduleHistory: async (): Promise<{
    success: boolean;
    data: ScheduleChangeHistoryItem[];
  }> => {
    return await axiosClient.get(`${WORK_SHIFT_ENDPOINT}/history`);
  },
};
