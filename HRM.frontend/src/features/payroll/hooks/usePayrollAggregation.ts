import { useEffect, useMemo, useState } from "react";
import { useCurrentUser } from "../../../core/auth/hooks/useCurrentUser";
import { useNotification } from "../../../core/context/NotificationContext";
import { payrollApi } from "../api/payrollApi";
import type { PayrollCalculationResult, PayrollPreflight, PayrollRunSummary } from "../types/payroll";
import { useSalarySlips } from "./useSalarySlips";

export const usePayrollAggregation = (month: number, year: number, period: string) => {
  const { user } = useCurrentUser();
  const { triggerAlert } = useNotification();
  const salarySlipsState = useSalarySlips(period);
  const [calculating, setCalculating] = useState(false);
  const [preflightLoading, setPreflightLoading] = useState(false);
  const [preflight, setPreflight] = useState<PayrollPreflight | null>(null);
  const [calculationResult, setCalculationResult] = useState<PayrollCalculationResult | null>(null);
  const [runSummary, setRunSummary] = useState<PayrollRunSummary | null>(null);
  const [runActionLoading, setRunActionLoading] = useState(false);

  const canManagePayroll = useMemo(() => {
    return ["Admin", "HR"].includes(user?.role || "");
  }, [user?.role]);

  const canViewPayrollRun = useMemo(() => {
    return ["Admin", "HR", "Director"].includes(user?.role || "");
  }, [user?.role]);

  const loadPreflight = async () => {
    if (!canManagePayroll) {
      setPreflight(null);
      return;
    }

    setPreflightLoading(true);
    try {
      const response = await payrollApi.preflight(month, year);
      setPreflight(response.data);
    } catch (error) {
      console.error(error);
      setPreflight(null);
    } finally {
      setPreflightLoading(false);
    }
  };

  const loadRunSummary = async () => {
    if (!canViewPayrollRun) {
      setRunSummary(null);
      return;
    }

    try {
      const response = await payrollApi.getPayrollRunSummary(month, year);
      setRunSummary(response.data);
    } catch (error) {
      console.error(error);
      setRunSummary(null);
    }
  };

  useEffect(() => {
    void loadPreflight();
    void loadRunSummary();
  }, [month, year, canManagePayroll, canViewPayrollRun]);

  const calculatePayroll = async () => {
    if (!canManagePayroll) {
      triggerAlert(
        "warning",
        "Không có quyền tổng hợp lương",
        "Tài khoản hiện tại chỉ có quyền tra cứu theo phạm vi được cấp.",
      );
      return;
    }

    if (preflight && !preflight.canCalculate) {
      triggerAlert(
        "warning",
        "Chưa thể tổng hợp lương",
        preflight.errors?.[0] || "Vui lòng kiểm tra cấu hình pháp lý và lịch công ty trước khi tính lương.",
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
      await loadPreflight();
      await loadRunSummary();
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

  const submitPayrollRun = async () => {
    if (!canManagePayroll) return;

    setRunActionLoading(true);
    try {
      const response = await payrollApi.submitPayrollRun(month, year);
      setRunSummary(response.data);
      triggerAlert(
        "success",
        "Đã gửi duyệt bảng lương",
        response.message || `Bảng lương ${period} đã được gửi tới Giám đốc.`,
      );
      await salarySlipsState.loadSlips();
      await loadRunSummary();
    } catch (error) {
      console.error(error);
      triggerAlert(
        "error",
        "Không thể gửi duyệt bảng lương",
        "Vui lòng kiểm tra trạng thái kỳ lương trước khi gửi duyệt.",
      );
    } finally {
      setRunActionLoading(false);
    }
  };

  const lockPayrollRun = async () => {
    if (!canManagePayroll) return;

    setRunActionLoading(true);
    try {
      const response = await payrollApi.lockPayrollRun(month, year);
      setRunSummary(response.data);
      triggerAlert(
        "success",
        "Đã chốt bảng lương",
        response.message || `Bảng lương ${period} đã được chốt và khóa.`,
      );
      await salarySlipsState.loadSlips();
      await loadPreflight();
      await loadRunSummary();
    } catch (error) {
      console.error(error);
      triggerAlert(
        "error",
        "Không thể chốt bảng lương",
        "Chỉ có thể chốt sau khi bảng lương đã được duyệt.",
      );
    } finally {
      setRunActionLoading(false);
    }
  };

  const currentStatus = String(runSummary?.status || "");
  const canSubmitPayroll =
    canManagePayroll &&
    salarySlipsState.slips.length > 0 &&
    ["Calculated", "HRReviewed", "RevisionRequired"].includes(currentStatus);
  const canLockPayroll =
    canManagePayroll &&
    salarySlipsState.slips.length > 0 &&
    currentStatus === "Approved";

  return {
    ...salarySlipsState,
    calculating,
    preflightLoading,
    preflight,
    calculationResult,
    runSummary,
    runActionLoading,
    canManagePayroll,
    canCalculatePayroll: canManagePayroll && (preflight?.canCalculate ?? true),
    canSubmitPayroll,
    canLockPayroll,
    loadPreflight,
    loadRunSummary,
    calculatePayroll,
    submitPayrollRun,
    lockPayrollRun,
  };
};
