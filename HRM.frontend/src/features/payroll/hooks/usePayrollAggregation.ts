import { useMemo, useState } from "react";
import { useCurrentUser } from "../../../core/auth/hooks/useCurrentUser";
import { useNotification } from "../../../core/context/NotificationContext";
import { payrollApi } from "../api/payrollApi";
import type { PayrollCalculationResult } from "../types/payroll";
import { useSalarySlips } from "./useSalarySlips";

export const usePayrollAggregation = (month: number, year: number, period: string) => {
  const { user } = useCurrentUser();
  const { triggerAlert } = useNotification();
  const salarySlipsState = useSalarySlips(period);
  const [calculating, setCalculating] = useState(false);
  const [calculationResult, setCalculationResult] = useState<PayrollCalculationResult | null>(null);

  const canManagePayroll = useMemo(() => {
    return ["Admin", "HR", "Director"].includes(user?.role || "");
  }, [user?.role]);

  const calculatePayroll = async () => {
    if (!canManagePayroll) {
      triggerAlert(
        "warning",
        "Không có quyền tổng hợp lương",
        "Tài khoản hiện tại chỉ có quyền tra cứu theo phạm vi được cấp.",
      );
      return;
    }

    setCalculating(true);
    try {
      const response = await payrollApi.calculate(month, year);
      setCalculationResult(response.data);
      const warnings = response.data?.warnings ?? [];
      triggerAlert(
        warnings.length > 0 ? "warning" : "success",
        "Đã tổng hợp bảng lương",
        warnings.length > 0
          ? `Tạo ${response.data?.createdCount ?? 0} phiếu, bỏ qua ${response.data?.skippedCount ?? 0} hồ sơ.`
          : response.message || "Bảng lương nháp đã được tạo.",
      );
      await salarySlipsState.loadSlips();
    } catch (error) {
      console.error(error);
      triggerAlert(
        "error",
        "Không thể tổng hợp bảng lương",
        "Vui lòng kiểm tra dữ liệu công, hợp đồng và cấu hình lương.",
      );
    } finally {
      setCalculating(false);
    }
  };

  return {
    ...salarySlipsState,
    calculating,
    calculationResult,
    canManagePayroll,
    calculatePayroll,
  };
};
