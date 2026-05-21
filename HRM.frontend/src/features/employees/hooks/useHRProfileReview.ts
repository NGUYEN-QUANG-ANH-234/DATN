import { useState, useEffect, useCallback } from "react";
import { hrProfileApi } from "../api/hrProfileApi";
import type { PendingProfileRequest } from "../types/profileRequest";

export const useHRProfileReview = () => {
  const [requests, setRequests] = useState<PendingProfileRequest[]>([]);
  const [loading, setLoading] = useState(false);
  const [processingId, setProcessingId] = useState<number | null>(null);

  const fetchPendingRequests = useCallback(async () => {
    setLoading(true);
    try {
      const res = await hrProfileApi.getPendingRequests();
      setRequests(res.data || []);
    } catch (error) {
      console.error("Lỗi tải danh sách chờ duyệt:", error);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchPendingRequests();
  }, [fetchPendingRequests]);

  const executeReview = async (
    id: number,
    isApproved: boolean,
    rejectReason?: string,
  ) => {
    setProcessingId(id);
    try {
      const res = await hrProfileApi.reviewRequest(id, {
        isApproved,
        rejectReason,
      });
      alert(res.message);
      // Xóa thành công thì loại khỏi mảng hiện tại
      setRequests((prev) => prev.filter((r) => r.id !== id));
      return true;
    } catch (error: unknown) {
      alert(
        (error as { response?: { data?: { message?: string } } }).response?.data
          ?.message || "Có lỗi xảy ra khi xử lý.",
      );
      return false;
    } finally {
      setProcessingId(null);
    }
  };

  return { requests, loading, processingId, executeReview };
};
