import { useState, useEffect, useCallback } from "react";
import { notificationTemplateApi } from "../api/notificationTemplateApi";
import type { NotificationTemplate } from "../types/notificationTemplate";

export const useNotificationTemplate = () => {
  const [templates, setTemplates] = useState<NotificationTemplate[]>([]);
  const [loading, setLoading] = useState<boolean>(false);

  const fetchTemplates = useCallback(async () => {
    setLoading(true);
    try {
      const res = (await notificationTemplateApi.getAll()) as unknown;
      if (Array.isArray(res)) setTemplates(res);
      else if (
        res &&
        typeof res === "object" &&
        Array.isArray((res as { data: unknown }).data)
      )
        setTemplates((res as { data: NotificationTemplate[] }).data);
    } catch (error) {
      console.error("Lỗi tải mẫu thông báo:", error);
    } finally {
      setLoading(false);
    }
  }, []);

  const updateTemplate = async (
    templateKey: string,
    payload: NotificationTemplate,
  ) => {
    try {
      const res = (await notificationTemplateApi.update(
        templateKey,
        payload,
      )) as unknown;
      await fetchTemplates(); // Refresh sau khi cập nhật
      return res;
    } catch (error: unknown) {
      throw (
        (error as { response?: { data?: { message?: string } } }).response?.data
          ?.message || "Lỗi hệ thống khi cập nhật mẫu"
      );
    }
  };

  useEffect(() => {
    fetchTemplates();
  }, [fetchTemplates]);

  return { templates, loading, updateTemplate };
};
