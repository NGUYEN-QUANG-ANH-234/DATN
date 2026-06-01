import { useState, type FormEvent } from "react";
import { MessageSquareText } from "lucide-react";
import { Button, Card, Select } from "../../../components/ui";
import type { CurrentManagerOpinionRequest, PersonnelChangeDetail } from "../types/personnelChange";

type Props = {
  request?: PersonnelChangeDetail | null;
  saving?: boolean;
  onSubmit: (id: number, payload: CurrentManagerOpinionRequest) => Promise<boolean>;
};

export const CurrentManagerOpinionPanel = ({ request, saving, onSubmit }: Props) => {
  const [isApproved, setIsApproved] = useState("true");
  const [opinion, setOpinion] = useState("");

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!request) return;
    await onSubmit(request.id, {
      isApproved: isApproved === "true",
      opinion: opinion || null,
    });
  };

  return (
    <Card title="Current manager opinion" description="Ghi nhan y kien quan ly hien tai truoc khi xin consent.">
      <form className="space-y-4" onSubmit={submit}>
        <Select
          label="Y kien"
          value={isApproved}
          options={[
            { value: "true", label: "Dong y" },
            { value: "false", label: "Khong dong y" },
          ]}
          onChange={(event) => setIsApproved(event.target.value)}
        />
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Noi dung</span>
          <textarea
            className="hicas-input min-h-24 resize-y"
            value={opinion}
            onChange={(event) => setOpinion(event.target.value)}
          />
        </label>
        <Button type="submit" iconLeft={<MessageSquareText size={16} />} isLoading={saving} disabled={!request}>
          Gui y kien
        </Button>
      </form>
    </Card>
  );
};
