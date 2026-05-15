import { useState, useEffect, useCallback } from "react";
import { attendanceConfigApi } from "../api/attendanceConfigApi";
import type { AttendanceConfig } from "../types/attendanceConfig";

export const useAttendanceConfig = () => {
  const [config, setConfig] = useState<AttendanceConfig | null>(null);
  const [loading, setLoading] = useState<boolean>(false);

  const fetchConfig = useCallback(async () => {
    setLoading(true);
    try {
      const res = (await attendanceConfigApi.get()) as {
        data?: AttendanceConfig;
      };
      if (res.data) setConfig(res.data);
    } catch (error) {
      console.error("Lỗi tải cấu hình chấm công:", error);
    } finally {
      setLoading(false);
    }
  }, []);

  const updateConfig = async (payload: AttendanceConfig) => {
    try {
      const res = (await attendanceConfigApi.update(payload)) as unknown;
      await fetchConfig(); // Refresh dữ liệu hiển thị
      return res;
    } catch (error: unknown) {
      throw (
        (error as { response?: { data?: { message?: string } } }).response?.data
          ?.message || "Lỗi hệ thống khi cập nhật"
      );
    }
  };

  useEffect(() => {
    fetchConfig();
  }, [fetchConfig]);

  return { config, loading, updateConfig };
};
