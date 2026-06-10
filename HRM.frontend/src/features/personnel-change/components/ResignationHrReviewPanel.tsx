import { useState, type FormEvent } from "react";
import { CheckCircle2, XCircle } from "lucide-react";
import { Button, Card, Select } from "../../../components/ui";
import {
  useEmployeePersonnelChangeLookups,
} from "../hooks/usePersonnelChangeLookups";
import type { HrReviewResignationRequest, PersonnelChangeDetail } from "../types/personnelChange";
import { ContractPicker } from "./PersonnelChangePickers";

type Props = {
  request?: PersonnelChangeDetail | null;
  saving?: boolean;
  onSubmit: (id: number, payload: HrReviewResignationRequest) => Promise<boolean>;
};

export const ResignationHrReviewPanel = ({ request, saving, onSubmit }: Props) => {
  const related = useEmployeePersonnelChangeLookups(request?.employeeId);
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
    <Card
      title="HR kiểm tra hồ sơ"
      description="HR kiểm tra hợp đồng, quyết toán cuối cùng và thời điểm khóa tài khoản."
    >
      <form className="grid gap-4 md:grid-cols-2" onSubmit={submit}>
        <Select
          label="Quyết định"
          value={form.isApproved}
          disabled={!request || saving}
          options={[
            { value: "true", label: "Đồng ý" },
            { value: "false", label: "Từ chối" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, isApproved: event.target.value }))}
        />
        <ContractPicker
          contracts={related.contracts}
          value={form.relatedContractId}
          disabled={!request || saving || !request.employeeId}
          helperText={!request?.employeeId ? "Hồ sơ chưa có nhân sự để tra cứu hợp đồng." : undefined}
          onChange={(value) => setForm((prev) => ({ ...prev, relatedContractId: value }))}
        />
        <Select
          label="Tạo quyết toán cuối cùng"
          value={form.requiresFinalSettlement}
          disabled={!request || saving}
          options={[
            { value: "true", label: "Có" },
            { value: "false", label: "Không" },
          ]}
          onChange={(event) =>
            setForm((prev) => ({ ...prev, requiresFinalSettlement: event.target.value }))
          }
        />
        <Select
          label="Khóa tài khoản sau ngày hiệu lực"
          value={form.lockAccountAfterEffectiveDate}
          disabled={!request || saving}
          options={[
            { value: "true", label: "Có" },
            { value: "false", label: "Không" },
          ]}
          onChange={(event) =>
            setForm((prev) => ({ ...prev, lockAccountAfterEffectiveDate: event.target.value }))
          }
        />
        <label className="block md:col-span-2">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">
            Ghi chú HR
          </span>
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
            Gửi kiểm tra HR
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
