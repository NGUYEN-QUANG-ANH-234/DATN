import { useState, useCallback, useEffect } from "react";
import { auditLogApi } from "../api/auditLogApi";
import type { AuditLog, AuditLogFilter } from "../types/auditLog";

export const useAuditLogs = () => {
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [loading, setLoading] = useState<boolean>(false);

  const fetchLogs = useCallback(async (filter?: AuditLogFilter) => {
    setLoading(true);
    try {
      // Ép kiểu any tạm thời để linh hoạt check dữ liệu
      const res = (await auditLogApi.getLogs(filter)) as unknown;

      // XỬ LÝ AN TOÀN: Bất chấp Axios bóc tách kiểu gì cũng nhận được dữ liệu
      if (Array.isArray(res)) {
        // Trường hợp 1: res đã là mảng
        setLogs(res);
      } else if (res && Array.isArray((res as { data: unknown }).data)) {
        // Trường hợp 2: res là object { success: true, data: [...] }
        setLogs((res as { data: AuditLog[] }).data);
      } else {
        // Dự phòng
        setLogs([]);
      }
    } catch (error) {
      console.error("Lỗi tải Audit Logs:", error);
    } finally {
      setLoading(false);
    }
  }, []);

  // Tải danh sách mặc định khi mở trang
  useEffect(() => {
    fetchLogs();
  }, [fetchLogs]);

  return { logs, loading, fetchLogs };
};
