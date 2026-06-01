import { useCallback, useEffect, useState } from "react";
import { useNotification } from "../../../core/context/NotificationContext";
import { payrollApi } from "../api/payrollApi";
import type { CreatePayrollAdjustmentRequest, PayrollAdjustment } from "../types/payroll";

export const usePayrollAdjustments = (month: number, year: number) => {
  const { triggerAlert } = useNotification();
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [adjustments, setAdjustments] = useState<PayrollAdjustment[]>([]);

  const loadAdjustments = useCallback(async () => {
    setLoading(true);
    try {
      const response = await payrollApi.getAdjustments(month, year);
      setAdjustments(response.data ?? []);
    } catch (error) {
      console.error(error);
      triggerAlert("error", "Không tải được điều chỉnh lương", "Vui lòng thử lại sau.");
    } finally {
      setLoading(false);
    }
  }, [month, year, triggerAlert]);

  useEffect(() => {
    void loadAdjustments();
  }, [loadAdjustments]);

  const createAdjustment = async (payload: CreatePayrollAdjustmentRequest) => {
    setSaving(true);
    try {
      await payrollApi.createAdjustment(payload);
      triggerAlert(
        "success",
        "Đã tạo khoản điều chỉnh",
        "Khoản điều chỉnh hợp lệ sẽ được đưa vào kỳ lương ghi nhận.",
      );
      await loadAdjustments();
      return true;
    } catch (error) {
      console.error(error);
      triggerAlert("error", "Không tạo được khoản điều chỉnh", "Vui lòng kiểm tra dữ liệu nhập.");
      return false;
    } finally {
      setSaving(false);
    }
  };

  return {
    loading,
    saving,
    adjustments,
    loadAdjustments,
    createAdjustment,
  };
};
