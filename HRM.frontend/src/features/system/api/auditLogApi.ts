import axiosClient from "../../../core/api/axiosClient";
import type { AuditLog, AuditLogFilter } from "../types/auditLog";

const ENDPOINT = "/system/audit-logs";

export const auditLogApi = {
  getLogs: async (
    filter?: AuditLogFilter,
  ): Promise<{ success: boolean; data: AuditLog[] }> => {
    // Lọc bỏ các param rỗng để URL sạch hơn
    const cleanedFilter = Object.fromEntries(
      Object.entries(filter || {}).filter(
        // eslint-disable-next-line @typescript-eslint/no-unused-vars
        ([_, v]) => v !== "" && v !== null && v !== undefined,
      ),
    );

    return await axiosClient.get(ENDPOINT, { params: cleanedFilter });
  },
};
