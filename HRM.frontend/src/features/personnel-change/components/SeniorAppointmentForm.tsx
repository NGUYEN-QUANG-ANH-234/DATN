import { useState, type FormEvent } from "react";
import { Send } from "lucide-react";
import { Button, Card, Input, Select } from "../../../components/ui";
import {
  PersonnelChangeContractFlowType,
  type CreateSeniorAppointmentRequest,
} from "../types/personnelChange";

type Props = {
  saving?: boolean;
  onSubmit: (payload: CreateSeniorAppointmentRequest) => Promise<boolean>;
};

export const SeniorAppointmentForm = ({ saving, onSubmit }: Props) => {
  const [form, setForm] = useState({
    employeeId: "",
    newDepartmentId: "",
    newPositionId: "",
    newJobLevelId: "",
    reportsToManagerId: "",
    isDepartmentManager: "false",
    relatedContractId: "",
    contractFlowType: String(PersonnelChangeContractFlowType.ContractAddendum),
    effectiveDate: "",
    reason: "",
  });

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const ok = await onSubmit({
      employeeId: Number(form.employeeId),
      newDepartmentId: toNumberOrNull(form.newDepartmentId),
      newPositionId: Number(form.newPositionId),
      newJobLevelId: toNumberOrNull(form.newJobLevelId),
      reportsToManagerId: toNumberOrNull(form.reportsToManagerId),
      isDepartmentManager: form.isDepartmentManager === "true",
      reason: form.reason || null,
      effectiveDate: form.effectiveDate || null,
      relatedContractId: toNumberOrNull(form.relatedContractId),
      contractFlowType: Number(form.contractFlowType) as CreateSeniorAppointmentRequest["contractFlowType"],
    });

    if (ok) {
      setForm((prev) => ({
        ...prev,
        reason: "",
      }));
    }
  };

  return (
    <Card title="Senior appointment" description="Tao ho so bo nhiem NSCC va gui nhan su xac nhan.">
      <form className="grid gap-4 md:grid-cols-2" onSubmit={submit}>
        <Input
          label="Nhan su"
          name="employeeId"
          type="number"
          min={1}
          required
          value={form.employeeId}
          onChange={(event) => setForm((prev) => ({ ...prev, employeeId: event.target.value }))}
        />
        <Input
          label="Chuc danh moi"
          name="newPositionId"
          type="number"
          min={1}
          required
          value={form.newPositionId}
          onChange={(event) => setForm((prev) => ({ ...prev, newPositionId: event.target.value }))}
        />
        <Input
          label="Phong ban"
          name="newDepartmentId"
          type="number"
          min={1}
          value={form.newDepartmentId}
          onChange={(event) => setForm((prev) => ({ ...prev, newDepartmentId: event.target.value }))}
        />
        <Input
          label="Job level moi"
          name="newJobLevelId"
          type="number"
          min={1}
          value={form.newJobLevelId}
          onChange={(event) => setForm((prev) => ({ ...prev, newJobLevelId: event.target.value }))}
        />
        <Input
          label="Quan ly truc tiep"
          name="reportsToManagerId"
          type="number"
          min={1}
          value={form.reportsToManagerId}
          onChange={(event) => setForm((prev) => ({ ...prev, reportsToManagerId: event.target.value }))}
        />
        <Select
          label="Bo nhiem truong phong"
          name="isDepartmentManager"
          value={form.isDepartmentManager}
          options={[
            { value: "false", label: "Khong" },
            { value: "true", label: "Co" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, isDepartmentManager: event.target.value }))}
        />
        <Input
          label="Hop dong lien quan"
          name="relatedContractId"
          type="number"
          min={1}
          value={form.relatedContractId}
          onChange={(event) => setForm((prev) => ({ ...prev, relatedContractId: event.target.value }))}
        />
        <Select
          label="Loai contract flow"
          name="contractFlowType"
          value={form.contractFlowType}
          options={[
            { value: String(PersonnelChangeContractFlowType.ContractAddendum), label: "Phu luc hop dong" },
            { value: String(PersonnelChangeContractFlowType.NewContract), label: "Hop dong moi" },
            { value: String(PersonnelChangeContractFlowType.ContractRenewal), label: "Gia han hop dong" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, contractFlowType: event.target.value }))}
        />
        <Input
          label="Ngay hieu luc"
          name="effectiveDate"
          type="date"
          value={form.effectiveDate}
          onChange={(event) => setForm((prev) => ({ ...prev, effectiveDate: event.target.value }))}
        />
        <label className="block md:col-span-2">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Ly do bo nhiem</span>
          <textarea
            className="hicas-input min-h-24 resize-y"
            value={form.reason}
            onChange={(event) => setForm((prev) => ({ ...prev, reason: event.target.value }))}
          />
        </label>
        <div className="md:col-span-2">
          <Button type="submit" iconLeft={<Send size={16} />} isLoading={saving}>
            Tao ho so bo nhiem
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
