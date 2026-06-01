import { useState, type FormEvent } from "react";
import { Send } from "lucide-react";
import { Button, Card, Input, Select } from "../../../components/ui";
import type { CreateDismissalRequest } from "../types/personnelChange";

type Props = {
  saving?: boolean;
  onSubmit: (payload: CreateDismissalRequest) => Promise<boolean>;
};

export const DismissalCreateForm = ({ saving, onSubmit }: Props) => {
  const [form, setForm] = useState({
    employeeId: "",
    sourcePenaltyRecordId: "",
    reason: "",
    evidenceFilePath: "",
    hrNote: "",
    managerNote: "",
    responseDeadlineAt: "",
    effectiveDate: "",
    relatedContractId: "",
    lockAccountOnExecution: "true",
    requiresFinalSettlement: "true",
  });

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    await onSubmit({
      employeeId: Number(form.employeeId),
      sourcePenaltyRecordId: Number(form.sourcePenaltyRecordId),
      reason: form.reason || null,
      evidenceFilePath: form.evidenceFilePath || null,
      hrNote: form.hrNote || null,
      managerNote: form.managerNote || null,
      responseDeadlineAt: form.responseDeadlineAt || null,
      effectiveDate: form.effectiveDate || null,
      relatedContractId: toNumberOrNull(form.relatedContractId),
      lockAccountOnExecution: form.lockAccountOnExecution === "true",
      requiresFinalSettlement: form.requiresFinalSettlement === "true",
    });
  };

  return (
    <Card title="Dismissal case" description="Tao ho so sa thai/ky luat tu penalty record da xac dinh.">
      <form className="grid gap-4 md:grid-cols-2" onSubmit={submit}>
        <Input
          label="Nhan su"
          type="number"
          min={1}
          required
          value={form.employeeId}
          onChange={(event) => setForm((prev) => ({ ...prev, employeeId: event.target.value }))}
        />
        <Input
          label="Penalty record"
          type="number"
          min={1}
          required
          value={form.sourcePenaltyRecordId}
          onChange={(event) => setForm((prev) => ({ ...prev, sourcePenaltyRecordId: event.target.value }))}
        />
        <Input
          label="Evidence file"
          value={form.evidenceFilePath}
          onChange={(event) => setForm((prev) => ({ ...prev, evidenceFilePath: event.target.value }))}
        />
        <Input
          label="Hop dong lien quan"
          type="number"
          min={1}
          value={form.relatedContractId}
          onChange={(event) => setForm((prev) => ({ ...prev, relatedContractId: event.target.value }))}
        />
        <Input
          label="Deadline giai trinh"
          type="datetime-local"
          value={form.responseDeadlineAt}
          onChange={(event) => setForm((prev) => ({ ...prev, responseDeadlineAt: event.target.value }))}
        />
        <Input
          label="Ngay hieu luc"
          type="date"
          value={form.effectiveDate}
          onChange={(event) => setForm((prev) => ({ ...prev, effectiveDate: event.target.value }))}
        />
        <Select
          label="Khoa tai khoan khi execute"
          value={form.lockAccountOnExecution}
          options={[
            { value: "true", label: "Co" },
            { value: "false", label: "Khong" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, lockAccountOnExecution: event.target.value }))}
        />
        <Select
          label="Tao final settlement"
          value={form.requiresFinalSettlement}
          options={[
            { value: "true", label: "Co" },
            { value: "false", label: "Khong" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, requiresFinalSettlement: event.target.value }))}
        />
        <label className="block md:col-span-2">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Ly do</span>
          <textarea
            className="hicas-input min-h-20 resize-y"
            value={form.reason}
            onChange={(event) => setForm((prev) => ({ ...prev, reason: event.target.value }))}
          />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">HR note</span>
          <textarea
            className="hicas-input min-h-20 resize-y"
            value={form.hrNote}
            onChange={(event) => setForm((prev) => ({ ...prev, hrNote: event.target.value }))}
          />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Manager note</span>
          <textarea
            className="hicas-input min-h-20 resize-y"
            value={form.managerNote}
            onChange={(event) => setForm((prev) => ({ ...prev, managerNote: event.target.value }))}
          />
        </label>
        <div className="md:col-span-2">
          <Button type="submit" iconLeft={<Send size={16} />} isLoading={saving}>
            Tao ho so
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
