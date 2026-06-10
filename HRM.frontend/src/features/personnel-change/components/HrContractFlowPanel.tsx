import { useState, type FormEvent } from "react";
import { FilePlus2 } from "lucide-react";
import { Button, Card, Select } from "../../../components/ui";
import {
  PersonnelChangeContractFlowType,
  type HrContractFlowRequest,
  type PersonnelChangeDetail,
} from "../types/personnelChange";
import { useEmployeePersonnelChangeLookups } from "../hooks/usePersonnelChangeLookups";
import { ContractPicker } from "./PersonnelChangePickers";

type Props = {
  request?: PersonnelChangeDetail | null;
  saving?: boolean;
  onSubmit: (id: number, payload: HrContractFlowRequest) => Promise<boolean>;
};

export const HrContractFlowPanel = ({ request, saving, onSubmit }: Props) => {
  const related = useEmployeePersonnelChangeLookups(request?.employeeId);
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
    <Card
      title="Xử lý hợp đồng"
      description="Tạo hợp đồng hoặc phụ lục liên quan cho hồ sơ biến động."
    >
      <form className="space-y-4" onSubmit={submit}>
        <Select
          label="Loại xử lý"
          value={form.contractFlowType}
          options={[
            { value: String(PersonnelChangeContractFlowType.ContractAddendum), label: "Phụ lục hợp đồng" },
            { value: String(PersonnelChangeContractFlowType.NewContract), label: "Hợp đồng mới" },
            { value: String(PersonnelChangeContractFlowType.ContractRenewal), label: "Gia hạn hợp đồng" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, contractFlowType: event.target.value }))}
        />
        <ContractPicker
          contracts={related.contracts}
          value={form.relatedContractId}
          disabled={!request?.employeeId}
          helperText={!request?.employeeId ? "Hồ sơ chưa có nhân sự để tra cứu hợp đồng." : undefined}
          onChange={(value) => setForm((prev) => ({ ...prev, relatedContractId: value }))}
        />
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">
            Ghi chú HR
          </span>
          <textarea
            className="hicas-input min-h-24 resize-y"
            value={form.note}
            onChange={(event) => setForm((prev) => ({ ...prev, note: event.target.value }))}
          />
        </label>
        <Button type="submit" iconLeft={<FilePlus2 size={16} />} isLoading={saving} disabled={!request}>
          Tạo xử lý hợp đồng
        </Button>
      </form>
    </Card>
  );
};

const toNumberOrNull = (value: string) => {
  const numericValue = Number(value);
  return Number.isInteger(numericValue) && numericValue > 0 ? numericValue : null;
};
