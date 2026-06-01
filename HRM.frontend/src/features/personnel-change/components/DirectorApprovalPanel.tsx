import { useState, type FormEvent } from "react";
import { ShieldCheck } from "lucide-react";
import { Button, Card, Select } from "../../../components/ui";
import type { DirectorApproveTransferRequest, PersonnelChangeDetail } from "../types/personnelChange";

type Props = {
  request?: PersonnelChangeDetail | null;
  saving?: boolean;
  onSubmit: (id: number, payload: DirectorApproveTransferRequest) => Promise<boolean>;
};

export const DirectorApprovalPanel = ({ request, saving, onSubmit }: Props) => {
  const [isApproved, setIsApproved] = useState("true");
  const [note, setNote] = useState("");

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!request) return;
    await onSubmit(request.id, {
      isApproved: isApproved === "true",
      note: note || null,
    });
  };

  return (
    <Card title="Director approval" description="Phe duyet cuoi cho ho so thuyen chuyen noi bo.">
      <form className="space-y-4" onSubmit={submit}>
        <Select
          label="Quyet dinh"
          value={isApproved}
          options={[
            { value: "true", label: "Phe duyet" },
            { value: "false", label: "Tu choi" },
          ]}
          onChange={(event) => setIsApproved(event.target.value)}
        />
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Ghi chu</span>
          <textarea
            className="hicas-input min-h-24 resize-y"
            value={note}
            onChange={(event) => setNote(event.target.value)}
          />
        </label>
        <Button type="submit" iconLeft={<ShieldCheck size={16} />} isLoading={saving} disabled={!request}>
          Xu ly phe duyet
        </Button>
      </form>
    </Card>
  );
};
