import { useState, type FormEvent } from "react";
import { CheckCircle2, Play, XCircle } from "lucide-react";
import { Button, Card, Input, Select } from "../../../components/ui";
import type {
  DirectorApproveResignationRequest,
  ExecutePersonnelChangeRequest,
  PersonnelChangeDetail,
} from "../types/personnelChange";
import {
  canExecutePersonnelChange,
  getContractFlowExecutionBlockReason,
} from "../utils/contractFlow";

type Props = {
  request?: PersonnelChangeDetail | null;
  saving?: boolean;
  onApprove: (id: number, payload: DirectorApproveResignationRequest) => Promise<boolean>;
  onExecute: (id: number, payload: ExecutePersonnelChangeRequest) => Promise<boolean>;
};

export const ResignationDirectorApprovalPanel = ({
  request,
  saving,
  onApprove,
  onExecute,
}: Props) => {
  const [isApproved, setIsApproved] = useState("true");
  const [note, setNote] = useState("");
  const [completedAt, setCompletedAt] = useState("");
  const [executeNote, setExecuteNote] = useState("");
  const executeBlockedReason = getContractFlowExecutionBlockReason(request);
  const executeDisabled = !request || saving || !canExecutePersonnelChange(request);

  const approve = async (event: FormEvent) => {
    event.preventDefault();
    if (!request) return;
    await onApprove(request.id, {
      isApproved: isApproved === "true",
      note: note || null,
    });
  };

  const execute = async (event: FormEvent) => {
    event.preventDefault();
    if (!request || !canExecutePersonnelChange(request)) return;
    await onExecute(request.id, {
      completedAt: completedAt || null,
      note: executeNote || null,
    });
  };

  return (
    <Card title="Phê duyệt nghỉ việc" description="Phê duyệt hồ sơ nghỉ việc và thực hiện sau khi xử lý hợp đồng hoàn tất.">
      <form className="space-y-4" onSubmit={approve}>
        <Select
          label="Quyết định"
          value={isApproved}
          disabled={!request || saving}
          options={[
            { value: "true", label: "Phê duyệt" },
            { value: "false", label: "Từ chối" },
          ]}
          onChange={(event) => setIsApproved(event.target.value)}
        />
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Ghi chú phê duyệt</span>
          <textarea
            className="hicas-input min-h-20 resize-y"
            disabled={!request || saving}
            value={note}
            onChange={(event) => setNote(event.target.value)}
          />
        </label>
        <Button
          type="submit"
          variant={isApproved === "true" ? "primary" : "danger"}
          iconLeft={isApproved === "true" ? <CheckCircle2 size={16} /> : <XCircle size={16} />}
          isLoading={saving}
          disabled={!request}
        >
          Gửi phê duyệt
        </Button>
      </form>

      <form className="mt-5 grid gap-4 border-t border-[var(--hicas-border-soft)] pt-5 md:grid-cols-2" onSubmit={execute}>
        <Input
          label="Thời điểm hoàn tất"
          type="datetime-local"
          disabled={executeDisabled}
          value={completedAt}
          onChange={(event) => setCompletedAt(event.target.value)}
        />
        <Input
          label="Ghi chú thực hiện"
          disabled={executeDisabled}
          value={executeNote}
          onChange={(event) => setExecuteNote(event.target.value)}
        />
        {executeBlockedReason ? (
          <p className="rounded-[var(--radius-md)] border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800 md:col-span-2">
            {executeBlockedReason}
          </p>
        ) : null}
        <div className="md:col-span-2">
          <Button type="submit" iconLeft={<Play size={16} />} isLoading={saving} disabled={executeDisabled}>
            Thực hiện nghỉ việc
          </Button>
        </div>
      </form>
    </Card>
  );
};
