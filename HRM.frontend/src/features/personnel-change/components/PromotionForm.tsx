import { useState, type FormEvent } from "react";
import { Send } from "lucide-react";
import { Button, Card, Input, Select } from "../../../components/ui";
import {
  EmployeeType,
  PersonnelChangeContractFlowType,
  PersonnelChangePromotionType,
  type CreatePromotionRequest,
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
  onSubmit: (payload: CreatePromotionRequest) => Promise<boolean>;
};

export const PromotionForm = ({ saving, onSubmit }: Props) => {
  const lookups = usePersonnelChangeLookups();
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
  const employeeId = toNumberOrNull(form.employeeId);
  const related = useEmployeePersonnelChangeLookups(employeeId);

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
    <Card
      title="Tạo hồ sơ thăng tiến"
      description="Ghi nhận đề xuất thăng tiến theo chức danh, cấp bậc hoặc loại nhân sự."
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
              sourcePerformanceReviewId: "",
              relatedContractId: "",
            }))
          }
        />
        <Select
          label="Loại thăng tiến"
          value={form.promotionType}
          options={[
            { value: String(PersonnelChangePromotionType.PositionPromotion), label: "Thăng tiến chức danh" },
            { value: String(PersonnelChangePromotionType.JobLevelPromotion), label: "Nâng cấp bậc" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, promotionType: event.target.value }))}
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
        <Select
          label="Loại nhân sự mới"
          value={form.newEmployeeType}
          options={[
            { value: "", label: "Không đổi" },
            { value: String(EmployeeType.Official), label: "Chính thức" },
            { value: String(EmployeeType.Probation), label: "Thử việc" },
            { value: String(EmployeeType.PartTime), label: "Bán thời gian" },
            { value: String(EmployeeType.Contractual), label: "Hợp đồng" },
            { value: String(EmployeeType.Intern), label: "Thực tập" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, newEmployeeType: event.target.value }))}
        />
        <PerformanceReviewPicker
          label="Đánh giá hiệu suất"
          reviews={related.performanceReviews}
          value={form.sourcePerformanceReviewId}
          disabled={!employeeId}
          helperText={!employeeId ? "Chọn nhân sự trước để xem đánh giá." : undefined}
          onChange={(value) => setForm((prev) => ({ ...prev, sourcePerformanceReviewId: value }))}
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
            { value: String(PersonnelChangeContractFlowType.ContractAddendum), label: "Phụ lục hợp đồng" },
            { value: String(PersonnelChangeContractFlowType.ContractRenewal), label: "Gia hạn hợp đồng" },
            { value: String(PersonnelChangeContractFlowType.NewContract), label: "Hợp đồng mới" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, contractFlowType: event.target.value }))}
        />
        <ContractPicker
          label="Hợp đồng liên quan"
          contracts={related.contracts}
          value={form.relatedContractId}
          disabled={!employeeId}
          helperText={!employeeId ? "Chọn nhân sự trước để xem hợp đồng." : undefined}
          onChange={(value) => setForm((prev) => ({ ...prev, relatedContractId: value }))}
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
            Tạo hồ sơ thăng tiến
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
