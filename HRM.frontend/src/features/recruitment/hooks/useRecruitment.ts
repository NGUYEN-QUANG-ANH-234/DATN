import { useState, useCallback } from "react";
import { recruitmentApi } from "../api/recruitmentApi";
import type { CreateRecruitmentPayload, ActiveJob } from "../types/recruitment";

export const useRecruitment = () => {
  const [loading, setLoading] = useState(false);
  const [activeJobs, setActiveJobs] = useState<ActiveJob[]>([]);

  const fetchActiveJobs = useCallback(async () => {
    setLoading(true);
    try {
      const res = await recruitmentApi.getActiveJobs();
      setActiveJobs(res.data || []);
    } catch (error) {
      console.error("Lỗi tải danh sách việc làm:", error);
    } finally {
      setLoading(false);
    }
  }, []);

  const handleCreateRequest = async (
    payload: CreateRecruitmentPayload,
  ): Promise<boolean> => {
    setLoading(true);
    try {
      await recruitmentApi.createRequest(payload);
      alert(
        "✅ Đã gửi đề xuất tuyển dụng! Hệ thống đang chờ các cấp phê duyệt.",
      );
      return true;
    } catch (error: unknown) {
      alert(
        "❌ " +
          (error as { response?: { data?: { message?: string } } }).response
            ?.data?.message || "Lỗi khi tạo đề xuất",
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  const handleReviewRequest = async (
    moduleCode: string,
    referenceId: number,
    isApproved: boolean,
    note?: string,
  ) => {
    if (
      !window.confirm(
        `Bạn chắc chắn muốn ${isApproved ? "DUYỆT" : "TỪ CHỐI"} yêu cầu này?`,
      )
    )
      return;

    setLoading(true);
    try {
      // ĐÃ SỬA: Gọi API chuẩn xác
      await recruitmentApi.reviewRequest({
        moduleCode,
        referenceId,
        isApproved,
        note: note || "",
      });
      alert("✅ Thao tác phê duyệt thành công!");
    } catch (error: unknown) {
      alert(
        "❌ " +
          (error as { response?: { data?: { message?: string } } }).response
            ?.data?.message || "Lỗi khi phê duyệt",
      );
    } finally {
      setLoading(false);
    }
  };

  return {
    loading,
    activeJobs,
    fetchActiveJobs,
    handleCreateRequest,
    handleReviewRequest,
  };
};
