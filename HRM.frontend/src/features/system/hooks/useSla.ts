import { useCallback, useEffect, useState } from "react";
import { slaApi } from "../api/slaApi";
import type { SlaConfig, SlaUpdateRequest } from "../types/sla";

export const useSla = () => {
  const [slas, setSlas] = useState<SlaConfig[]>([]);
  const [loading, setLoading] = useState<boolean>(false);

  const fetchSlas = useCallback(async () => {
    setLoading(true);
    try {
      const res = (await slaApi.getAll()) as unknown;
      if (Array.isArray(res)) {
        setSlas(normalizeSlas(res));
      } else if (
        res &&
        typeof res === "object" &&
        Array.isArray((res as { data?: unknown }).data)
      ) {
        setSlas(normalizeSlas((res as { data: SlaConfig[] }).data));
      }
    } catch (error) {
      console.error("Lỗi khi tải danh sách SLA:", error);
    } finally {
      setLoading(false);
    }
  }, []);

  const updateSla = async (payload: SlaUpdateRequest) => {
    try {
      const res = (await slaApi.update(payload)) as unknown;
      await fetchSlas();
      return res;
    } catch (error: unknown) {
      throw (
        (error as { response?: { data?: { message?: string } } }).response?.data
          ?.message || "Lỗi hệ thống khi cập nhật SLA"
      );
    }
  };

  const setSlaActive = async (moduleCode: string, isActive: boolean) => {
    try {
      const res = (await slaApi.setActive(moduleCode, { isActive })) as unknown;
      await fetchSlas();
      return res;
    } catch (error: unknown) {
      throw (
        (error as { response?: { data?: { message?: string } } }).response?.data
          ?.message || "Lỗi hệ thống khi cập nhật trạng thái SLA"
      );
    }
  };

  useEffect(() => {
    fetchSlas();
  }, [fetchSlas]);

  return { slas, loading, updateSla, setSlaActive };
};

const normalizeSlas = (items: SlaConfig[]): SlaConfig[] =>
  items.map((item) => {
    const raw = item as SlaConfig & {
      Code?: string;
      ModuleCode?: string;
      DisplayName?: string;
      ModuleName?: string;
      Description?: string;
      Value?: string;
      Unit?: string;
      IsActive?: boolean;
    };
    const code = item.code || item.moduleCode || raw.Code || raw.ModuleCode || "";

    return {
      ...item,
      code,
      moduleCode: item.moduleCode || raw.ModuleCode || code,
      displayName: item.displayName || raw.DisplayName || code,
      moduleName: item.moduleName || raw.ModuleName || "Quy trình hệ thống",
      description: item.description || raw.Description || "",
      value: item.value || raw.Value || "",
      unit: item.unit || raw.Unit || "HOURS",
      isActive: item.isActive ?? raw.IsActive ?? true,
    };
  });
