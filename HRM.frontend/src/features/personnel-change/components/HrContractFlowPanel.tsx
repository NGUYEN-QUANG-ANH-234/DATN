import { useState, type FormEvent } from "react";
import { FilePlus2 } from "lucide-react";
import { Button, Card, Input, Select } from "../../../components/ui";
import {
  PersonnelChangeContractFlowType,
  type HrContractFlowRequest,
  type PersonnelChangeDetail,
} from "../types/personnelChange";

type Props = {
  request?: PersonnelChangeDetail | null;
  saving?: boolean;
  onSubmit: (id: number, payload: HrContractFlowRequest) => Promise<boolean>;
};

export const HrContractFlowPanel = ({ request, saving, onSubmit }: Props) => {
  const [form, setForm] = useState({
    contractFlowType: String(PersonnelChangeContractFlowType.ContractAddendum),
    relatedContractId: "",
    note: "",
  });

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!request) return;
    await onSubmit(request.id, {
      contractFlowType: Number(form.contractFlowType) as HrContractFlowRequest["contractFlowType"],
      relatedContractId: toNumberOrNull(form.relatedContractId),
      note: form.note || null,
    });
  };

  return (
    <Card title="HR contract flow" description="Tao hop dong/phu luc ben Module 3, Module 7 chi luu link.">
      <form className="space-y-4" onSubmit={submit}>
        <Select
          label="Loai flow"
          value={form.contractFlowType}
          options={[
            { value: String(PersonnelChangeContractFlowType.ContractAddendum), label: "Phu luc hop dong" },
            { value: String(PersonnelChangeContractFlowType.NewContract), label: "Hop dong moi" },
            { value: String(PersonnelChangeContractFlowType.ContractRenewal), label: "Gia han hop dong" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, contractFlowType: event.target.value }))}
        />
        <Input
          label="Hop dong lien quan"
          type="number"
          min={1}
          value={form.relatedContractId}
          onChange={(event) => setForm((prev) => ({ ...prev, relatedContractId: event.target.value }))}
        />
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Ghi chu HR</span>
          <textarea
            className="hicas-input min-h-24 resize-y"
            value={form.note}
            onChange={(event) => setForm((prev) => ({ ...prev, note: event.target.value }))}
          />
        </label>
        <Button type="submit" iconLeft={<FilePlus2 size={16} />} isLoading={saving} disabled={!request}>
          Tao contract flow
        </Button>
      </form>
    </Card>
  );
};

const toNumberOrNull = (value: string) => {
  const numericValue = Number(value);
  return Number.isInteger(numericValue) && numericValue > 0 ? numericValue : null;
};
