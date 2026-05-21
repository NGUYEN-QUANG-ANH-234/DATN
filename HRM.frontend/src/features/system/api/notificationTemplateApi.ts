import axiosClient from "../../../core/api/axiosClient";
import type { BaseResponse } from "../types/salaryVariable"; // Tái sử dụng BaseResponse
import type { NotificationTemplate } from "../types/notificationTemplate";

const ENDPOINT = "/system/notification-templates";

export const notificationTemplateApi = {
  getAll: async (): Promise<BaseResponse<NotificationTemplate[]>> => {
    return await axiosClient.get(ENDPOINT);
  },

  update: async (
    templateKey: string,
    payload: NotificationTemplate,
  ): Promise<BaseResponse<null>> => {
    return await axiosClient.put(
      `${ENDPOINT}/${templateKey}`,
      payload,
    );
  },
};
