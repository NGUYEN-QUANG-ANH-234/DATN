import { useState, type FormEvent } from "react";
import { Send } from "lucide-react";
import { Button, Card, Input } from "../../../components/ui";
import type { SubmitResignationRequest } from "../types/personnelChange";

type Props = {
  saving?: boolean;
  onSubmit: (payload: SubmitResignationRequest) => Promise<boolean>;
};

export const ResignationSubmitForm = ({ saving, onSubmit }: Props) => {
  const [form, setForm] = useState({
    employeeId: "",
    expectedLastWorkingDate: "",
    reason: "",
    employeeNote: "",
  });

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
    <Card title="Submit resignation" description="Nhan vien gui yeu cau nghi viec chu dong.">
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
          label="Ngay lam viec cuoi"
          type="date"
          required
          value={form.expectedLastWorkingDate}
          onChange={(event) =>
            setForm((prev) => ({ ...prev, expectedLastWorkingDate: event.target.value }))
          }
        />
        <label className="block md:col-span-2">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Ly do</span>
          <textarea
            className="hicas-input min-h-20 resize-y"
            value={form.reason}
            onChange={(event) => setForm((prev) => ({ ...prev, reason: event.target.value }))}
          />
        </label>
        <label className="block md:col-span-2">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Employee note</span>
          <textarea
            className="hicas-input min-h-20 resize-y"
            value={form.employeeNote}
            onChange={(event) => setForm((prev) => ({ ...prev, employeeNote: event.target.value }))}
          />
        </label>
        <div className="md:col-span-2">
          <Button type="submit" iconLeft={<Send size={16} />} isLoading={saving}>
            Gui don nghi viec
          </Button>
        </div>
      </form>
    </Card>
  );
};
