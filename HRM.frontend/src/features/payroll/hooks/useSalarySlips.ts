import { useCallback, useEffect, useState } from "react";
import { useNotification } from "../../../core/context/NotificationContext";
import { payrollApi } from "../api/payrollApi";
import type { SalarySlip } from "../types/payroll";

export const useSalarySlips = (period: string) => {
  const { triggerAlert } = useNotification();
  const [loading, setLoading] = useState(false);
  const [slips, setSlips] = useState<SalarySlip[]>([]);
  const [selectedIds, setSelectedIds] = useState<number[]>([]);
  const [activeSlip, setActiveSlip] = useState<SalarySlip | null>(null);

  const loadSlips = useCallback(async () => {
    setLoading(true);
    try {
      const response = await payrollApi.getSalarySlips(period);
      setSlips(response.data ?? []);
      setSelectedIds([]);
      setActiveSlip(null);
    } catch (error) {
      console.error(error);
      triggerAlert("error", "Không tải được phiếu lương", "Vui lòng thử lại sau hoặc liên hệ quản trị hệ thống.");
    } finally {
      setLoading(false);
    }
  }, [period, triggerAlert]);

  useEffect(() => {
    void loadSlips();
  }, [loadSlips]);

  const openDetail = async (id: number) => {
    setLoading(true);
    try {
      const response = await payrollApi.getSalarySlipDetail(id);
      setActiveSlip(response.data);
    } catch (error) {
      console.error(error);
      triggerAlert("error", "Không mở được phiếu lương", "Chi tiết phiếu lương chưa sẵn sàng.");
    } finally {
      setLoading(false);
    }
  };

  const toggleSelected = (id: number) => {
    setSelectedIds((current) =>
      current.includes(id) ? current.filter((item) => item !== id) : [...current, id],
    );
  };

  const setSelectedSlipIds = (ids: number[]) => {
    setSelectedIds(Array.from(new Set(ids)));
  };

  const selectSlips = (ids: number[]) => {
    setSelectedIds((current) => Array.from(new Set([...current, ...ids])));
  };

  const unselectSlips = (ids: number[]) => {
    const removeIds = new Set(ids);
    setSelectedIds((current) => current.filter((id) => !removeIds.has(id)));
  };

  const clearSelected = () => {
    setSelectedIds([]);
  };

  const exportSelected = async () => {
    if (selectedIds.length === 0) {
      triggerAlert("warning", "Chưa chọn phiếu lương", "Vui lòng chọn ít nhất một phiếu để kết xuất.");
      return;
    }

    setLoading(true);
    try {
      const blob = await payrollApi.exportSalarySlips(selectedIds);
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `salary-slips-${period}.csv`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
      triggerAlert(
        "success",
        "Đã kết xuất phiếu lương",
        "File đã được tạo từ dữ liệu phiếu lương được phép truy cập.",
      );
    } catch (error) {
      console.error(error);
      triggerAlert("error", "Không kết xuất được phiếu lương", "Vui lòng thử lại sau.");
    } finally {
      setLoading(false);
    }
  };

  return {
    loading,
    slips,
    selectedIds,
    activeSlip,
    loadSlips,
    openDetail,
    toggleSelected,
    setSelectedSlipIds,
    selectSlips,
    unselectSlips,
    clearSelected,
    exportSelected,
  };
};
