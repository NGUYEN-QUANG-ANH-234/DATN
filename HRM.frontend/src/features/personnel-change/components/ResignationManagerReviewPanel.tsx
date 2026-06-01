import { useState, type FormEvent } from "react";
import { CheckCircle2, XCircle } from "lucide-react";
import { Button, Card, Select } from "../../../components/ui";
import type { ManagerReviewResignationRequest, PersonnelChangeDetail } from "../types/personnelChange";

type Props = {
  request?: PersonnelChangeDetail | null;
  saving?: boolean;
  onSubmit: (id: number, payload: ManagerReviewResignationRequest) => Promise<boolean>;
};

export const ResignationManagerReviewPanel = ({ request, saving, onSubmit }: Props) => {
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
    <Card title="Manager review" description="Quan ly truc tiep xac nhan yeu cau nghi viec.">
      <form className="space-y-4" onSubmit={submit}>
        <Select
          label="Quyet dinh"
          value={isApproved}
          disabled={!request || saving}
          options={[
            { value: "true", label: "Approve" },
            { value: "false", label: "Reject" },
          ]}
          onChange={(event) => setIsApproved(event.target.value)}
        />
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Manager note</span>
          <textarea
            className="hicas-input min-h-24 resize-y"
            disabled={!request || saving}
            value={note}
            onChange={(event) => setNote(event.target.value)}
          />
        </label>
        <Button
          type="submit"
          variant={isApproved === "true" ? "primary" : "danger"}
          iconLeft={isApproved === "true" ? <CheckCircle2 size={16} /> : <XCircle size={16} />}
          isLoading={saving}
          disabled={!request}
        >
          Gui review
        </Button>
      </form>
    </Card>
  );
};
