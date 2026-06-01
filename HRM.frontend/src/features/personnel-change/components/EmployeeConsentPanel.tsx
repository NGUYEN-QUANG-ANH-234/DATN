import { useState, type FormEvent } from "react";
import { CheckCheck } from "lucide-react";
import { Button, Card, Select } from "../../../components/ui";
import type { EmployeeConsentRequest, PersonnelChangeDetail } from "../types/personnelChange";

type Props = {
  request?: PersonnelChangeDetail | null;
  saving?: boolean;
  onSubmit: (id: number, payload: EmployeeConsentRequest) => Promise<boolean>;
};

export const EmployeeConsentPanel = ({ request, saving, onSubmit }: Props) => {
  const [isAccepted, setIsAccepted] = useState("true");
  const [note, setNote] = useState("");

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!request) return;
    await onSubmit(request.id, {
      isAccepted: isAccepted === "true",
      note: note || null,
    });
  };

  return (
    <Card title="Employee consent" description="Nhan vien xac nhan dong y hoac tu choi dieu chuyen.">
      <form className="space-y-4" onSubmit={submit}>
        <Select
          label="Phan hoi"
          value={isAccepted}
          options={[
            { value: "true", label: "Dong y" },
            { value: "false", label: "Tu choi" },
          ]}
          onChange={(event) => setIsAccepted(event.target.value)}
        />
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Ghi chu</span>
          <textarea
            className="hicas-input min-h-24 resize-y"
            value={note}
            onChange={(event) => setNote(event.target.value)}
          />
        </label>
        <Button type="submit" iconLeft={<CheckCheck size={16} />} isLoading={saving} disabled={!request}>
          Gui phan hoi
        </Button>
      </form>
    </Card>
  );
};
