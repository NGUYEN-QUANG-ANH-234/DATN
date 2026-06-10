import { useState, type FormEvent } from "react";
import { CheckCheck } from "lucide-react";
import { Button, Card, Select } from "../../../components/ui";
import type { AppointmentConsentRequest, PersonnelChangeDetail } from "../types/personnelChange";

type Props = {
  request?: PersonnelChangeDetail | null;
  saving?: boolean;
  onSubmit: (id: number, payload: AppointmentConsentRequest) => Promise<boolean>;
};

export const AppointmentConsentPanel = ({ request, saving, onSubmit }: Props) => {
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
    <Card title="Xác nhận bổ nhiệm" description="Nhân sự xác nhận đồng ý hoặc từ chối đề xuất bổ nhiệm.">
      <form className="space-y-4" onSubmit={submit}>
        <Select
          label="Phản hồi"
          value={isAccepted}
          options={[
            { value: "true", label: "Đồng ý" },
            { value: "false", label: "Từ chối" },
          ]}
          onChange={(event) => setIsAccepted(event.target.value)}
        />
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Ghi chú</span>
          <textarea
            className="hicas-input min-h-24 resize-y"
            value={note}
            onChange={(event) => setNote(event.target.value)}
          />
        </label>
        <Button type="submit" iconLeft={<CheckCheck size={16} />} isLoading={saving} disabled={!request}>
          Gửi phản hồi bổ nhiệm
        </Button>
      </form>
    </Card>
  );
};
