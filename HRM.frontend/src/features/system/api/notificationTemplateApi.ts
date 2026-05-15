import axiosClient from "../../../core/api/axiosClient";
import type { BaseResponse } from "../types/salaryVariable"; // Tái sử dụng BaseResponse
import type { NotificationTemplate } from "../types/notificationTemplate";

const ENDPOINT = "/system/notification-templates";

export const notificationTemplateApi = {
  getAll: async (): Promise<BaseResponse<NotificationTemplate[]>> => {
    const response = await axiosClient.get(ENDPOINT);
    return response.data;
  },

  update: async (
    templateKey: string,
    payload: NotificationTemplate,
  ): Promise<BaseResponse<null>> => {
    const response = await axiosClient.put(
      `${ENDPOINT}/${templateKey}`,
      payload,
    );
    return response.data || response;
  },
};
