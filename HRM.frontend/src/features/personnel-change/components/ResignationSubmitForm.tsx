import { useEffect, useState, type FormEvent } from "react";
import { Send } from "lucide-react";
import { Button, Card, Input } from "../../../components/ui";
import { usePersonnelChangeLookups } from "../hooks/usePersonnelChangeLookups";
import type { SubmitResignationRequest } from "../types/personnelChange";
import { EmployeePicker } from "./PersonnelChangePickers";

type Props = {
  saving?: boolean;
  onSubmit: (payload: SubmitResignationRequest) => Promise<boolean>;
};

export const ResignationSubmitForm = ({ saving, onSubmit }: Props) => {
  const lookups = usePersonnelChangeLookups();
  const [form, setForm] = useState({
    employeeId: "",
    expectedLastWorkingDate: "",
    reason: "",
    employeeNote: "",
  });

  useEffect(() => {
    if (form.employeeId || lookups.employees.length !== 1) return;
    setForm((prev) => ({ ...prev, employeeId: String(lookups.employees[0].id) }));
  }, [form.employeeId, lookups.employees]);

  const selectedEmployee = lookups.employees.find((employee) => String(employee.id) === form.employeeId);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const ok = await onSubmit({
      employeeId: Number(form.employeeId),
      expectedLastWorkingDate: form.expectedLastWorkingDate,
      reason: form.reason || null,
      employeeNote: form.employeeNote || null,
    });

    if (ok) {
      setForm((prev) => ({
        ...prev,
        reason: "",
        employeeNote: "",
      }));
    }
  };

  return (
    <Card title="Gửi đơn nghỉ việc" description="Nhân viên gửi yêu cầu nghỉ việc chủ động.">
      <form className="grid gap-4 md:grid-cols-2" onSubmit={submit}>
        {lookups.employees.length > 1 ? (
          <EmployeePicker
            label="Nhân sự"
            employees={lookups.employees}
            required
            value={form.employeeId}
            helperText="Chỉ hiển thị nhân sự thuộc phạm vi quyền của bạn."
            onChange={(value) => setForm((prev) => ({ ...prev, employeeId: value }))}
          />
        ) : (
          <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-[var(--hicas-bg-soft)] p-3">
            <p className="text-sm font-semibold text-[var(--hicas-text-main)]">
              {selectedEmployee
                ? `${selectedEmployee.employeeCode} - ${selectedEmployee.fullName}`
                : "Đang xác định nhân sự"}
            </p>
            <p className="mt-1 text-xs text-[var(--hicas-text-secondary)]">
              Đơn nghỉ việc sẽ được gửi theo hồ sơ cá nhân của bạn.
            </p>
          </div>
        )}
        <Input
          label="Ngày làm việc cuối"
          type="date"
          required
          value={form.expectedLastWorkingDate}
          onChange={(event) =>
            setForm((prev) => ({ ...prev, expectedLastWorkingDate: event.target.value }))
          }
        />
        <label className="block md:col-span-2">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">
            Lý do
          </span>
          <textarea
            className="hicas-input min-h-20 resize-y"
            value={form.reason}
            onChange={(event) => setForm((prev) => ({ ...prev, reason: event.target.value }))}
          />
        </label>
        <label className="block md:col-span-2">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">
            Ghi chú nhân viên
          </span>
          <textarea
            className="hicas-input min-h-20 resize-y"
            value={form.employeeNote}
            onChange={(event) => setForm((prev) => ({ ...prev, employeeNote: event.target.value }))}
          />
        </label>
        <div className="md:col-span-2">
          <Button
            type="submit"
            iconLeft={<Send size={16} />}
            isLoading={saving}
            disabled={!form.employeeId}
          >
            Gửi đơn nghỉ việc
          </Button>
        </div>
      </form>
    </Card>
  );
};
