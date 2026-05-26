import { useCallback, useEffect, useState, type ReactNode } from "react";
import {
  FeatureCard,
  FeaturePage,
  primaryButtonClass,
} from "../../../core/components/FeatureShell";
import { useCurrentUser } from "../../../core/auth/hooks/useCurrentUser";
import { useNotification } from "../../../core/context/NotificationContext";
import {
  leaveRequestApi,
  type LeaveRequest,
  type LeaveTypeOption,
} from "../api/leaveRequestApi";

const today = new Date().toISOString().slice(0, 10);

export const LeaveRequestPage = () => {
  const { user } = useCurrentUser();
  const role = user?.role || "";
  const isAdmin = role === "Admin";
  const { triggerAlert } = useNotification();

  const [loading, setLoading] = useState(false);
  const [leaveTypes, setLeaveTypes] = useState<LeaveTypeOption[]>([]);
  const [myRequests, setMyRequests] = useState<LeaveRequest[]>([]);
  const [form, setForm] = useState({
    leaveTypeId: "",
    startDate: today,
    endDate: today,
    reason: "",
  });

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const typeRes = await leaveRequestApi.getLeaveTypes();
      setLeaveTypes(typeRes.data || []);

      if (!isAdmin) {
        try {
          const myRes = await leaveRequestApi.getMy();
          setMyRequests(myRes.data || []);
        } catch {
          setMyRequests([]);
        }
      }
    } finally {
      setLoading(false);
    }
  }, [isAdmin]);

  useEffect(() => {
    void fetchData();
  }, [fetchData]);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!form.leaveTypeId) {
      triggerAlert("warning", "Thiếu loại phép", "Vui lòng chọn loại nghỉ phép.");
      return;
    }

    setLoading(true);
    try {
      const res = await leaveRequestApi.create({
        leaveTypeId: Number(form.leaveTypeId),
        startDate: form.startDate,
        endDate: form.endDate,
        reason: form.reason,
      });
      triggerAlert(
        "success",
        "Đã gửi đơn nghỉ phép",
        res.message || "Đơn đã được gửi tới luồng phê duyệt.",
      );
      setForm((prev) => ({ ...prev, reason: "" }));
      await fetchData();
    } catch (error) {
      triggerAlert("error", "Không thể gửi đơn nghỉ phép", getErrorMessage(error));
    } finally {
      setLoading(false);
    }
  };

  return (
    <FeaturePage
      title="Nghỉ phép"
      description="Tạo đơn nghỉ phép và theo dõi trạng thái xử lý của cá nhân. Các bước phê duyệt được xử lý tập trung tại mục Phê duyệt."
      width="wide"
    >
      {!isAdmin && (
        <FeatureCard title="Tạo đơn nghỉ phép">
          <form
            className="grid gap-4 md:grid-cols-2 xl:grid-cols-5"
            onSubmit={submit}
          >
            <Field label="Loại phép">
              <select
                required
                value={form.leaveTypeId}
                onChange={(event) =>
                  setForm({ ...form, leaveTypeId: event.target.value })
                }
                className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
              >
                <option value="">Chọn loại phép</option>
                {leaveTypes.map((type) => (
                  <option key={type.id} value={type.id}>
                    {type.typeName}
                  </option>
                ))}
              </select>
            </Field>
            <Field label="Từ ngày">
              <input
                type="date"
                required
                value={form.startDate}
                onChange={(event) =>
                  setForm({ ...form, startDate: event.target.value })
                }
                className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
              />
            </Field>
            <Field label="Đến ngày">
              <input
                type="date"
                required
                value={form.endDate}
                onChange={(event) =>
                  setForm({ ...form, endDate: event.target.value })
                }
                className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
              />
            </Field>
            <div className="md:col-span-2 xl:col-span-5">
              <Field label="Lý do nghỉ">
                <textarea
                  required
                  value={form.reason}
                  onChange={(event) =>
                    setForm({ ...form, reason: event.target.value })
                  }
                  rows={3}
                  className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
                />
              </Field>
            </div>
            <div className="md:col-span-2 xl:col-span-5">
              <button
                type="submit"
                disabled={loading}
                className={primaryButtonClass}
              >
                Gửi đơn nghỉ phép
              </button>
            </div>
          </form>
        </FeatureCard>
      )}

      {!isAdmin && (
        <LeaveTable
          title="Đơn nghỉ phép của tôi"
          data={myRequests}
          emptyText="Bạn chưa có đơn nghỉ phép nào."
        />
      )}

      {isAdmin && (
        <FeatureCard title="Nghỉ phép">
          <p className="text-sm text-gray-600">
            Admin xử lý phê duyệt và theo dõi trạng thái nghỉ phép tại mục
            Phê duyệt.
          </p>
        </FeatureCard>
      )}
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

const LeaveTable = ({
  title,
  data,
  emptyText,
}: {
  title: string;
  data: LeaveRequest[];
  emptyText: string;
}) => (
  <FeatureCard title={title}>
    <div className="overflow-x-auto">
      <table className="w-full min-w-[840px] text-left text-sm">
        <thead className="border-b bg-gray-50 text-xs uppercase text-gray-500">
          <tr>
            <th className="px-3 py-2">Loại phép</th>
            <th className="px-3 py-2">Thời gian</th>
            <th className="px-3 py-2">Số ngày</th>
            <th className="px-3 py-2">Lý do</th>
            <th className="px-3 py-2">Trạng thái</th>
          </tr>
        </thead>
        <tbody>
          {data.map((item) => (
            <tr key={item.id} className="border-b">
              <td className="px-3 py-3">{item.leaveTypeName}</td>
              <td className="px-3 py-3">
                {formatDate(item.startDate)} - {formatDate(item.endDate)}
              </td>
              <td className="px-3 py-3">{item.requestedDays}</td>
              <td className="max-w-[320px] px-3 py-3 text-gray-600">
                {item.reason}
              </td>
              <td className="px-3 py-3">{formatStatus(item.status)}</td>
            </tr>
          ))}
          {data.length === 0 && (
            <tr>
              <td colSpan={5} className="px-3 py-6 text-center text-gray-500">
                {emptyText}
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  </FeatureCard>
);

const formatDate = (value: string) => {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "Không xác định";
  return date.toLocaleDateString("vi-VN");
};

const formatStatus = (status: string) => {
  const map: Record<string, string> = {
    PendingDept: "Chờ Trưởng phòng",
    PendingDirector: "Chờ Giám đốc",
    RejectedByDept: "Trưởng phòng từ chối",
    RejectedByDirector: "Giám đốc từ chối",
    Approved: "Đã duyệt",
    AutoDeptApproved: "Tự duyệt cấp phòng",
    AutoFinalApproved: "Tự duyệt cuối",
  };

  return map[status] || status;
};

const getErrorMessage = (error: unknown) =>
  error instanceof Error ? error.message : "Đã có lỗi xảy ra.";
