import { useState, type FormEvent } from "react";
import { BellRing } from "lucide-react";
import { Button, Card, Input } from "../../../components/ui";
import type { NotifyEmployeeDismissalRequest, PersonnelChangeDetail } from "../types/personnelChange";

type Props = {
  request?: PersonnelChangeDetail | null;
  saving?: boolean;
  onSubmit: (id: number, payload: NotifyEmployeeDismissalRequest) => Promise<boolean>;
};

export const DismissalNotificationPanel = ({ request, saving, onSubmit }: Props) => {
  const [form, setForm] = useState({
    employeeNotifiedAt: "",
    responseDeadlineAt: "",
    evidenceFilePath: "",
    hrNote: "",
    note: "",
  });

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!request) return;
    await onSubmit(request.id, {
      employeeNotifiedAt: form.employeeNotifiedAt || null,
      responseDeadlineAt: form.responseDeadlineAt || null,
      evidenceFilePath: form.evidenceFilePath || null,
      hrNote: form.hrNote || null,
      note: form.note || null,
    });
  };

  return (
    <Card title="Notify employee" description="Ghi nhan thoi diem thong bao va deadline giai trinh.">
      <form className="space-y-4" onSubmit={submit}>
        <Input
          label="Da thong bao luc"
          type="datetime-local"
          value={form.employeeNotifiedAt}
          onChange={(event) => setForm((prev) => ({ ...prev, employeeNotifiedAt: event.target.value }))}
        />
        <Input
          label="Deadline giai trinh"
          type="datetime-local"
          value={form.responseDeadlineAt}
          onChange={(event) => setForm((prev) => ({ ...prev, responseDeadlineAt: event.target.value }))}
        />
        <Input
          label="Evidence file"
          value={form.evidenceFilePath}
          onChange={(event) => setForm((prev) => ({ ...prev, evidenceFilePath: event.target.value }))}
        />
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">HR note</span>
          <textarea
            className="hicas-input min-h-20 resize-y"
            value={form.hrNote}
            onChange={(event) => setForm((prev) => ({ ...prev, hrNote: event.target.value }))}
          />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Ghi chu thong bao</span>
          <textarea
            className="hicas-input min-h-20 resize-y"
            value={form.note}
            onChange={(event) => setForm((prev) => ({ ...prev, note: event.target.value }))}
          />
        </label>
        <Button type="submit" iconLeft={<BellRing size={16} />} isLoading={saving} disabled={!request}>
          Ghi nhan thong bao
        </Button>
      </form>
    </Card>
  );
};
