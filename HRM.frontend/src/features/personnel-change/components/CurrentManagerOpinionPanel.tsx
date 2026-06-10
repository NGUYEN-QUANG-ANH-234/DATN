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
    <Card title="Ý kiến quản lý hiện tại" description="Ghi nhận ý kiến của quản lý trước khi xin xác nhận từ nhân viên.">
      <form className="space-y-4" onSubmit={submit}>
        <Select
          label="Ý kiến"
          value={isApproved}
          options={[
            { value: "true", label: "Đồng ý" },
            { value: "false", label: "Không đồng ý" },
          ]}
          onChange={(event) => setIsApproved(event.target.value)}
        />
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Nội dung</span>
          <textarea
            className="hicas-input min-h-24 resize-y"
            value={opinion}
            onChange={(event) => setOpinion(event.target.value)}
          />
        </label>
        <Button type="submit" iconLeft={<MessageSquareText size={16} />} isLoading={saving} disabled={!request}>
          Gửi ý kiến
        </Button>
      </form>
    </Card>
  );
};
