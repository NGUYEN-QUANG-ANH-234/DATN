import { useState, useEffect, useCallback } from "react";
import { slaApi } from "../api/slaApi";
import type { SlaConfig, SlaUpdateRequest } from "../types/sla";

export const useSla = () => {
  const [slas, setSlas] = useState<SlaConfig[]>([]);
  const [loading, setLoading] = useState<boolean>(false);

  const fetchSlas = useCallback(async () => {
    setLoading(true);
    try {
      const res = (await slaApi.getAll()) as unknown;
      // Linh hoạt xử lý cả 2 trường hợp bóc vỏ và chưa bóc vỏ
      if (Array.isArray(res)) {
        setSlas(res);
      } else if (
        res &&
        typeof res === "object" &&
        Array.isArray((res as { data?: unknown }).data)
      ) {
        setSlas((res as { data: SlaConfig[] }).data);
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
      // Gọi fetchSlas() ngay lập tức mà không cần check res.success cứng ngắc
      await fetchSlas();
      return res;
    } catch (error: unknown) {
      throw (
        (error as { response?: { data?: { message?: string } } }).response?.data
          ?.message || "Lỗi hệ thống khi cập nhật SLA"
      );
    }
  };

  useEffect(() => {
    fetchSlas();
  }, [fetchSlas]);

  return { slas, loading, updateSla };
};
