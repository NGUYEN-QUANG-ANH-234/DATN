import { useState } from "react";
import { candidateApi } from "../api/candidateApi";
import type { ApplyJobPayload } from "../types/candidate";
import { useNotification } from "../../../core/context/NotificationContext";

export interface ApplyJobResult {
  candidateId?: number;
  trackingCode: string;
}

export const useApplyJob = () => {
  const [loading, setLoading] = useState(false);
  const { triggerAlert } = useNotification();

  const handleApply = async (
    payload: ApplyJobPayload,
  ): Promise<ApplyJobResult | null> => {
    setLoading(true);
    try {
      const response = await candidateApi.applyForJob(payload);

      const responseData =
        typeof response.data === "object" && response.data !== null
          ? (response.data as { candidateId?: number; trackingCode?: string })
          : null;

      const trackingCode = responseData?.trackingCode
        ? String(responseData.trackingCode)
        : response.data
          ? String(response.data)
          : "";

      triggerAlert(
        "success",
        "Nộp hồ sơ thành công",
        trackingCode
          ? `Mã tra cứu hồ sơ: ${trackingCode}. HICAS cũng đã gửi mã này về email của bạn.`
          : response.message || "Đã gửi thông tin ứng tuyển.",
      );

      return {
        candidateId: responseData?.candidateId,
        trackingCode,
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
