import { useCallback, useEffect, useState } from "react";
import { onboardingApi } from "../api/onboardingApi";
import type { PendingOnboardingRequest } from "../types/onboarding";

export const useHrOnboardingReview = () => {
  const [requests, setRequests] = useState<PendingOnboardingRequest[]>([]);
  const [loading, setLoading] = useState(false);
  const [processingId, setProcessingId] = useState<number | null>(null);

  const fetchRequests = useCallback(async () => {
    setLoading(true);
    try {
      const res = await onboardingApi.getPendingRequests();
      setRequests(res.data || []);
    } catch (error) {
      console.error("Lỗi tải danh sách onboarding:", error);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void fetchRequests();
  }, [fetchRequests]);

  const executeReview = async (
    id: number,
    isApproved: boolean,
    roleId?: number,
    departmentId?: number,
    positionId?: number,
    rejectReason?: string,
  ) => {
    setProcessingId(id);
    try {
      const response: unknown = await onboardingApi.reviewRequest(id, {
        isApproved,
        roleId,
        departmentId,
        positionId,
        rejectReason,
      });
      const message =
        (response as { message?: string; Message?: string })?.message ||
        (response as { message?: string; Message?: string })?.Message ||
        "Thao tác thành công.";
      alert(`✅ ${message}`);
      setRequests((prev) => prev.filter((r) => r.id !== id));
      return true;
    } catch (error: unknown) {
      const errMsg =
        (
          error as {
            response?: { data?: { message?: string; Message?: string } };
          }
        ).response?.data?.message ||
        (
          error as {
            response?: { data?: { message?: string; Message?: string } };
          }
        ).response?.data?.Message ||
        "Có lỗi xảy ra khi duyệt.";
      alert(`❌ ${errMsg}`);
      return false;
    } finally {
      setProcessingId(null);
    }
  };

  return {
    requests,
    loading,
    processingId,
    executeReview,
    refresh: fetchRequests,
  };
};
