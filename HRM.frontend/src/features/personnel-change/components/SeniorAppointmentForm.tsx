import { useState, type FormEvent } from "react";
import { Send } from "lucide-react";
import { Button, Card, Input, Select } from "../../../components/ui";
import {
  PersonnelChangeContractFlowType,
  type CreateSeniorAppointmentRequest,
} from "../types/personnelChange";
import {
  useEmployeePersonnelChangeLookups,
  usePersonnelChangeLookups,
} from "../hooks/usePersonnelChangeLookups";
import {
  ContractPicker,
  DepartmentPicker,
  EmployeePicker,
  JobLevelPicker,
  ManagerPicker,
  PositionPicker,
} from "./PersonnelChangePickers";

type Props = {
  saving?: boolean;
  onSubmit: (payload: CreateSeniorAppointmentRequest) => Promise<boolean>;
};

export const SeniorAppointmentForm = ({ saving, onSubmit }: Props) => {
  const lookups = usePersonnelChangeLookups();
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
  const employeeId = toNumberOrNull(form.employeeId);
  const related = useEmployeePersonnelChangeLookups(employeeId);

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
    <Card title="Bổ nhiệm cấp cao" description="Tạo hồ sơ bổ nhiệm và gửi nhân sự xác nhận.">
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
              relatedContractId: "",
            }))
          }
        />
        <PositionPicker
          label="Chức danh mới"
          positions={lookups.positions}
          required
          value={form.newPositionId}
          onChange={(value) => setForm((prev) => ({ ...prev, newPositionId: value }))}
        />
        <DepartmentPicker
          label="Phòng ban"
          departments={lookups.departments}
          value={form.newDepartmentId}
          onChange={(value) => setForm((prev) => ({ ...prev, newDepartmentId: value }))}
        />
        <JobLevelPicker
          label="Cấp bậc mới"
          jobLevels={lookups.jobLevels}
          value={form.newJobLevelId}
          onChange={(value) => setForm((prev) => ({ ...prev, newJobLevelId: value }))}
        />
        <ManagerPicker
          label="Quản lý trực tiếp"
          managers={lookups.managers}
          value={form.reportsToManagerId}
          onChange={(value) => setForm((prev) => ({ ...prev, reportsToManagerId: value }))}
        />
        <Select
          label="Bổ nhiệm trưởng phòng"
          name="isDepartmentManager"
          value={form.isDepartmentManager}
          options={[
            { value: "false", label: "Không" },
            { value: "true", label: "Có" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, isDepartmentManager: event.target.value }))}
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
          label="Loại xử lý hợp đồng"
          name="contractFlowType"
          value={form.contractFlowType}
          options={[
            { value: String(PersonnelChangeContractFlowType.ContractAddendum), label: "Phụ lục hợp đồng" },
            { value: String(PersonnelChangeContractFlowType.NewContract), label: "Hợp đồng mới" },
            { value: String(PersonnelChangeContractFlowType.ContractRenewal), label: "Gia hạn hợp đồng" },
          ]}
          onChange={(event) => setForm((prev) => ({ ...prev, contractFlowType: event.target.value }))}
        />
        <Input
          label="Ngày hiệu lực"
          name="effectiveDate"
          type="date"
          value={form.effectiveDate}
          onChange={(event) => setForm((prev) => ({ ...prev, effectiveDate: event.target.value }))}
        />
        <label className="block md:col-span-2">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">
            Lý do bổ nhiệm
          </span>
          <textarea
            className="hicas-input min-h-24 resize-y"
            value={form.reason}
            onChange={(event) => setForm((prev) => ({ ...prev, reason: event.target.value }))}
          />
        </label>
        <div className="md:col-span-2">
          <Button type="submit" iconLeft={<Send size={16} />} isLoading={saving}>
            Tạo hồ sơ bổ nhiệm
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
