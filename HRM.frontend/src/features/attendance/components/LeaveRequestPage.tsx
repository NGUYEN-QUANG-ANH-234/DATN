import { useCallback, useEffect, useState, type ReactNode } from "react";
import { Button, Card, DataTable, StatusBadge } from "../../../components/ui";
import type { DataTableColumn } from "../../../components/ui";
import { useCurrentUser } from "../../../core/auth/hooks/useCurrentUser";
import { useNotification } from "../../../core/context/NotificationContext";
import { FeaturePage } from "../../../core/components/FeatureShell";
import { formatDate } from "../../../utils";
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
  const selectedLeaveType = leaveTypes.find((type) => type.id.toString() === form.leaveTypeId);
  const isMaternityLeave = selectedLeaveType?.category === "Maternity";

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
      description="Tạo đơn nghỉ phép và theo dõi trạng thái xử lý."
      width="wide"
    >
      {!isAdmin && (
        <Card title="Tạo đơn nghỉ phép">
          <form className="grid gap-4 md:grid-cols-2 xl:grid-cols-5" onSubmit={submit}>
            <Field label="Loại phép">
              <select
                required
                value={form.leaveTypeId}
                onChange={(event) => setForm({ ...form, leaveTypeId: event.target.value })}
                className="hicas-input w-full"
              >
                <option value="">Chọn loại phép</option>
                {leaveTypes.map((type) => (
                  <option key={type.id} value={type.id}>
                    {type.typeName}{type.category === "Maternity" ? " - Thai sản" : ""}
                  </option>
                ))}
              </select>
            </Field>
            <Field label="Từ ngày">
              <input
                type="date"
                required
                value={form.startDate}
                onChange={(event) => setForm({ ...form, startDate: event.target.value })}
                className="hicas-input w-full"
              />
            </Field>
            <Field label="Đến ngày">
              <input
                type="date"
                required
                value={form.endDate}
                onChange={(event) => setForm({ ...form, endDate: event.target.value })}
                className="hicas-input w-full"
              />
            </Field>
            <div className="md:col-span-2 xl:col-span-5">
              <Field label="Lý do nghỉ">
                <textarea
                  required
                  value={form.reason}
                  onChange={(event) => setForm({ ...form, reason: event.target.value })}
                  rows={3}
                  className="hicas-input min-h-[104px] w-full py-3"
                />
              </Field>
            </div>
            {isMaternityLeave && (
              <div className="md:col-span-2 xl:col-span-5 rounded-2xl border border-[var(--hicas-orange)]/30 bg-[var(--hicas-orange-lighter)] px-4 py-3 text-sm text-[var(--hicas-text-main)]">
                Đơn nghỉ thai sản sau khi được duyệt sẽ tự động ghi nhận hồ sơ thai sản,
                cập nhật trạng thái nhân sự và đồng bộ bảng công/payroll theo loại nghỉ thai sản.
              </div>
            )}
            <div className="md:col-span-2 xl:col-span-5">
              <Button type="submit" isLoading={loading}>
                Gửi đơn nghỉ phép
              </Button>
            </div>
          </form>
        </Card>
      )}

      {!isAdmin && (
        <LeaveTable
          title="Đơn nghỉ phép của tôi"
          data={myRequests}
          loading={loading}
          emptyText="Bạn chưa có đơn nghỉ phép nào."
        />
      )}

      {isAdmin && (
        <Card title="Nghỉ phép">
          <p className="text-sm text-[var(--hicas-text-secondary)]">
            Admin xử lý phê duyệt và theo dõi trạng thái nghỉ phép tại mục Phê duyệt.
          </p>
        </Card>
      )}
    </FeaturePage>
  );
};

const Field = ({ label, children }: { label: string; children: ReactNode }) => (
  <label className="block">
    <span className="mb-2 block text-xs font-semibold uppercase tracking-[0.08em] text-[var(--hicas-text-secondary)]">
      {label}
    </span>
    {children}
  </label>
);

const LeaveTable = ({
  title,
  data,
  loading,
  emptyText,
}: {
  title: string;
  data: LeaveRequest[];
  loading: boolean;
  emptyText: string;
}) => {
  const columns: Array<DataTableColumn<LeaveRequest>> = [
    { key: "type", header: "Loại phép", render: (item) => item.leaveTypeName },
    {
      key: "category",
      header: "Nhóm nghỉ",
      render: (item) => getLeaveCategoryLabel(item.leaveCategory),
    },
    {
      key: "period",
      header: "Thời gian",
      render: (item) => `${formatDate(item.startDate)} - ${formatDate(item.endDate)}`,
    },
    { key: "days", header: "Số ngày", render: (item) => item.requestedDays },
    {
      key: "reason",
      header: "Lý do",
      render: (item) => (
        <span className="line-clamp-2 text-[var(--hicas-text-secondary)]">{item.reason}</span>
      ),
    },
    { key: "status", header: "Trạng thái", render: (item) => <StatusBadge status={item.status} /> },
  ];

  return (
    <Card title={title}>
      <DataTable
        columns={columns}
        data={data}
        loading={loading}
        rowKey={(row) => row.id}
        emptyTitle={emptyText}
        className="border-0 shadow-none"
      />
    </Card>
  );
};

const getErrorMessage = (error: unknown) =>
  error instanceof Error ? error.message : "Đã có lỗi xảy ra.";

const getLeaveCategoryLabel = (category: string) => {
  const map: Record<string, string> = {
    AnnualPaid: "Phép năm",
    Unpaid: "Không lương",
    Sick: "Ốm đau",
    Maternity: "Thai sản",
    SpecialPaid: "Nghỉ hưởng lương khác",
  };

  return map[category] ?? "Khác";
};
