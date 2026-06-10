import { useState, type FormEvent } from "react";
import { Send } from "lucide-react";
import { Button, Card, Input, Select } from "../../../components/ui";
import {
  EmployeeType,
  PersonnelChangeContractFlowType,
  type CreateConvertOfficialRequest,
} from "../types/personnelChange";
import {
  useEmployeePersonnelChangeLookups,
  usePersonnelChangeLookups,
} from "../hooks/usePersonnelChangeLookups";
import {
  ContractPicker,
  EmployeePicker,
  JobLevelPicker,
  PerformanceReviewPicker,
  PositionPicker,
} from "./PersonnelChangePickers";

type Props = {
  saving?: boolean;
  onSubmit: (payload: CreateConvertOfficialRequest) => Promise<boolean>;
};

export const ConvertOfficialForm = ({ saving, onSubmit }: Props) => {
  const lookups = usePersonnelChangeLookups();
  const [form, setForm] = useState({
    employeeId: "",
    newPositionId: "",
    newJobLevelId: "",
    newEmployeeType: String(EmployeeType.Official),
    sourcePerformanceReviewId: "",
    requiresContractFlow: "true",
    contractFlowType: String(PersonnelChangeContractFlowType.ContractRenewal),
    relatedContractId: "",
    effectiveDate: "",
    reason: "",
  });
  const employeeId = toNumberOrNull(form.employeeId);
  const related = useEmployeePersonnelChangeLookups(employeeId);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const ok = await onSubmit({
      employeeId: Number(form.employeeId),
      newPositionId: toNumberOrNull(form.newPositionId),
      newJobLevelId: toNumberOrNull(form.newJobLevelId),
      newEmployeeType: Number(form.newEmployeeType) as CreateConvertOfficialRequest["newEmployeeType"],
      effectiveDate: form.effectiveDate || null,
      reason: form.reason || null,
      sourcePerformanceReviewId: toNumberOrNull(form.sourcePerformanceReviewId),
      requiresContractFlow: form.requiresContractFlow === "true",
      contractFlowType: Number(form.contractFlowType) as CreateConvertOfficialRequest["contractFlowType"],
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
    <Card title="Chuyển chính thức" description="Tạo hồ sơ chuyển nhân sự sang trạng thái chính thức.">
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
              sourcePerformanceReviewId: "",
              relatedContractId: "",
            }))
          }
        />
        <Select
          label="Loại nhân sự mới"
          value={form.newEmployeeType}
          options={[
            { value: String(EmployeeType.Official), label: "Chính thức" },
            { value: String(EmployeeType.Contractual), label: "Hợp đồng" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, newEmployeeType: event.target.value }))}
        />
        <PositionPicker
          label="Chức danh mới"
          positions={lookups.positions}
          value={form.newPositionId}
          onChange={(value) => setForm((prev) => ({ ...prev, newPositionId: value }))}
        />
        <JobLevelPicker
          label="Cấp bậc mới"
          jobLevels={lookups.jobLevels}
          value={form.newJobLevelId}
          onChange={(value) => setForm((prev) => ({ ...prev, newJobLevelId: value }))}
        />
        <PerformanceReviewPicker
          label="Đánh giá hiệu suất"
          reviews={related.performanceReviews}
          value={form.sourcePerformanceReviewId}
          disabled={!employeeId}
          helperText={!employeeId ? "Chọn nhân sự trước để xem đánh giá." : undefined}
          onChange={(value) => setForm((prev) => ({ ...prev, sourcePerformanceReviewId: value }))}
        />
        <ContractPicker
          label="Hợp đồng liên quan"
          contracts={related.contracts}
          value={form.relatedContractId}
          disabled={!employeeId}
          helperText={!employeeId ? "Chọn nhân sự trước để xem hợp đồng." : undefined}
          onChange={(value) => setForm((prev) => ({ ...prev, relatedContractId: value }))}
        />
        <Select
          label="Cần xử lý hợp đồng"
          value={form.requiresContractFlow}
          options={[
            { value: "true", label: "Có" },
            { value: "false", label: "Không" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, requiresContractFlow: event.target.value }))}
        />
        <Select
          label="Loại xử lý hợp đồng"
          value={form.contractFlowType}
          options={[
            { value: String(PersonnelChangeContractFlowType.ContractRenewal), label: "Gia hạn hợp đồng" },
            { value: String(PersonnelChangeContractFlowType.ContractAddendum), label: "Phụ lục hợp đồng" },
            { value: String(PersonnelChangeContractFlowType.NewContract), label: "Hợp đồng mới" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, contractFlowType: event.target.value }))}
        />
        <Input
          label="Ngày hiệu lực"
          type="date"
          value={form.effectiveDate}
          onChange={(event) => setForm((prev) => ({ ...prev, effectiveDate: event.target.value }))}
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
        <div className="md:col-span-2">
          <Button type="submit" iconLeft={<Send size={16} />} isLoading={saving}>
            Tạo hồ sơ chuyển chính thức
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
