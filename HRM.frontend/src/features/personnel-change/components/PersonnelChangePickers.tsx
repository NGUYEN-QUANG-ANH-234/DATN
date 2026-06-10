import { useState, type ChangeEvent } from "react";
import { Upload } from "lucide-react";
import { Select } from "../../../components/ui";
import { personnelChangeApi } from "../api/personnelChangeApi";
import type {
  PersonnelChangeContractOption,
  PersonnelChangeDepartmentOption,
  PersonnelChangeEmployeeOption,
  PersonnelChangeJobLevelOption,
  PersonnelChangePenaltyOption,
  PersonnelChangePerformanceReviewOption,
  PersonnelChangePositionOption,
} from "../types/personnelChange";

type BasePickerProps = {
  value: string;
  onChange: (value: string) => void;
  required?: boolean;
  disabled?: boolean;
  helperText?: string;
  placeholder?: string;
};

export const EmployeePicker = ({
  value,
  onChange,
  employees,
  label = "Nhân sự",
  placeholder,
  ...props
}: BasePickerProps & {
  employees: PersonnelChangeEmployeeOption[];
  label?: string;
}) => (
  <Select
    label={label}
    value={value}
    options={employees.map((employee) => ({
      value: employee.id,
      label: [
        `${employee.employeeCode} - ${employee.fullName}`,
        employee.departmentName,
        employee.positionName,
      ]
        .filter(Boolean)
        .join(" | "),
    }))}
    placeholder={placeholder ?? "Chọn nhân sự"}
    onChange={(event) => onChange(event.target.value)}
    {...props}
  />
);

export const DepartmentPicker = ({
  value,
  onChange,
  departments,
  label = "Phòng ban",
  placeholder,
  ...props
}: BasePickerProps & {
  departments: PersonnelChangeDepartmentOption[];
  label?: string;
}) => (
  <Select
    label={label}
    value={value}
    options={departments.map((department) => ({
      value: department.id,
      label: `${department.deptCode} - ${department.deptName}`,
    }))}
    placeholder={placeholder ?? "Chọn phòng ban"}
    onChange={(event) => onChange(event.target.value)}
    {...props}
  />
);

export const PositionPicker = ({
  value,
  onChange,
  positions,
  label = "Chức danh",
  placeholder,
  ...props
}: BasePickerProps & {
  positions: PersonnelChangePositionOption[];
  label?: string;
}) => (
  <Select
    label={label}
    value={value}
    options={positions.map((position) => ({
      value: position.id,
      label: `${position.title} | Cấp ${position.jobLevel}`,
    }))}
    placeholder={placeholder ?? "Chọn chức danh"}
    onChange={(event) => onChange(event.target.value)}
    {...props}
  />
);

export const JobLevelPicker = ({
  value,
  onChange,
  jobLevels,
  label = "Cấp bậc",
  placeholder,
  ...props
}: BasePickerProps & {
  jobLevels: PersonnelChangeJobLevelOption[];
  label?: string;
}) => (
  <Select
    label={label}
    value={value}
    options={jobLevels.map((level) => ({
      value: level.id,
      label: `${level.code} - ${level.name}`,
    }))}
    placeholder={placeholder ?? "Chọn cấp bậc"}
    onChange={(event) => onChange(event.target.value)}
    {...props}
  />
);

export const ManagerPicker = ({
  value,
  onChange,
  managers,
  label = "Quản lý",
  placeholder,
  ...props
}: BasePickerProps & {
  managers: PersonnelChangeEmployeeOption[];
  label?: string;
}) => (
  <Select
    label={label}
    value={value}
    options={managers.map((manager) => ({
      value: manager.id,
      label: [
        `${manager.employeeCode} - ${manager.fullName}`,
        manager.departmentName,
        manager.positionName,
      ]
        .filter(Boolean)
        .join(" | "),
    }))}
    placeholder={placeholder ?? "Chọn quản lý"}
    onChange={(event) => onChange(event.target.value)}
    {...props}
  />
);

export const PenaltyRecordPicker = ({
  value,
  onChange,
  penalties,
  label = "Hồ sơ vi phạm",
  placeholder,
  ...props
}: BasePickerProps & {
  penalties: PersonnelChangePenaltyOption[];
  label?: string;
}) => (
  <Select
    label={label}
    value={value}
    options={penalties.map((penalty) => ({
      value: penalty.id,
      label: [
        penalty.period,
        penalty.ruleCode,
        `${penalty.penaltyPoint} điểm`,
        penalty.status,
        penalty.reason,
      ]
        .filter(Boolean)
        .join(" | "),
    }))}
    placeholder={placeholder ?? "Chọn hồ sơ vi phạm"}
    onChange={(event) => onChange(event.target.value)}
    {...props}
  />
);

export const PerformanceReviewPicker = ({
  value,
  onChange,
  reviews,
  label = "Đánh giá hiệu suất",
  placeholder,
  ...props
}: BasePickerProps & {
  reviews: PersonnelChangePerformanceReviewOption[];
  label?: string;
}) => (
  <Select
    label={label}
    value={value}
    options={reviews.map((review) => ({
      value: review.id,
      label: [
        review.period,
        `${review.totalScore} điểm`,
        review.finalRating,
        review.status,
      ]
        .filter(Boolean)
        .join(" | "),
    }))}
    placeholder={placeholder ?? "Chọn đánh giá hiệu suất"}
    onChange={(event) => onChange(event.target.value)}
    {...props}
  />
);

export const ContractPicker = ({
  value,
  onChange,
  contracts,
  label = "Hợp đồng liên quan",
  placeholder,
  ...props
}: BasePickerProps & {
  contracts: PersonnelChangeContractOption[];
  label?: string;
}) => (
  <Select
    label={label}
    value={value}
    options={contracts.map((contract) => ({
      value: contract.id,
      label: [
        contract.contractNumber,
        contract.contractType,
        contract.status,
        formatDate(contract.startDate),
      ]
        .filter(Boolean)
        .join(" | "),
    }))}
    placeholder={placeholder ?? "Chọn hợp đồng"}
    onChange={(event) => onChange(event.target.value)}
    {...props}
  />
);

export const EvidenceFileUpload = ({
  value,
  onUploaded,
  label = "Tệp minh chứng",
  helperText = "Hỗ trợ PDF, DOC, DOCX hoặc hình ảnh, tối đa 10MB.",
}: {
  value?: string | null;
  onUploaded: (filePath: string) => void;
  label?: string;
  helperText?: string;
}) => {
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const upload = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setUploading(true);
    setError(null);
    try {
      const res = await personnelChangeApi.uploadEvidenceFile(file);
      onUploaded(res.data.filePath);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể tải tệp minh chứng.");
    } finally {
      setUploading(false);
      event.target.value = "";
    }
  };

  return (
    <div className="block">
      <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">
        {label}
      </span>
      <label className="flex min-h-[42px] cursor-pointer items-center justify-between gap-3 rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white px-3 py-2 text-sm transition hover:border-[var(--hicas-primary)]">
        <span className="min-w-0 flex-1 truncate text-[var(--hicas-text-secondary)]">
          {value || "Chọn tệp minh chứng"}
        </span>
        <span className="inline-flex items-center gap-1 rounded-[var(--radius-sm)] bg-[var(--hicas-primary)] px-3 py-1.5 text-xs font-semibold text-white">
          <Upload size={14} />
          {uploading ? "Đang tải..." : "Tải lên"}
        </span>
        <input className="hidden" type="file" onChange={upload} />
      </label>
      <span
        className={`mt-1 block text-xs ${
          error ? "text-[var(--hicas-danger)]" : "text-[var(--hicas-text-secondary)]"
        }`}
      >
        {error || helperText}
      </span>
    </div>
  );
};

const formatDate = (value?: string | null) => {
  if (!value) return null;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toLocaleDateString("vi-VN");
};
