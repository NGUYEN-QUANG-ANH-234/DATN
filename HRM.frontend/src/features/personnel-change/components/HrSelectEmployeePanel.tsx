import { useState, type FormEvent } from "react";
import { UserCheck } from "lucide-react";
import { Button, Card, Input } from "../../../components/ui";
import type { HrSelectEmployeeRequest, PersonnelChangeDetail } from "../types/personnelChange";

type Props = {
  request?: PersonnelChangeDetail | null;
  saving?: boolean;
  onSubmit: (id: number, payload: HrSelectEmployeeRequest) => Promise<boolean>;
};

export const HrSelectEmployeePanel = ({ request, saving, onSubmit }: Props) => {
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
    <Card title="HR select employee" description="Chon nhan su va xac nhan thong tin dich den.">
      <form className="grid gap-4 md:grid-cols-2" onSubmit={submit}>
        <Input
          label="Ma nhan su"
          name="employeeId"
          type="number"
          min={1}
          required
          value={form.employeeId}
          onChange={(event) => setForm((prev) => ({ ...prev, employeeId: event.target.value }))}
        />
        <Input
          label="Phong ban moi"
          name="newDepartmentId"
          type="number"
          min={1}
          placeholder={request?.newDepartmentId ? String(request.newDepartmentId) : undefined}
          value={form.newDepartmentId}
          onChange={(event) => setForm((prev) => ({ ...prev, newDepartmentId: event.target.value }))}
        />
        <Input
          label="Chuc danh moi"
          name="newPositionId"
          type="number"
          min={1}
          placeholder={request?.newPositionId ? String(request.newPositionId) : undefined}
          value={form.newPositionId}
          onChange={(event) => setForm((prev) => ({ ...prev, newPositionId: event.target.value }))}
        />
        <Input
          label="Quan ly moi"
          name="newManagerId"
          type="number"
          min={1}
          placeholder={request?.newManagerId ? String(request.newManagerId) : undefined}
          value={form.newManagerId}
          onChange={(event) => setForm((prev) => ({ ...prev, newManagerId: event.target.value }))}
        />
        <Input
          label="Job level moi"
          name="newJobLevelId"
          type="number"
          min={1}
          value={form.newJobLevelId}
          onChange={(event) => setForm((prev) => ({ ...prev, newJobLevelId: event.target.value }))}
        />
        <label className="flex items-center gap-3 pt-7 text-sm font-medium text-[var(--hicas-text-main)]">
          <input
            type="checkbox"
            checked={form.requiresContractAddendum}
            onChange={(event) => setForm((prev) => ({ ...prev, requiresContractAddendum: event.target.checked }))}
          />
          Can phu luc hop dong
        </label>
        <label className="block md:col-span-2">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Ghi chu HR</span>
          <textarea
            className="hicas-input min-h-20 resize-y"
            value={form.note}
            onChange={(event) => setForm((prev) => ({ ...prev, note: event.target.value }))}
          />
        </label>
        <div className="md:col-span-2">
          <Button type="submit" iconLeft={<UserCheck size={16} />} isLoading={saving} disabled={!request}>
            Xac nhan nhan su
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
