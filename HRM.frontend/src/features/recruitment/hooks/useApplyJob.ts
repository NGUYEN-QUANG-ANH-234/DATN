import { useState } from "react";
import { candidateApi } from "../api/candidateApi";
import type { ApplyJobPayload } from "../types/candidate";
import { useNotification } from "../../../core/context/NotificationContext";

// 1. Định nghĩa kiểu dữ liệu trả về rõ ràng thay vì dùng 'unknown'
export interface ApplyJobResult {
  trackingCode: string;
}

export const useApplyJob = () => {
  const [loading, setLoading] = useState(false);
  const { triggerAlert } = useNotification();

  // 2. Đổi Promise<unknown> thành Promise<ApplyJobResult | null>
  const handleApply = async (
    payload: ApplyJobPayload,
  ): Promise<ApplyJobResult | null> => {
    setLoading(true);
    try {
      const response = await candidateApi.applyForJob(payload);

      triggerAlert(
        "success",
        "Nộp hồ sơ thành công",
        response.message || "Đã gửi thông tin ứng tuyển.",
      );

      // 3. Backend đang trả về Data là ID ứng viên (số).
      // Ta ghép thêm chữ "CAND-" để tạo thành trackingCode hợp lệ trả về cho UI.
      const rawTrackingCode =
        typeof response.data === "object" &&
        response.data !== null &&
        "trackingCode" in response.data
          ? String((response.data as { trackingCode?: string }).trackingCode)
          : response.data
            ? String(response.data)
            : "";

      return {
        trackingCode: rawTrackingCode.startsWith("CAND-")
          ? rawTrackingCode
          : `CAND-${rawTrackingCode}`,
      };
    } catch (error: unknown) {
      const err = error as { response?: { data?: { message?: string } } };
      const errorMsg =
        err.response?.data?.message ?? "Đã xảy ra lỗi khi nộp hồ sơ.";

      triggerAlert("error", "Lỗi nộp hồ sơ", errorMsg);
      return null;
    } finally {
      setLoading(false);
    }
  };

  return { loading, handleApply };
};
