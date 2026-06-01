import { useState, type FormEvent } from "react";
import { Send } from "lucide-react";
import { Button, Card, Input, Select } from "../../../components/ui";
import type { InternalTransferDemandRequest } from "../types/personnelChange";

type Props = {
  saving?: boolean;
  onSubmit: (payload: InternalTransferDemandRequest) => Promise<boolean>;
};

export const InternalTransferDemandForm = ({ saving, onSubmit }: Props) => {
  const [form, setForm] = useState({
    requestedDepartmentId: "",
    requestedPositionId: "",
    requestedManagerId: "",
    reason: "",
    urgencyLevel: "Normal",
    expectedEffectiveDate: "",
    requiredSkills: "",
  });

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const ok = await onSubmit({
      requestedDepartmentId: Number(form.requestedDepartmentId),
      requestedPositionId: toNumberOrNull(form.requestedPositionId),
      requestedManagerId: toNumberOrNull(form.requestedManagerId),
      reason: form.reason || null,
      urgencyLevel: form.urgencyLevel || null,
      expectedEffectiveDate: form.expectedEffectiveDate || null,
      requiredSkills: form.requiredSkills || null,
    });

    if (ok) {
      setForm((prev) => ({
        ...prev,
        reason: "",
        requiredSkills: "",
      }));
    }
  };

  return (
    <Card title="Internal transfer demand" description="Tao demand truoc khi HR chon nhan su phu hop.">
      <form className="grid gap-4 md:grid-cols-2" onSubmit={submit}>
        <Input
          label="Phong ban yeu cau"
          name="requestedDepartmentId"
          type="number"
          min={1}
          required
          value={form.requestedDepartmentId}
          onChange={(event) => setForm((prev) => ({ ...prev, requestedDepartmentId: event.target.value }))}
        />
        <Input
          label="Chuc danh du kien"
          name="requestedPositionId"
          type="number"
          min={1}
          value={form.requestedPositionId}
          onChange={(event) => setForm((prev) => ({ ...prev, requestedPositionId: event.target.value }))}
        />
        <Input
          label="Quan ly moi"
          name="requestedManagerId"
          type="number"
          min={1}
          value={form.requestedManagerId}
          onChange={(event) => setForm((prev) => ({ ...prev, requestedManagerId: event.target.value }))}
        />
        <Select
          label="Muc uu tien"
          name="urgencyLevel"
          value={form.urgencyLevel}
          options={[
            { value: "Low", label: "Low" },
            { value: "Normal", label: "Normal" },
            { value: "High", label: "High" },
            { value: "Critical", label: "Critical" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, urgencyLevel: event.target.value }))}
        />
        <Input
          label="Ngay hieu luc du kien"
          name="expectedEffectiveDate"
          type="date"
          value={form.expectedEffectiveDate}
          onChange={(event) => setForm((prev) => ({ ...prev, expectedEffectiveDate: event.target.value }))}
        />
        <Input
          label="Ky nang can co"
          name="requiredSkills"
          value={form.requiredSkills}
          onChange={(event) => setForm((prev) => ({ ...prev, requiredSkills: event.target.value }))}
        />
        <label className="block md:col-span-2">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Ly do</span>
          <textarea
            className="hicas-input min-h-24 resize-y"
            value={form.reason}
            onChange={(event) => setForm((prev) => ({ ...prev, reason: event.target.value }))}
          />
        </label>
        <div className="md:col-span-2">
          <Button type="submit" iconLeft={<Send size={16} />} isLoading={saving}>
            Gui demand
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
