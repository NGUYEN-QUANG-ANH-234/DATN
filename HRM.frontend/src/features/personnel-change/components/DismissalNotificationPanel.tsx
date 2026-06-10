import { useState, type FormEvent } from "react";
import { BellRing } from "lucide-react";
import { Button, Card, Input } from "../../../components/ui";
import type { NotifyEmployeeDismissalRequest, PersonnelChangeDetail } from "../types/personnelChange";
import { EvidenceFileUpload } from "./PersonnelChangePickers";

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
    <Card
      title="Thông báo nhân viên"
      description="Ghi nhận thời điểm thông báo và hạn gửi giải trình."
    >
      <form className="space-y-4" onSubmit={submit}>
        <Input
          label="Đã thông báo lúc"
          type="datetime-local"
          value={form.employeeNotifiedAt}
          onChange={(event) => setForm((prev) => ({ ...prev, employeeNotifiedAt: event.target.value }))}
        />
        <Input
          label="Hạn giải trình"
          type="datetime-local"
          value={form.responseDeadlineAt}
          onChange={(event) => setForm((prev) => ({ ...prev, responseDeadlineAt: event.target.value }))}
        />
        <EvidenceFileUpload
          label="Tệp bằng chứng"
          value={form.evidenceFilePath}
          onUploaded={(filePath) => setForm((prev) => ({ ...prev, evidenceFilePath: filePath }))}
        />
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">
            Ghi chú HR
          </span>
          <textarea
            className="hicas-input min-h-20 resize-y"
            value={form.hrNote}
            onChange={(event) => setForm((prev) => ({ ...prev, hrNote: event.target.value }))}
          />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">
            Ghi chú thông báo
          </span>
          <textarea
            className="hicas-input min-h-20 resize-y"
            value={form.note}
            onChange={(event) => setForm((prev) => ({ ...prev, note: event.target.value }))}
          />
        </label>
        <Button type="submit" iconLeft={<BellRing size={16} />} isLoading={saving} disabled={!request}>
          Ghi nhận thông báo
        </Button>
      </form>
    </Card>
  );
};
