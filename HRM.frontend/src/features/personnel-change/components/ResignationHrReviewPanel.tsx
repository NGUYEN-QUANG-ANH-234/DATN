import { useState, type FormEvent } from "react";
import { CheckCircle2, XCircle } from "lucide-react";
import { Button, Card, Input, Select } from "../../../components/ui";
import type { HrReviewResignationRequest, PersonnelChangeDetail } from "../types/personnelChange";

type Props = {
  request?: PersonnelChangeDetail | null;
  saving?: boolean;
  onSubmit: (id: number, payload: HrReviewResignationRequest) => Promise<boolean>;
};

export const ResignationHrReviewPanel = ({ request, saving, onSubmit }: Props) => {
  const [form, setForm] = useState({
    isApproved: "true",
    relatedContractId: "",
    requiresFinalSettlement: "true",
    lockAccountAfterEffectiveDate: "true",
    note: "",
  });

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!request) return;
    await onSubmit(request.id, {
      isApproved: form.isApproved === "true",
      relatedContractId: toNumberOrNull(form.relatedContractId),
      requiresFinalSettlement: form.requiresFinalSettlement === "true",
      lockAccountAfterEffectiveDate: form.lockAccountAfterEffectiveDate === "true",
      note: form.note || null,
    });
  };

  return (
    <Card title="HR review" description="HR kiem tra hop dong, final settlement va account lock.">
      <form className="grid gap-4 md:grid-cols-2" onSubmit={submit}>
        <Select
          label="Quyet dinh"
          value={form.isApproved}
          disabled={!request || saving}
          options={[
            { value: "true", label: "Approve" },
            { value: "false", label: "Reject" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, isApproved: event.target.value }))}
        />
        <Input
          label="Hop dong lien quan"
          type="number"
          min={1}
          disabled={!request || saving}
          value={form.relatedContractId}
          onChange={(event) => setForm((prev) => ({ ...prev, relatedContractId: event.target.value }))}
        />
        <Select
          label="Tao final settlement"
          value={form.requiresFinalSettlement}
          disabled={!request || saving}
          options={[
            { value: "true", label: "Co" },
            { value: "false", label: "Khong" },
          ]}
          onChange={(event) =>
            setForm((prev) => ({ ...prev, requiresFinalSettlement: event.target.value }))
          }
        />
        <Select
          label="Khoa account sau hieu luc"
          value={form.lockAccountAfterEffectiveDate}
          disabled={!request || saving}
          options={[
            { value: "true", label: "Co" },
            { value: "false", label: "Khong" },
          ]}
          onChange={(event) =>
            setForm((prev) => ({ ...prev, lockAccountAfterEffectiveDate: event.target.value }))
          }
        />
        <label className="block md:col-span-2">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">HR note</span>
          <textarea
            className="hicas-input min-h-24 resize-y"
            disabled={!request || saving}
            value={form.note}
            onChange={(event) => setForm((prev) => ({ ...prev, note: event.target.value }))}
          />
        </label>
        <div className="md:col-span-2">
          <Button
            type="submit"
            variant={form.isApproved === "true" ? "primary" : "danger"}
            iconLeft={form.isApproved === "true" ? <CheckCircle2 size={16} /> : <XCircle size={16} />}
            isLoading={saving}
            disabled={!request}
          >
            Gui HR review
          </Button>
        </div>
      </form>
    </Card>
  );
};

const toNumberOrNull = (value: string) => {
  const numericValue = Number(value);
  return Number.isInteger(numericValue) && numericValue > 0 ? numericValue : null;
};
