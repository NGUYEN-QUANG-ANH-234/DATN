import { useState, type FormEvent } from "react";
import { FileCheck2, Play } from "lucide-react";
import { Button, Card, Input } from "../../../components/ui";
import type {
  ExecutePersonnelChangeRequest,
  IssueTransferDecisionRequest,
  PersonnelChangeDetail,
} from "../types/personnelChange";
import {
  canExecutePersonnelChange,
  getContractFlowExecutionBlockReason,
} from "../utils/contractFlow";

type Props = {
  request?: PersonnelChangeDetail | null;
  saving?: boolean;
  onIssue: (id: number, payload: IssueTransferDecisionRequest) => Promise<boolean>;
  onExecute: (id: number, payload: ExecutePersonnelChangeRequest) => Promise<boolean>;
};

export const IssueTransferDecisionPanel = ({ request, saving, onIssue, onExecute }: Props) => {
  const [decision, setDecision] = useState({
    decisionNumber: "",
    decisionFilePath: "",
    decisionIssuedAt: "",
    note: "",
  });
  const [executeNote, setExecuteNote] = useState("");
  const executeBlockedReason = getContractFlowExecutionBlockReason(request);
  const executeDisabled = !request || saving || !canExecutePersonnelChange(request);

  const issue = async (event: FormEvent) => {
    event.preventDefault();
    if (!request) return;
    await onIssue(request.id, {
      decisionNumber: decision.decisionNumber,
      decisionFilePath: decision.decisionFilePath || null,
      decisionIssuedAt: decision.decisionIssuedAt || null,
      note: decision.note || null,
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
    <Card title="Decision and execution" description="Ban hanh quyet dinh va thuc thi cap nhat ho so nhan su.">
      <form className="grid gap-4 md:grid-cols-2" onSubmit={issue}>
        <Input
          label="So quyet dinh"
          required
          value={decision.decisionNumber}
          onChange={(event) => setDecision((prev) => ({ ...prev, decisionNumber: event.target.value }))}
        />
        <Input
          label="Ngay ban hanh"
          type="date"
          value={decision.decisionIssuedAt}
          onChange={(event) => setDecision((prev) => ({ ...prev, decisionIssuedAt: event.target.value }))}
        />
        <Input
          label="File quyet dinh"
          className="md:col-span-2"
          value={decision.decisionFilePath}
          onChange={(event) => setDecision((prev) => ({ ...prev, decisionFilePath: event.target.value }))}
        />
        <label className="block md:col-span-2">
          <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">Ghi chu quyet dinh</span>
          <textarea
            className="hicas-input min-h-20 resize-y"
            value={decision.note}
            onChange={(event) => setDecision((prev) => ({ ...prev, note: event.target.value }))}
          />
        </label>
        <div className="md:col-span-2">
          <Button type="submit" iconLeft={<FileCheck2 size={16} />} isLoading={saving} disabled={!request}>
            Ban hanh quyet dinh
          </Button>
        </div>
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
          Thuc thi thuyen chuyen
        </Button>
      </div>
    </Card>
  );
};
