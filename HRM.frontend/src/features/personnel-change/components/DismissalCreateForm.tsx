import { useState, type FormEvent } from "react";
import { Send } from "lucide-react";
import { Button, Card, Input, Select } from "../../../components/ui";
import type { CreateDismissalRequest } from "../types/personnelChange";
import {
  useEmployeePersonnelChangeLookups,
  usePersonnelChangeLookups,
} from "../hooks/usePersonnelChangeLookups";
import {
  ContractPicker,
  EmployeePicker,
  EvidenceFileUpload,
  PenaltyRecordPicker,
} from "./PersonnelChangePickers";

type Props = {
  saving?: boolean;
  onSubmit: (payload: CreateDismissalRequest) => Promise<boolean>;
};

export const DismissalCreateForm = ({ saving, onSubmit }: Props) => {
  const lookups = usePersonnelChangeLookups();
  const [form, setForm] = useState({
    employeeId: "",
    sourcePenaltyRecordId: "",
    reason: "",
    evidenceFilePath: "",
    hrNote: "",
    managerNote: "",
    responseDeadlineAt: "",
    effectiveDate: "",
    relatedContractId: "",
    lockAccountOnExecution: "true",
    requiresFinalSettlement: "true",
  });
  const employeeId = toNumberOrNull(form.employeeId);
  const related = useEmployeePersonnelChangeLookups(employeeId);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    await onSubmit({
      employeeId: Number(form.employeeId),
      sourcePenaltyRecordId: Number(form.sourcePenaltyRecordId),
      reason: form.reason || null,
      evidenceFilePath: form.evidenceFilePath || null,
      hrNote: form.hrNote || null,
      managerNote: form.managerNote || null,
      responseDeadlineAt: form.responseDeadlineAt || null,
      effectiveDate: form.effectiveDate || null,
      relatedContractId: toNumberOrNull(form.relatedContractId),
      lockAccountOnExecution: form.lockAccountOnExecution === "true",
      requiresFinalSettlement: form.requiresFinalSettlement === "true",
    });
  };

  return (
    <Card
      title="Tạo hồ sơ kỷ luật"
      description="Ghi nhận hồ sơ kỷ luật hoặc sa thải từ vi phạm đã xác định."
    >
      <form className="grid gap-4 md:grid-cols-2" onSubmit={submit}>
        <EmployeePicker
          label="Nhân sự"
          employees={lookups.employees}
          required
          value={form.employeeId}
          helperText={lookups.loading ? "Đang tải danh sách nhân sự..." : undefined}
          onChange={(value) =>
            setForm((prev) => ({
              ...prev,
              employeeId: value,
              sourcePenaltyRecordId: "",
              relatedContractId: "",
            }))
          }
        />
        <PenaltyRecordPicker
          penalties={related.penalties}
          required
          value={form.sourcePenaltyRecordId}
          disabled={!employeeId}
          helperText={!employeeId ? "Chọn nhân sự trước để xem hồ sơ vi phạm." : undefined}
          onChange={(value) => setForm((prev) => ({ ...prev, sourcePenaltyRecordId: value }))}
        />
        <EvidenceFileUpload
          value={form.evidenceFilePath}
          onUploaded={(filePath) => setForm((prev) => ({ ...prev, evidenceFilePath: filePath }))}
        />
        <ContractPicker
          contracts={related.contracts}
          value={form.relatedContractId}
          disabled={!employeeId}
          helperText={!employeeId ? "Chọn nhân sự trước để xem hợp đồng." : undefined}
          onChange={(value) => setForm((prev) => ({ ...prev, relatedContractId: value }))}
        />
        <Input
          label="Hạn giải trình"
          type="datetime-local"
          value={form.responseDeadlineAt}
          onChange={(event) => setForm((prev) => ({ ...prev, responseDeadlineAt: event.target.value }))}
        />
        <Input
          label="Ngày hiệu lực"
          type="date"
          value={form.effectiveDate}
          onChange={(event) => setForm((prev) => ({ ...prev, effectiveDate: event.target.value }))}
        />
        <Select
          label="Khóa tài khoản khi thực hiện"
          value={form.lockAccountOnExecution}
          options={[
            { value: "true", label: "Có" },
            { value: "false", label: "Không" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, lockAccountOnExecution: event.target.value }))}
        />
        <Select
          label="Tạo quyết toán cuối cùng"
          value={form.requiresFinalSettlement}
          options={[
            { value: "true", label: "Có" },
            { value: "false", label: "Không" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, requiresFinalSettlement: event.target.value }))}
        />
        <label className="block md:col-span-2">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">
            Lý do
          </span>
          <textarea
            className="hicas-input min-h-20 resize-y"
            value={form.reason}
            onChange={(event) => setForm((prev) => ({ ...prev, reason: event.target.value }))}
          />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">
            Ghi chú HR
          </span>
          <textarea
            className="hicas-input min-h-20 resize-y"
            value={form.hrNote}
            onChange={(event) => setForm((prev) => ({ ...prev, hrNote: event.target.value }))}
          />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">
            Ghi chú quản lý
          </span>
          <textarea
            className="hicas-input min-h-20 resize-y"
            value={form.managerNote}
            onChange={(event) => setForm((prev) => ({ ...prev, managerNote: event.target.value }))}
          />
        </label>
        <div className="md:col-span-2">
          <Button type="submit" iconLeft={<Send size={16} />} isLoading={saving}>
            Tạo hồ sơ
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
