import { useState, type FormEvent } from "react";
import { Send } from "lucide-react";
import { Button, Card, Input, Select } from "../../../components/ui";
import {
  EmployeeType,
  PersonnelChangeContractFlowType,
  PersonnelChangePromotionType,
  type CreatePromotionRequest,
} from "../types/personnelChange";

type Props = {
  saving?: boolean;
  onSubmit: (payload: CreatePromotionRequest) => Promise<boolean>;
};

export const PromotionForm = ({ saving, onSubmit }: Props) => {
  const [form, setForm] = useState({
    employeeId: "",
    promotionType: String(PersonnelChangePromotionType.PositionPromotion),
    newPositionId: "",
    newJobLevelId: "",
    newEmployeeType: "",
    sourcePerformanceReviewId: "",
    requiresContractFlow: "true",
    contractFlowType: String(PersonnelChangeContractFlowType.ContractAddendum),
    relatedContractId: "",
    effectiveDate: "",
    reason: "",
  });

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const ok = await onSubmit({
      employeeId: Number(form.employeeId),
      promotionType: Number(form.promotionType) as CreatePromotionRequest["promotionType"],
      newPositionId: toNumberOrNull(form.newPositionId),
      newJobLevelId: toNumberOrNull(form.newJobLevelId),
      newEmployeeType: toEmployeeTypeOrNull(form.newEmployeeType),
      effectiveDate: form.effectiveDate || null,
      reason: form.reason || null,
      sourcePerformanceReviewId: toNumberOrNull(form.sourcePerformanceReviewId),
      requiresContractFlow: form.requiresContractFlow === "true",
      contractFlowType: Number(form.contractFlowType) as CreatePromotionRequest["contractFlowType"],
      relatedContractId: toNumberOrNull(form.relatedContractId),
    });

    if (ok) {
      setForm((prev) => ({
        ...prev,
        reason: "",
        sourcePerformanceReviewId: "",
      }));
    }
  };

  return (
    <Card title="Promotion" description="Tao ho so thang tien theo chuc danh, job level hoac loai nhan su.">
      <form className="grid gap-4 md:grid-cols-2" onSubmit={submit}>
        <Input
          label="Nhan su"
          type="number"
          min={1}
          required
          value={form.employeeId}
          onChange={(event) => setForm((prev) => ({ ...prev, employeeId: event.target.value }))}
        />
        <Select
          label="Loai thang tien"
          value={form.promotionType}
          options={[
            { value: String(PersonnelChangePromotionType.PositionPromotion), label: "Position promotion" },
            { value: String(PersonnelChangePromotionType.JobLevelPromotion), label: "Job level promotion" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, promotionType: event.target.value }))}
        />
        <Input
          label="Chuc danh moi"
          type="number"
          min={1}
          value={form.newPositionId}
          onChange={(event) => setForm((prev) => ({ ...prev, newPositionId: event.target.value }))}
        />
        <Input
          label="Job level moi"
          type="number"
          min={1}
          value={form.newJobLevelId}
          onChange={(event) => setForm((prev) => ({ ...prev, newJobLevelId: event.target.value }))}
        />
        <Select
          label="Loai nhan su moi"
          value={form.newEmployeeType}
          options={[
            { value: "", label: "Khong doi" },
            { value: String(EmployeeType.Official), label: "Official" },
            { value: String(EmployeeType.Probation), label: "Probation" },
            { value: String(EmployeeType.PartTime), label: "PartTime" },
            { value: String(EmployeeType.Contractual), label: "Contractual" },
            { value: String(EmployeeType.Intern), label: "Intern" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, newEmployeeType: event.target.value }))}
        />
        <Input
          label="Performance review"
          type="number"
          min={1}
          value={form.sourcePerformanceReviewId}
          onChange={(event) =>
            setForm((prev) => ({ ...prev, sourcePerformanceReviewId: event.target.value }))
          }
        />
        <Select
          label="Can contract flow"
          value={form.requiresContractFlow}
          options={[
            { value: "true", label: "Co" },
            { value: "false", label: "Khong" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, requiresContractFlow: event.target.value }))}
        />
        <Select
          label="Loai contract flow"
          value={form.contractFlowType}
          options={[
            { value: String(PersonnelChangeContractFlowType.ContractAddendum), label: "Phu luc hop dong" },
            { value: String(PersonnelChangeContractFlowType.ContractRenewal), label: "Gia han hop dong" },
            { value: String(PersonnelChangeContractFlowType.NewContract), label: "Hop dong moi" },
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
        <Input
          label="Ngay hieu luc"
          type="date"
          value={form.effectiveDate}
          onChange={(event) => setForm((prev) => ({ ...prev, effectiveDate: event.target.value }))}
        />
        <label className="block md:col-span-2">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Ly do</span>
          <textarea
            className="hicas-input min-h-20 resize-y"
            value={form.reason}
            onChange={(event) => setForm((prev) => ({ ...prev, reason: event.target.value }))}
          />
        </label>
        <div className="md:col-span-2">
          <Button type="submit" iconLeft={<Send size={16} />} isLoading={saving}>
            Tao ho so thang tien
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

const toEmployeeTypeOrNull = (value: string) => {
  const numericValue = Number(value);
  return Number.isInteger(numericValue) ? (numericValue as EmployeeType) : null;
};
