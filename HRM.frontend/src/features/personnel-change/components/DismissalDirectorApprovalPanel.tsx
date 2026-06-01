import { useState, type FormEvent } from "react";
import { CheckCircle2, Play } from "lucide-react";
import { Button, Card, Select } from "../../../components/ui";
import type {
  DirectorApproveDismissalRequest,
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
  onApprove: (id: number, payload: DirectorApproveDismissalRequest) => Promise<boolean>;
  onExecute: (id: number, payload: ExecutePersonnelChangeRequest) => Promise<boolean>;
};

export const DismissalDirectorApprovalPanel = ({ request, saving, onApprove, onExecute }: Props) => {
  const [isApproved, setIsApproved] = useState("true");
  const [note, setNote] = useState("");
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

  const execute = async () => {
    if (!request || !canExecutePersonnelChange(request)) return;
    await onExecute(request.id, {
      completedAt: new Date().toISOString(),
      note: executeNote || null,
    });
  };

  return (
    <Card title="Director approval" description="Phe duyet sa thai va thuc thi sau khi contract termination hoan tat.">
      <form className="space-y-4" onSubmit={approve}>
        <Select
          label="Quyet dinh"
          value={isApproved}
          options={[
            { value: "true", label: "Phe duyet" },
            { value: "false", label: "Tu choi" },
          ]}
          onChange={(event) => setIsApproved(event.target.value)}
        />
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Director note</span>
          <textarea
            className="hicas-input min-h-24 resize-y"
            value={note}
            onChange={(event) => setNote(event.target.value)}
          />
        </label>
        <Button type="submit" iconLeft={<CheckCircle2 size={16} />} isLoading={saving} disabled={!request}>
          Gui phe duyet
        </Button>
      </form>
      <div className="mt-5 border-t border-[var(--hicas-border-soft)] pt-5">
        <label className="mb-3 block">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Ghi chu thuc thi</span>
          <textarea
            className="hicas-input min-h-20 resize-y"
            disabled={executeDisabled}
            value={executeNote}
            onChange={(event) => setExecuteNote(event.target.value)}
          />
        </label>
        {executeBlockedReason ? (
          <p className="mb-3 rounded-[var(--radius-md)] border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800">
            {executeBlockedReason}
          </p>
        ) : null}
        <Button iconLeft={<Play size={16} />} isLoading={saving} disabled={executeDisabled} onClick={execute}>
          Thuc thi sa thai
        </Button>
      </div>
    </Card>
  );
};
