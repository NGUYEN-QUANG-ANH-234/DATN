import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import {
  FeatureCard,
  FeaturePage,
  primaryButtonClass,
} from "../../../core/components/FeatureShell";
import { useCurrentUser } from "../../../core/auth/hooks/useCurrentUser";
import { useNotification } from "../../../core/context/NotificationContext";
import {
  overtimeApi,
  type OvertimeEmployeeOption,
  type OvertimeRequest,
} from "../api/overtimeApi";
import { OvertimeTable } from "./OvertimeTable";

type OvertimeFormState = {
  employeeId: string;
  workDate: string;
  startTime: string;
  endTime: string;
  reason: string;
  projectCode: string;
};

const today = new Date().toISOString().slice(0, 10);

export const OvertimeRequestPage = () => {
  const { user } = useCurrentUser();
  const role = user?.role || "";
  const { triggerAlert } = useNotification();

  const [form, setForm] = useState<OvertimeFormState>({
    employeeId: "",
    workDate: today,
    startTime: "18:00",
    endTime: "20:00",
    reason: "",
    projectCode: "",
  });
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

  const submitRequest = async (event: React.FormEvent) => {
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
            "Không thể tạo OT hàng loạt",
            "Chỉ Trưởng phòng hoặc Admin được tạo OT cho danh sách nhân viên.",
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
        "Đã gửi yêu cầu OT",
        "Yêu cầu làm thêm giờ đã được ghi nhận.",
      );
      setForm((prev) => ({ ...prev, reason: "", projectCode: "" }));
      await fetchData();
    } catch (error) {
      triggerAlert("error", "Không thể gửi OT", getErrorMessage(error));
    } finally {
      setLoading(false);
    }
  };

  return (
    <FeaturePage
      title="Làm thêm giờ (OT)"
      description="Tạo yêu cầu OT và theo dõi trạng thái xử lý của các yêu cầu đã gửi."
      width="wide"
    >
      <FeatureCard title="Tạo yêu cầu OT">
        <form
          className="grid gap-4 md:grid-cols-2 xl:grid-cols-6"
          onSubmit={submitRequest}
        >
          {canCreateForOther && (
            <Field
              label={canCreateBulk ? "Chọn nhân viên OT" : "Nhân viên OT"}
            >
              <select
                multiple={canCreateBulk}
                value={canCreateBulk ? selectedEmployeeIds : form.employeeId}
                onChange={(e) =>
                  setForm({
                    ...form,
                    employeeId: canCreateBulk
                      ? Array.from(e.target.selectedOptions)
                          .map((option) => option.value)
                          .join(",")
                      : e.target.value,
                  })
                }
                className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
                size={canCreateBulk ? 5 : 1}
              >
                {!canCreateBulk && <option value="">Bản thân</option>}
                {employeeOptions.map((employee) => (
                  <option key={employee.id} value={employee.id}>
                    {employee.employeeCode} - {employee.fullName}
                    {employee.departmentName
                      ? ` (${employee.departmentName})`
                      : ""}
                  </option>
                ))}
              </select>
              {canCreateBulk && (
                <p className="mt-1 text-xs text-gray-500">
                  Không chọn nếu đăng ký cho bản thân; giữ Ctrl để chọn nhiều
                  nhân viên trong phạm vi được phép.
                </p>
              )}
            </Field>
          )}

          <Field label="Ngày OT">
            <input
              type="date"
              required
              value={form.workDate}
              onChange={(e) => setForm({ ...form, workDate: e.target.value })}
              className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
            />
          </Field>
          <Field label="Bắt đầu">
            <input
              type="time"
              required
              value={form.startTime}
              onChange={(e) => setForm({ ...form, startTime: e.target.value })}
              className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
            />
          </Field>
          <Field label="Kết thúc">
            <input
              type="time"
              required
              value={form.endTime}
              onChange={(e) => setForm({ ...form, endTime: e.target.value })}
              className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
            />
          </Field>
          <Field label="Dự án">
            <input
              value={form.projectCode}
              onChange={(e) =>
                setForm({ ...form, projectCode: e.target.value })
              }
              className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
            />
          </Field>

          <div className="md:col-span-2 xl:col-span-6">
            <Field label="Lý do OT">
              <textarea
                required
                value={form.reason}
                onChange={(e) => setForm({ ...form, reason: e.target.value })}
                rows={3}
                className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
              />
            </Field>
          </div>

          <div className="md:col-span-2 xl:col-span-6">
            <button
              type="submit"
              disabled={loading}
              className={primaryButtonClass}
            >
              Gửi yêu cầu OT
            </button>
          </div>
        </form>
      </FeatureCard>

      <OvertimeTable
        title="Yêu cầu OT của tôi"
        data={myRequests}
        emptyText="Bạn chưa có yêu cầu OT nào."
      />
    </FeaturePage>
  );
};

const Field = ({ label, children }: { label: string; children: ReactNode }) => (
  <label className="block">
    <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
      {label}
    </span>
    {children}
  </label>
);

const parseEmployeeIds = (value: string) =>
  value
    .split(/[\s,;]+/)
    .map((item) => Number(item.trim()))
    .filter((item) => Number.isInteger(item) && item > 0);

const getErrorMessage = (error: unknown) =>
  error instanceof Error ? error.message : "Đã có lỗi xảy ra.";
