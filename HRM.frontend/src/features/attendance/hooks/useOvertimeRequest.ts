import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { useCurrentUser } from "../../../core/auth/hooks/useCurrentUser";
import { useNotification } from "../../../core/context/NotificationContext";
import {
  overtimeApi,
  type OvertimeEmployeeOption,
  type OvertimeRequest,
} from "../api/overtimeApi";
import type { OvertimeFormState } from "../types/overtimeRequest";

const today = new Date().toISOString().slice(0, 10);

const initialForm: OvertimeFormState = {
  employeeId: "",
  workDate: today,
  startTime: "18:00",
  endTime: "20:00",
  reason: "",
  projectCode: "",
};

export const useOvertimeRequest = () => {
  const { user } = useCurrentUser();
  const role = user?.role || "";
  const { triggerAlert } = useNotification();

  const [form, setForm] = useState<OvertimeFormState>(initialForm);
  const [loading, setLoading] = useState(false);
  const [myRequests, setMyRequests] = useState<OvertimeRequest[]>([]);
  const [employeeOptions, setEmployeeOptions] = useState<
    OvertimeEmployeeOption[]
  >([]);

  const canCreateForOther = useMemo(
    () => ["Manager", "HR", "Admin"].includes(role),
    [role],
  );
  const canCreateBulk = useMemo(
    () => ["Manager", "Admin"].includes(role),
    [role],
  );
  const selectedEmployeeIds = useMemo(
    () => parseEmployeeIds(form.employeeId).map(String),
    [form.employeeId],
  );

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const myRes = await overtimeApi.getMy();
      setMyRequests(myRes.data);

      if (canCreateForOther) {
        try {
          const employeeRes = await overtimeApi.getAssignableEmployees();
          setEmployeeOptions(employeeRes.data);
        } catch {
          setEmployeeOptions([]);
        }
      }
    } finally {
      setLoading(false);
    }
  }, [canCreateForOther]);

  useEffect(() => {
    void fetchData();
  }, [fetchData]);

  const submitRequest = async (event: FormEvent) => {
    event.preventDefault();
    setLoading(true);
    try {
      const targetIds = parseEmployeeIds(form.employeeId);
      const payload = {
        workDate: form.workDate,
        startTime: `${form.startTime}:00`,
        endTime: `${form.endTime}:00`,
        reason: form.reason,
        projectCode: form.projectCode || null,
      };

      if (targetIds.length > 1) {
        if (!canCreateBulk) {
          triggerAlert(
            "error",
            "Không thể tạo yêu cầu hàng loạt",
            "Chỉ Trưởng phòng hoặc Admin được tạo yêu cầu làm thêm cho danh sách nhân viên.",
          );
          return;
        }

        await overtimeApi.createBulk({
          ...payload,
          employeeIds: targetIds,
        });
      } else {
        await overtimeApi.create({
          ...payload,
          employeeId: targetIds[0] ?? null,
        });
      }

      triggerAlert(
        "success",
        "Đã gửi yêu cầu làm thêm",
        "Yêu cầu làm thêm giờ đã được ghi nhận.",
      );
      setForm((prev) => ({ ...prev, reason: "", projectCode: "" }));
      await fetchData();
    } catch (error) {
      triggerAlert("error", "Không thể gửi yêu cầu làm thêm", getErrorMessage(error));
    } finally {
      setLoading(false);
    }
  };

  return {
    form,
    setForm,
    loading,
    myRequests,
    employeeOptions,
    canCreateForOther,
    canCreateBulk,
    selectedEmployeeIds,
    submitRequest,
  };
};

const parseEmployeeIds = (value: string) =>
  value
    .split(/[\s,;]+/)
    .map((item) => Number(item.trim()))
    .filter((item) => Number.isInteger(item) && item > 0);

const getErrorMessage = (error: unknown) =>
  error instanceof Error ? error.message : "Đã có lỗi xảy ra.";
