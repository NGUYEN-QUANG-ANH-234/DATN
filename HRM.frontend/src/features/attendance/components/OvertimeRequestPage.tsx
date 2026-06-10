import type { ReactNode } from "react";
import { Button, Card } from "../../../components/ui";
import { FeaturePage } from "../../../core/components/FeatureShell";
import { useOvertimeRequest } from "../hooks/useOvertimeRequest";
import { OvertimeTable } from "./OvertimeTable";

export const OvertimeRequestPage = () => {
  const {
    form,
    setForm,
    loading,
    myRequests,
    employeeOptions,
    canCreateForOther,
    canCreateBulk,
    selectedEmployeeIds,
    submitRequest,
  } = useOvertimeRequest();

  return (
    <FeaturePage
      title="Làm thêm giờ"
      description="Tạo yêu cầu làm thêm và theo dõi trạng thái xử lý."
      width="wide"
    >
      <Card title="Tạo yêu cầu làm thêm">
        <form className="grid gap-4 md:grid-cols-2 xl:grid-cols-6" onSubmit={submitRequest}>
          {canCreateForOther && (
            <Field label={canCreateBulk ? "Chọn nhân viên" : "Nhân viên"}>
              <select
                multiple={canCreateBulk}
                value={canCreateBulk ? selectedEmployeeIds : form.employeeId}
                onChange={(event) =>
                  setForm({
                    ...form,
                    employeeId: canCreateBulk
                      ? Array.from(event.target.selectedOptions)
                          .map((option) => option.value)
                          .join(",")
                      : event.target.value,
                  })
                }
                className="hicas-input w-full"
                size={canCreateBulk ? 5 : 1}
              >
                {!canCreateBulk && <option value="">Bản thân</option>}
                {employeeOptions.map((employee) => (
                  <option key={employee.id} value={employee.id}>
                    {employee.employeeCode} - {employee.fullName}
                    {employee.departmentName ? ` (${employee.departmentName})` : ""}
                  </option>
                ))}
              </select>
              {canCreateBulk && (
                <p className="mt-1 text-xs text-[var(--hicas-text-secondary)]">
                  Không chọn nếu đăng ký cho bản thân; giữ Ctrl để chọn nhiều nhân viên trong phạm vi được phép.
                </p>
              )}
            </Field>
          )}

          <Field label="Ngày làm thêm">
            <input
              type="date"
              required
              value={form.workDate}
              onChange={(event) => setForm({ ...form, workDate: event.target.value })}
              className="hicas-input w-full"
            />
          </Field>
          <Field label="Bắt đầu">
            <input
              type="time"
              required
              value={form.startTime}
              onChange={(event) => setForm({ ...form, startTime: event.target.value })}
              className="hicas-input w-full"
            />
          </Field>
          <Field label="Kết thúc">
            <input
              type="time"
              required
              value={form.endTime}
              onChange={(event) => setForm({ ...form, endTime: event.target.value })}
              className="hicas-input w-full"
            />
          </Field>
          <Field label="Dự án">
            <input
              value={form.projectCode}
              onChange={(event) => setForm({ ...form, projectCode: event.target.value })}
              className="hicas-input w-full"
            />
          </Field>

          <div className="md:col-span-2 xl:col-span-6">
            <Field label="Lý do làm thêm">
              <textarea
                required
                value={form.reason}
                onChange={(event) => setForm({ ...form, reason: event.target.value })}
                rows={3}
                className="hicas-input min-h-[104px] w-full py-3"
              />
            </Field>
          </div>

          <div className="md:col-span-2 xl:col-span-6">
            <Button type="submit" isLoading={loading}>
              Gửi yêu cầu
            </Button>
          </div>
        </form>
      </Card>

      <OvertimeTable
        title="Yêu cầu làm thêm của tôi"
        data={myRequests}
        emptyText="Bạn chưa có yêu cầu làm thêm nào."
      />
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
