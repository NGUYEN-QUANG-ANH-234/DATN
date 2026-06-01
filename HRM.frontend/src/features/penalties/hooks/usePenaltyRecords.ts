import { useCallback, useEffect, useState } from "react";
import { useNotification } from "../../../core/context/NotificationContext";
import { penaltyApi } from "../api/penaltyApi";
import type { CreateManualPenaltyRecordRequest, PenaltyRecord } from "../types/penalty";

export const usePenaltyRecords = () => {
  const { triggerAlert } = useNotification();
  const [records, setRecords] = useState<PenaltyRecord[]>([]);
  const [historyRecords, setHistoryRecords] = useState<PenaltyRecord[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [status, setStatus] = useState("");

  const loadRecords = useCallback(async (nextStatus = status) => {
    setLoading(true);
    try {
      const response = await penaltyApi.getRecords(nextStatus);
      setRecords(response.data ?? []);
    } catch (error) {
      console.error(error);
      triggerAlert("error", "Không tải được biên bản vi phạm", "Vui lòng thử lại sau.");
    } finally {
      setLoading(false);
    }
  }, [status, triggerAlert]);

  useEffect(() => {
    void loadRecords();
  }, [loadRecords]);

  const updateStatus = (nextStatus: string) => {
    setStatus(nextStatus);
    void loadRecords(nextStatus);
  };

  const createManualRecord = async (payload: CreateManualPenaltyRecordRequest) => {
    setSaving(true);
    try {
      await penaltyApi.createManual(payload);
      triggerAlert("success", "Đã ghi nhận biên bản", "Biên bản đã chuyển vào luồng giải trình/HR duyệt.");
      await loadRecords();
      return true;
    } catch (error) {
      console.error(error);
      triggerAlert("error", "Không tạo được biên bản", "Vui lòng kiểm tra dữ liệu nhập.");
      return false;
    } finally {
      setSaving(false);
    }
  };

  const loadEmployeeHistory = async (employeeId: number) => {
    if (!employeeId) {
      setHistoryRecords([]);
      return;
    }

    setLoading(true);
    try {
      const response = await penaltyApi.getEmployeeHistory(employeeId);
      setHistoryRecords(response.data ?? []);
    } catch (error) {
      console.error(error);
      triggerAlert("error", "Không tải được lịch sử điểm trừ", "Vui lòng thử lại sau.");
    } finally {
      setLoading(false);
    }
  };

  return {
    records,
    historyRecords,
    loading,
    saving,
    status,
    setStatus: updateStatus,
    loadRecords,
    createManualRecord,
    loadEmployeeHistory,
  };
};
