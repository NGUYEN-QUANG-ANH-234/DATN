import { useCallback, useEffect, useState } from "react";
import { notificationTemplateApi } from "../api/notificationTemplateApi";
import type { NotificationTemplate, TemplateVariable } from "../types/notificationTemplate";

export const useNotificationTemplate = () => {
  const [templates, setTemplates] = useState<NotificationTemplate[]>([]);
  const [loading, setLoading] = useState<boolean>(false);

  const fetchTemplates = useCallback(async () => {
    setLoading(true);
    try {
      const res = (await notificationTemplateApi.getAll()) as unknown;
      if (Array.isArray(res)) {
        setTemplates(normalizeTemplates(res));
      } else if (
        res &&
        typeof res === "object" &&
        Array.isArray((res as { data?: unknown; Data?: unknown }).data)
      ) {
        setTemplates(normalizeTemplates((res as { data: NotificationTemplate[] }).data));
      } else if (
        res &&
        typeof res === "object" &&
        Array.isArray((res as { Data?: unknown }).Data)
      ) {
        setTemplates(normalizeTemplates((res as { Data: NotificationTemplate[] }).Data));
      }
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
      await fetchTemplates();
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

const normalizeTemplates = (items: NotificationTemplate[]): NotificationTemplate[] =>
  items.map((item) => {
    const raw = item as NotificationTemplate & {
      TemplateKey?: string;
      DisplayName?: string;
      Category?: string;
      AllowedPlaceholders?: string[];
      SystemPlaceholders?: string[];
      CustomVariables?: TemplateVariable[];
      Subject?: string;
      BodyHtml?: string;
    };
    const templateKey = item.templateKey || raw.TemplateKey || "";

    return {
      templateKey,
      displayName: item.displayName || raw.DisplayName || templateKey,
      category: item.category || raw.Category || "Mẫu hệ thống",
      allowedPlaceholders: item.allowedPlaceholders || raw.AllowedPlaceholders || [],
      systemPlaceholders: item.systemPlaceholders || raw.SystemPlaceholders || [],
      customVariables: normalizeVariables(item.customVariables || raw.CustomVariables || []),
      subject: item.subject || raw.Subject || "",
      bodyHtml: item.bodyHtml || raw.BodyHtml || "",
    };
  });

const normalizeVariables = (items: TemplateVariable[]): TemplateVariable[] =>
  items.map((item) => {
    const raw = item as TemplateVariable & {
      Code?: string;
      Label?: string;
      DataType?: TemplateVariable["dataType"];
      SourceType?: "Manual";
      IsRequired?: boolean;
      Description?: string | null;
      Placeholder?: string;
    };
    const code = item.code || raw.Code || "";

    return {
      code,
      label: item.label || raw.Label || code,
      dataType: item.dataType || raw.DataType || "Text",
      sourceType: "Manual",
      isRequired: item.isRequired ?? raw.IsRequired ?? false,
      description: item.description ?? raw.Description ?? null,
      placeholder: item.placeholder || raw.Placeholder || `{${code}}`,
    };
  });
