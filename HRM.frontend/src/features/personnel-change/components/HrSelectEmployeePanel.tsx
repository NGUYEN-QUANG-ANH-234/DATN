import { useState, type FormEvent } from "react";
import { UserCheck } from "lucide-react";
import { Button, Card } from "../../../components/ui";
import { usePersonnelChangeLookups } from "../hooks/usePersonnelChangeLookups";
import type { HrSelectEmployeeRequest, PersonnelChangeDetail } from "../types/personnelChange";
import {
  DepartmentPicker,
  EmployeePicker,
  JobLevelPicker,
  ManagerPicker,
  PositionPicker,
} from "./PersonnelChangePickers";

type Props = {
  request?: PersonnelChangeDetail | null;
  saving?: boolean;
  onSubmit: (id: number, payload: HrSelectEmployeeRequest) => Promise<boolean>;
};

export const HrSelectEmployeePanel = ({ request, saving, onSubmit }: Props) => {
  const lookups = usePersonnelChangeLookups();
  const [form, setForm] = useState({
    employeeId: "",
    newDepartmentId: "",
    newPositionId: "",
    newManagerId: "",
    newJobLevelId: "",
    requiresContractAddendum: false,
    note: "",
  });

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!request) return;

    await onSubmit(request.id, {
      employeeId: Number(form.employeeId),
      newDepartmentId: toNumberOrNull(form.newDepartmentId) ?? request.newDepartmentId ?? null,
      newPositionId: toNumberOrNull(form.newPositionId) ?? request.newPositionId ?? null,
      newManagerId: toNumberOrNull(form.newManagerId) ?? request.newManagerId ?? null,
      newJobLevelId: toNumberOrNull(form.newJobLevelId),
      requiresContractAddendum: form.requiresContractAddendum,
      note: form.note || null,
    });
  };

  return (
    <Card
      title="HR chọn nhân sự"
      description="Chọn nhân sự và xác nhận thông tin điều chuyển."
    >
      <form className="grid gap-4 md:grid-cols-2" onSubmit={submit}>
        <EmployeePicker
          label="Nhân sự được chọn"
          employees={lookups.employees}
          required
          value={form.employeeId}
          helperText={lookups.loading ? "Đang tải danh sách nhân sự..." : undefined}
          onChange={(value) => setForm((prev) => ({ ...prev, employeeId: value }))}
        />
        <DepartmentPicker
          label="Phòng ban mới"
          departments={lookups.departments}
          value={form.newDepartmentId}
          placeholder={request?.newDepartmentName || "Giữ theo nhu cầu ban đầu"}
          onChange={(value) => setForm((prev) => ({ ...prev, newDepartmentId: value }))}
        />
        <PositionPicker
          label="Chức danh mới"
          positions={lookups.positions}
          value={form.newPositionId}
          placeholder={request?.newPositionName || "Giữ theo nhu cầu ban đầu"}
          onChange={(value) => setForm((prev) => ({ ...prev, newPositionId: value }))}
        />
        <ManagerPicker
          label="Quản lý mới"
          managers={lookups.managers}
          value={form.newManagerId}
          placeholder={request?.newManagerName || "Chọn quản lý mới"}
          onChange={(value) => setForm((prev) => ({ ...prev, newManagerId: value }))}
        />
        <JobLevelPicker
          label="Cấp bậc mới"
          jobLevels={lookups.jobLevels}
          value={form.newJobLevelId}
          onChange={(value) => setForm((prev) => ({ ...prev, newJobLevelId: value }))}
        />
        <label className="flex items-center gap-3 pt-7 text-sm font-medium text-[var(--hicas-text-main)]">
          <input
            type="checkbox"
            checked={form.requiresContractAddendum}
            onChange={(event) =>
              setForm((prev) => ({
                ...prev,
                requiresContractAddendum: event.target.checked,
              }))
            }
          />
          Cần phụ lục hợp đồng
        </label>
        <label className="block md:col-span-2">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">
            Ghi chú HR
          </span>
          <textarea
            className="hicas-input min-h-20 resize-y"
            value={form.note}
            onChange={(event) => setForm((prev) => ({ ...prev, note: event.target.value }))}
          />
        </label>
        <div className="md:col-span-2">
          <Button
            type="submit"
            iconLeft={<UserCheck size={16} />}
            isLoading={saving}
            disabled={!request}
          >
            Xác nhận nhân sự
          </Button>
        </div>
      </form>
    </Card>
  );
};

const toNumberOrNull = (value: string) => {
  const numericValue = Number(value);
  return Number.isInteger(numericValue) && numericValue > 0 ? numericValue : null;
};
