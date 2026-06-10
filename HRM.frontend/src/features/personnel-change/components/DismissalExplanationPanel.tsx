import { useState, type FormEvent } from "react";
import { MessageSquareText } from "lucide-react";
import { Button, Card } from "../../../components/ui";
import type { DismissalEmployeeExplanationRequest, PersonnelChangeDetail } from "../types/personnelChange";
import { EvidenceFileUpload } from "./PersonnelChangePickers";

type Props = {
  request?: PersonnelChangeDetail | null;
  saving?: boolean;
  onSubmit: (id: number, payload: DismissalEmployeeExplanationRequest) => Promise<boolean>;
};

export const DismissalExplanationPanel = ({ request, saving, onSubmit }: Props) => {
  const [form, setForm] = useState({
    explanation: "",
    evidenceFilePath: "",
  });

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!request) return;
    await onSubmit(request.id, {
      explanation: form.explanation,
      evidenceFilePath: form.evidenceFilePath || null,
    });
  };

  return (
    <Card
      title="Giải trình nhân viên"
      description="Nhân viên gửi giải trình trước khi hồ sơ được phê duyệt."
    >
      <form className="space-y-4" onSubmit={submit}>
        <EvidenceFileUpload
          label="Bằng chứng bổ sung"
          value={form.evidenceFilePath}
          onUploaded={(filePath) => setForm((prev) => ({ ...prev, evidenceFilePath: filePath }))}
        />
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">
            Giải trình
          </span>
          <textarea
            className="hicas-input min-h-28 resize-y"
            required
            value={form.explanation}
            onChange={(event) => setForm((prev) => ({ ...prev, explanation: event.target.value }))}
          />
        </label>
        <Button type="submit" iconLeft={<MessageSquareText size={16} />} isLoading={saving} disabled={!request}>
          Gửi giải trình
        </Button>
      </form>
    </Card>
  );
};
