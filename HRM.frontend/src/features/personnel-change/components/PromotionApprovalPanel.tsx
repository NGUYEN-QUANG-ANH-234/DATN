import { useState, type FormEvent } from "react";
import { CheckCircle2, Play, XCircle } from "lucide-react";
import { Button, Card, Input, Select } from "../../../components/ui";
import {
  PersonnelChangeContractFlowType,
  type ApprovePromotionRequest,
  type ExecutePersonnelChangeRequest,
  type PersonnelChangeContractOption,
  type PersonnelChangeDetail,
} from "../types/personnelChange";
import { useEmployeePersonnelChangeLookups } from "../hooks/usePersonnelChangeLookups";
import {
  canExecutePersonnelChange,
  getContractFlowExecutionBlockReason,
} from "../utils/contractFlow";
import { ContractPicker } from "./PersonnelChangePickers";

type Props = {
  request?: PersonnelChangeDetail | null;
  saving?: boolean;
  onHrReview: (id: number, payload: ApprovePromotionRequest) => Promise<boolean>;
  onDirectorApprove: (id: number, payload: ApprovePromotionRequest) => Promise<boolean>;
  onExecute: (id: number, payload: ExecutePersonnelChangeRequest) => Promise<boolean>;
};

export const PromotionApprovalPanel = ({
  request,
  saving,
  onHrReview,
  onDirectorApprove,
  onExecute,
}: Props) => {
  const related = useEmployeePersonnelChangeLookups(request?.employeeId);
  const [hrForm, setHrForm] = useState({
    isApproved: "true",
    note: "",
    requiresContractFlow: "keep",
    contractFlowType: String(PersonnelChangeContractFlowType.ContractAddendum),
    relatedContractId: "",
  });
  const [directorForm, setDirectorForm] = useState({
    isApproved: "true",
    note: "",
    requiresContractFlow: "keep",
    contractFlowType: String(PersonnelChangeContractFlowType.ContractAddendum),
    relatedContractId: "",
  });
  const [executeForm, setExecuteForm] = useState({
    completedAt: "",
    note: "",
  });

  const disabled = !request || saving;
  const executeBlockedReason = getContractFlowExecutionBlockReason(request);
  const executeDisabled = disabled || !canExecutePersonnelChange(request);

  const submitHrReview = async (event: FormEvent) => {
    event.preventDefault();
    if (!request) return;
    await onHrReview(request.id, toApprovalPayload(hrForm));
  };

  const submitDirectorReview = async (event: FormEvent) => {
    event.preventDefault();
    if (!request) return;
    await onDirectorApprove(request.id, toApprovalPayload(directorForm));
  };

  const submitExecute = async (event: FormEvent) => {
    event.preventDefault();
    if (!request || !canExecutePersonnelChange(request)) return;
    await onExecute(request.id, {
      completedAt: executeForm.completedAt || null,
      note: executeForm.note || null,
    });
  };

  return (
    <Card
      title="Phê duyệt và thực hiện"
      description="Rà soát hồ sơ trước khi cập nhật thay đổi vào thông tin nhân sự."
    >
      <div className="space-y-5">
        <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-3 text-sm">
          <div className="font-semibold text-[var(--hicas-text-main)]">
            {request ? `PC-${String(request.id).padStart(5, "0")}` : "Chưa chọn hồ sơ"}
          </div>
          <div className="mt-1 text-[var(--hicas-text-secondary)]">
            {request?.employeeName || "Mở một hồ sơ trong bảng để thao tác."}
          </div>
        </div>

        <form className="grid gap-3 md:grid-cols-2" onSubmit={submitHrReview}>
          <Select
            label="Quyết định HR"
            value={hrForm.isApproved}
            disabled={disabled}
            options={[
              { value: "true", label: "Đồng ý" },
              { value: "false", label: "Từ chối" },
            ]}
            onChange={(event) => setHrForm((prev) => ({ ...prev, isApproved: event.target.value }))}
          />
          <ContractFlowFields
            disabled={disabled}
            contracts={related.contracts}
            loadingContracts={related.loading}
            hasEmployee={Boolean(request?.employeeId)}
            value={hrForm}
            onChange={(next) => setHrForm((prev) => ({ ...prev, ...next }))}
          />
          <label className="block md:col-span-2">
            <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Ghi chú HR</span>
            <textarea
              className="hicas-input min-h-16 resize-y"
              disabled={disabled}
              value={hrForm.note}
              onChange={(event) => setHrForm((prev) => ({ ...prev, note: event.target.value }))}
            />
          </label>
          <div className="md:col-span-2">
            <Button
              type="submit"
              variant={hrForm.isApproved === "true" ? "primary" : "danger"}
              iconLeft={hrForm.isApproved === "true" ? <CheckCircle2 size={16} /> : <XCircle size={16} />}
              disabled={disabled}
              isLoading={saving}
            >
              Gửi kiểm tra HR
            </Button>
          </div>
        </form>

        <form className="grid gap-3 md:grid-cols-2" onSubmit={submitDirectorReview}>
          <Select
            label="Quyết định phê duyệt"
            value={directorForm.isApproved}
            disabled={disabled}
            options={[
              { value: "true", label: "Phê duyệt" },
              { value: "false", label: "Từ chối" },
            ]}
            onChange={(event) =>
              setDirectorForm((prev) => ({ ...prev, isApproved: event.target.value }))
            }
          />
          <ContractFlowFields
            disabled={disabled}
            contracts={related.contracts}
            loadingContracts={related.loading}
            hasEmployee={Boolean(request?.employeeId)}
            value={directorForm}
            onChange={(next) => setDirectorForm((prev) => ({ ...prev, ...next }))}
          />
          <label className="block md:col-span-2">
            <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">
              Ghi chú phê duyệt
            </span>
            <textarea
              className="hicas-input min-h-16 resize-y"
              disabled={disabled}
              value={directorForm.note}
              onChange={(event) => setDirectorForm((prev) => ({ ...prev, note: event.target.value }))}
            />
          </label>
          <div className="md:col-span-2">
            <Button
              type="submit"
              variant={directorForm.isApproved === "true" ? "primary" : "danger"}
              iconLeft={
                directorForm.isApproved === "true" ? <CheckCircle2 size={16} /> : <XCircle size={16} />
              }
              disabled={disabled}
              isLoading={saving}
            >
              Gửi phê duyệt
            </Button>
          </div>
        </form>

        <form className="grid gap-3 md:grid-cols-2" onSubmit={submitExecute}>
          <Input
            label="Thời điểm hoàn tất"
            type="datetime-local"
            disabled={executeDisabled}
            value={executeForm.completedAt}
            onChange={(event) => setExecuteForm((prev) => ({ ...prev, completedAt: event.target.value }))}
          />
          <Input
            label="Ghi chú thực hiện"
            disabled={executeDisabled}
            value={executeForm.note}
            onChange={(event) => setExecuteForm((prev) => ({ ...prev, note: event.target.value }))}
          />
          {executeBlockedReason ? (
            <p className="rounded-[var(--radius-md)] border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800 md:col-span-2">
              {executeBlockedReason}
            </p>
          ) : null}
          <div className="md:col-span-2">
            <Button type="submit" iconLeft={<Play size={16} />} disabled={executeDisabled} isLoading={saving}>
              Thực hiện
            </Button>
          </div>
        </form>
      </div>
    </Card>
  );
};

type ContractFormValue = {
  requiresContractFlow: string;
  contractFlowType: string;
  relatedContractId: string;
};

const ContractFlowFields = ({
  disabled,
  contracts,
  loadingContracts,
  hasEmployee,
  value,
  onChange,
}: {
  disabled?: boolean;
  contracts: PersonnelChangeContractOption[];
  loadingContracts?: boolean;
  hasEmployee: boolean;
  value: ContractFormValue;
  onChange: (next: Partial<ContractFormValue>) => void;
}) => (
  <>
    <Select
      label="Xử lý hợp đồng"
      value={value.requiresContractFlow}
      disabled={disabled}
      options={[
        { value: "keep", label: "Giữ nguyên" },
        { value: "true", label: "Cần xử lý" },
        { value: "false", label: "Không cần" },
      ]}
      onChange={(event) => onChange({ requiresContractFlow: event.target.value })}
    />
    <Select
      label="Loại xử lý hợp đồng"
      value={value.contractFlowType}
      disabled={disabled}
      options={[
        { value: String(PersonnelChangeContractFlowType.ContractAddendum), label: "Phụ lục hợp đồng" },
        { value: String(PersonnelChangeContractFlowType.ContractRenewal), label: "Gia hạn hợp đồng" },
        { value: String(PersonnelChangeContractFlowType.NewContract), label: "Hợp đồng mới" },
      ]}
      onChange={(event) => onChange({ contractFlowType: event.target.value })}
    />
    <ContractPicker
      label="Hợp đồng liên quan"
      value={value.relatedContractId}
      contracts={contracts}
      disabled={disabled || !hasEmployee}
      helperText={
        !hasEmployee
          ? "Hồ sơ chưa có nhân sự để tra cứu hợp đồng."
          : loadingContracts
            ? "Đang tải danh sách hợp đồng..."
            : undefined
      }
      onChange={(nextValue) => onChange({ relatedContractId: nextValue })}
    />
  </>
);

const toApprovalPayload = (
  form: {
    isApproved: string;
    note: string;
    requiresContractFlow: string;
    contractFlowType: string;
    relatedContractId: string;
  },
): ApprovePromotionRequest => ({
  isApproved: form.isApproved === "true",
  note: form.note || null,
  hrAssignedAccountId: null,
  requiresContractFlow:
    form.requiresContractFlow === "keep" ? null : form.requiresContractFlow === "true",
  contractFlowType: Number(form.contractFlowType) as PersonnelChangeContractFlowType,
  relatedContractId: toNumberOrNull(form.relatedContractId),
});

const toNumberOrNull = (value: string) => {
  const numericValue = Number(value);
  return Number.isInteger(numericValue) && numericValue > 0 ? numericValue : null;
};
