import { useState, type FormEvent } from "react";
import { CheckCircle2, Play, XCircle } from "lucide-react";
import { Button, Card, Input, Select } from "../../../components/ui";
import {
  PersonnelChangeContractFlowType,
  type ApprovePromotionRequest,
  type ExecutePersonnelChangeRequest,
  type PersonnelChangeDetail,
} from "../types/personnelChange";
import {
  canExecutePersonnelChange,
  getContractFlowExecutionBlockReason,
} from "../utils/contractFlow";

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
  const [hrForm, setHrForm] = useState({
    isApproved: "true",
    note: "",
    hrAssignedAccountId: "",
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
    await onHrReview(request.id, toApprovalPayload(hrForm, true));
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
    <Card title="Approval and execute" description="Xu ly HR review, Director approval va execute cho F7.1.">
      <div className="space-y-5">
        <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-3 text-sm">
          <div className="font-semibold text-[var(--hicas-text-main)]">
            {request ? `PC-${String(request.id).padStart(5, "0")}` : "Chua chon ho so"}
          </div>
          <div className="mt-1 text-[var(--hicas-text-secondary)]">
            {request?.employeeName || "Mo mot ho so trong bang de thao tac."}
          </div>
        </div>

        <form className="grid gap-3 md:grid-cols-2" onSubmit={submitHrReview}>
          <Select
            label="HR decision"
            value={hrForm.isApproved}
            disabled={disabled}
            options={[
              { value: "true", label: "Approve" },
              { value: "false", label: "Reject" },
            ]}
            onChange={(event) => setHrForm((prev) => ({ ...prev, isApproved: event.target.value }))}
          />
          <Input
            label="HR assigned"
            type="number"
            min={1}
            disabled={disabled}
            value={hrForm.hrAssignedAccountId}
            onChange={(event) =>
              setHrForm((prev) => ({ ...prev, hrAssignedAccountId: event.target.value }))
            }
          />
          <ContractFlowFields
            disabled={disabled}
            value={hrForm}
            onChange={(next) => setHrForm((prev) => ({ ...prev, ...next }))}
          />
          <label className="block md:col-span-2">
            <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">HR note</span>
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
              HR review
            </Button>
          </div>
        </form>

        <form className="grid gap-3 md:grid-cols-2" onSubmit={submitDirectorReview}>
          <Select
            label="Director decision"
            value={directorForm.isApproved}
            disabled={disabled}
            options={[
              { value: "true", label: "Approve" },
              { value: "false", label: "Reject" },
            ]}
            onChange={(event) =>
              setDirectorForm((prev) => ({ ...prev, isApproved: event.target.value }))
            }
          />
          <ContractFlowFields
            disabled={disabled}
            value={directorForm}
            onChange={(next) => setDirectorForm((prev) => ({ ...prev, ...next }))}
          />
          <label className="block md:col-span-2">
            <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">
              Director note
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
              Director approve
            </Button>
          </div>
        </form>

        <form className="grid gap-3 md:grid-cols-2" onSubmit={submitExecute}>
          <Input
            label="Completed at"
            type="datetime-local"
            disabled={executeDisabled}
            value={executeForm.completedAt}
            onChange={(event) => setExecuteForm((prev) => ({ ...prev, completedAt: event.target.value }))}
          />
          <Input
            label="Execute note"
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
              Execute
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
  value,
  onChange,
}: {
  disabled?: boolean;
  value: ContractFormValue;
  onChange: (next: Partial<ContractFormValue>) => void;
}) => (
  <>
    <Select
      label="Contract flow"
      value={value.requiresContractFlow}
      disabled={disabled}
      options={[
        { value: "keep", label: "Giu nguyen" },
        { value: "true", label: "Can" },
        { value: "false", label: "Khong can" },
      ]}
      onChange={(event) => onChange({ requiresContractFlow: event.target.value })}
    />
    <Select
      label="Loai contract flow"
      value={value.contractFlowType}
      disabled={disabled}
      options={[
        { value: String(PersonnelChangeContractFlowType.ContractAddendum), label: "Phu luc hop dong" },
        { value: String(PersonnelChangeContractFlowType.ContractRenewal), label: "Gia han hop dong" },
        { value: String(PersonnelChangeContractFlowType.NewContract), label: "Hop dong moi" },
      ]}
      onChange={(event) => onChange({ contractFlowType: event.target.value })}
    />
    <Input
      label="Hop dong lien quan"
      type="number"
      min={1}
      disabled={disabled}
      value={value.relatedContractId}
      onChange={(event) => onChange({ relatedContractId: event.target.value })}
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
    hrAssignedAccountId?: string;
  },
  includeHrAssigned = false,
): ApprovePromotionRequest => ({
  isApproved: form.isApproved === "true",
  note: form.note || null,
  hrAssignedAccountId: includeHrAssigned ? toNumberOrNull(form.hrAssignedAccountId || "") : null,
  requiresContractFlow:
    form.requiresContractFlow === "keep" ? null : form.requiresContractFlow === "true",
  contractFlowType: Number(form.contractFlowType) as PersonnelChangeContractFlowType,
  relatedContractId: toNumberOrNull(form.relatedContractId),
});

const toNumberOrNull = (value: string) => {
  const numericValue = Number(value);
  return Number.isInteger(numericValue) && numericValue > 0 ? numericValue : null;
};
