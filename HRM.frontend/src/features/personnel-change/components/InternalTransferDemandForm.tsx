import { useState, type FormEvent } from "react";
import { Send } from "lucide-react";
import { Button, Card, Input, Select } from "../../../components/ui";
import { usePersonnelChangeLookups } from "../hooks/usePersonnelChangeLookups";
import {
  DepartmentPicker,
  ManagerPicker,
  PositionPicker,
} from "./PersonnelChangePickers";
import type { InternalTransferDemandRequest } from "../types/personnelChange";

type Props = {
  saving?: boolean;
  onSubmit: (payload: InternalTransferDemandRequest) => Promise<boolean>;
};

export const InternalTransferDemandForm = ({ saving, onSubmit }: Props) => {
  const lookups = usePersonnelChangeLookups();
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
    <Card title="Nhu cầu thuyên chuyển" description="Ghi nhận nhu cầu trước khi HR chọn nhân sự phù hợp.">
      <form className="grid gap-4 md:grid-cols-2" onSubmit={submit}>
        <DepartmentPicker
          label="Phòng ban yêu cầu"
          departments={lookups.departments}
          helperText={lookups.loading ? "Đang tải danh sách phòng ban..." : undefined}
          required
          value={form.requestedDepartmentId}
          onChange={(value) => setForm((prev) => ({ ...prev, requestedDepartmentId: value }))}
        />
        <PositionPicker
          label="Chức danh dự kiến"
          positions={lookups.positions}
          value={form.requestedPositionId}
          onChange={(value) => setForm((prev) => ({ ...prev, requestedPositionId: value }))}
        />
        <ManagerPicker
          label="Quản lý mới"
          managers={lookups.managers}
          value={form.requestedManagerId}
          onChange={(value) => setForm((prev) => ({ ...prev, requestedManagerId: value }))}
        />
        <Select
          label="Mức ưu tiên"
          name="urgencyLevel"
          value={form.urgencyLevel}
          options={[
            { value: "Low", label: "Thấp" },
            { value: "Normal", label: "Bình thường" },
            { value: "High", label: "Cao" },
            { value: "Critical", label: "Khẩn cấp" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, urgencyLevel: event.target.value }))}
        />
        <Input
          label="Ngày hiệu lực dự kiến"
          name="expectedEffectiveDate"
          type="date"
          value={form.expectedEffectiveDate}
          onChange={(event) => setForm((prev) => ({ ...prev, expectedEffectiveDate: event.target.value }))}
        />
        <Input
          label="Kỹ năng cần có"
          name="requiredSkills"
          value={form.requiredSkills}
          onChange={(event) => setForm((prev) => ({ ...prev, requiredSkills: event.target.value }))}
        />
        <label className="block md:col-span-2">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Lý do</span>
          <textarea
            className="hicas-input min-h-24 resize-y"
            value={form.reason}
            onChange={(event) => setForm((prev) => ({ ...prev, reason: event.target.value }))}
          />
        </label>
        <div className="md:col-span-2">
          <Button type="submit" iconLeft={<Send size={16} />} isLoading={saving}>
            Gửi nhu cầu
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
