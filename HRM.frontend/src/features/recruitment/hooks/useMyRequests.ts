import { useState, useEffect, useCallback } from "react";
import { recruitmentApi } from "../api/recruitmentApi";
import type { MyRequestRecord } from "../types/recruitment";

export const useMyRequests = () => {
  const [myRequests, setMyRequests] = useState<MyRequestRecord[]>([]);

  const fetchMyRequests = useCallback(async () => {
    try {
      const res: unknown = await recruitmentApi.getMyRequests();
      const rawData = res as {
        data?: MyRequestRecord[];
        Data?: MyRequestRecord[];
      };
      setMyRequests(rawData.data || rawData.Data || []);
    } catch (error) {
      console.error("Lỗi tải lịch sử đơn:", error);
    }
  }, []);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    fetchMyRequests();
  }, [fetchMyRequests]);

  return { myRequests, fetchMyRequests };
};
