import { useCallback, useEffect, useMemo, useState } from "react";
import { useNotification } from "../../../core/context/NotificationContext";
import { payrollApi } from "../api/payrollApi";
import type { SalarySlip } from "../types/payroll";

type SalarySlipAccessMode = "self" | "scope";

const byLatestSlip = (a: SalarySlip, b: SalarySlip) => {
  const aTime = new Date(a.lockedAt || a.approvedAt || a.calculatedAt || 0).getTime();
  const bTime = new Date(b.lockedAt || b.approvedAt || b.calculatedAt || 0).getTime();

  if (aTime !== bTime) return bTime - aTime;
  return b.id - a.id;
};

export const useMySalarySlips = (period: string, mode: SalarySlipAccessMode = "self") => {
  const { triggerAlert } = useNotification();
  const [loading, setLoading] = useState(false);
  const [slips, setSlips] = useState<SalarySlip[]>([]);
  const [activeSlip, setActiveSlip] = useState<SalarySlip | null>(null);

  const loadSlips = useCallback(async () => {
    setLoading(true);
    try {
      const response =
        mode === "scope"
          ? await payrollApi.getSalarySlips(period)
          : await payrollApi.getMySalarySlips(period);
      const nextSlips = [...(response.data ?? [])].sort(byLatestSlip);
      setSlips(nextSlips);

      if (nextSlips[0]) {
        const detail =
          mode === "scope"
            ? await payrollApi.getSalarySlipDetail(nextSlips[0].id)
            : await payrollApi.getMySalarySlipDetail(nextSlips[0].id);
        setActiveSlip(detail.data);
      } else {
        setActiveSlip(null);
      }
    } catch (error) {
      console.error(error);
      triggerAlert("error", "Không tải được phiếu lương", "Vui lòng thử lại sau hoặc liên hệ bộ phận nhân sự.");
      setSlips([]);
      setActiveSlip(null);
    } finally {
      setLoading(false);
    }
  }, [mode, period, triggerAlert]);

  useEffect(() => {
    void loadSlips();
  }, [loadSlips]);

  const openSlip = async (id: number) => {
    setLoading(true);
    try {
      const response =
        mode === "scope"
          ? await payrollApi.getSalarySlipDetail(id)
          : await payrollApi.getMySalarySlipDetail(id);
      setActiveSlip(response.data);
    } catch (error) {
      console.error(error);
      triggerAlert("error", "Không mở được phiếu lương", "Phiếu lương này chưa sẵn sàng hoặc bạn không có quyền xem.");
    } finally {
      setLoading(false);
    }
  };

  const summary = useMemo(() => {
    const slip = activeSlip ?? slips[0] ?? null;
    if (!slip) return null;

    const totalDeductions =
      Number(slip.employeeInsuranceAmount || 0) +
      Number(slip.pitAmount || 0) +
      Number(slip.otherDeductions || 0);

    const totalIncome = Number(slip.grossIncome || 0);
    const netRatio = totalIncome > 0 ? Math.max(0, Math.min(100, (Number(slip.netSalary || 0) / totalIncome) * 100)) : 0;
    const deductionRatio = totalIncome > 0 ? Math.max(0, Math.min(100, (totalDeductions / totalIncome) * 100)) : 0;

    return {
      slip,
      totalIncome,
      totalDeductions,
      netRatio,
      deductionRatio,
    };
  }, [activeSlip, slips]);

  return {
    loading,
    slips,
    activeSlip,
    summary,
    loadSlips,
    openSlip,
  };
};
