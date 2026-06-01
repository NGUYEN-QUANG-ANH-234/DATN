import { useState, type FormEvent } from "react";
import { MessageSquareText } from "lucide-react";
import { Button, Card, Input } from "../../../components/ui";
import type { DismissalEmployeeExplanationRequest, PersonnelChangeDetail } from "../types/personnelChange";

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
    <Card title="Employee explanation" description="Nhan vien gui giai trinh truoc khi Director phe duyet.">
      <form className="space-y-4" onSubmit={submit}>
        <Input
          label="Evidence bo sung"
          value={form.evidenceFilePath}
          onChange={(event) => setForm((prev) => ({ ...prev, evidenceFilePath: event.target.value }))}
        />
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Giai trinh</span>
          <textarea
            className="hicas-input min-h-28 resize-y"
            required
            value={form.explanation}
            onChange={(event) => setForm((prev) => ({ ...prev, explanation: event.target.value }))}
          />
        </label>
        <Button type="submit" iconLeft={<MessageSquareText size={16} />} isLoading={saving} disabled={!request}>
          Gui giai trinh
        </Button>
      </form>
    </Card>
  );
};
