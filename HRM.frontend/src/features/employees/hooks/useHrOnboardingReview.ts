import { useState, useEffect, useCallback } from "react";
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
      console.error("Lỗi tải danh sách Onboarding:", error);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchRequests();
  }, [fetchRequests]);

  const executeReview = async (
    id: number,
    isApproved: boolean,
    roleId?: number,
    rejectReason?: string,
  ) => {
    setProcessingId(id);
    try {
      const response: unknown = await onboardingApi.reviewRequest(id, {
        isApproved,
        roleId,
        rejectReason,
      });
      alert(
        "✅ " + (response as { message?: string; Message?: string })?.message ||
          (response as { message?: string; Message?: string })?.Message ||
          "Thao tác thành công!",
      );
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
      alert("❌ " + errMsg);
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
