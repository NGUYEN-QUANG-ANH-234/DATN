import { useState, useCallback, useEffect } from "react";
import { scheduleApi } from "../api/scheduleApi";
import type {
  ConfiguredScheduleItem,
  LeaveTypeSelect,
  ConfigureWorkScheduleDto,
  ScheduleChangeHistoryItem,
} from "../types/scheduleConfig";
import type { DepartmentTree } from "../../organization/types/department";
import { useNotification } from "../../../core/context/NotificationContext";

export const useScheduleConfig = () => {
  const [departments, setDepartments] = useState<DepartmentTree[]>([]);
  const [leaveTypes, setLeaveTypes] = useState<LeaveTypeSelect[]>([]);
  const [configuredSchedules, setConfiguredSchedules] = useState<
    ConfiguredScheduleItem[]
  >([]);
  const [history, setHistory] = useState<ScheduleChangeHistoryItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const { triggerAlert } = useNotification();

  const loadMasterData = useCallback(async () => {
    setLoading(true);
    try {
      const [deptRes, leaveRes, configRes, historyRes] = await Promise.all([
        scheduleApi.getDepartments(),
        scheduleApi.getLeaveTypes(),
        scheduleApi.getConfiguredSchedules(),
        scheduleApi.getScheduleHistory(),
      ]);
      setDepartments(deptRes.data || []);
      setLeaveTypes(leaveRes.data || []);
      setConfiguredSchedules(configRes.data || []);
      setHistory(historyRes.data || []);
    } catch (error) {
      console.error("Lỗi hệ thống:", error);
    } finally {
      setLoading(false);
    }
  }, []);

  const handleSaveConfig = async (data: ConfigureWorkScheduleDto) => {
    setSubmitting(true);
    try {
      await scheduleApi.configureSchedule(data);
      triggerAlert(
        "success",
        "Đã lưu cấu hình",
        "Thiết lập ca và quỹ phép bộ phận thành công.",
      );

      const configRes = await scheduleApi.getConfiguredSchedules();
      const historyRes = await scheduleApi.getScheduleHistory();
      setConfiguredSchedules(configRes.data || []);
      setHistory(historyRes.data || []);
      return true;
    } catch (error: unknown) {
      const message =
        error && typeof error === "object" && "response" in error
          ? (error as { response?: { data?: { message?: string } } }).response
              ?.data?.message
          : undefined;
      triggerAlert(
        "error",
        "Lỗi khi lưu",
        message || "Không thể lưu cấu hình.",
      );
      return false;
    } finally {
      setSubmitting(false);
    }
  };

  useEffect(() => {
    loadMasterData();
  }, [loadMasterData]);

  return {
    departments,
    leaveTypes,
    configuredSchedules,
    history,
    loading,
    submitting,
    handleSaveConfig,
  };
};
