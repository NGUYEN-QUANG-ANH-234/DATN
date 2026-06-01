import { Card } from "../../../components/ui";
import { formatDate } from "../../../utils/formatters";
import type { PersonnelChangeDetail } from "../types/personnelChange";

type Props = {
  request?: PersonnelChangeDetail | null;
};

export const DismissalEvidencePanel = ({ request }: Props) => (
  <Card title="Dismissal evidence" description="Tom tat penalty, evidence, deadline va giai trinh lien quan.">
    {!request ? (
      <p className="text-sm text-[var(--hicas-text-secondary)]">Chon mot ho so de xem evidence.</p>
    ) : (
      <div className="grid gap-4 md:grid-cols-2">
        <EvidenceItem label="Penalty record" value={request.sourcePenaltyRecordId ? String(request.sourcePenaltyRecordId) : "-"} />
        <EvidenceItem label="Evidence file" value={request.evidenceFilePath || "-"} />
        <EvidenceItem label="Notified at" value={formatDate(request.employeeNotifiedAt)} />
        <EvidenceItem label="Response deadline" value={formatDate(request.responseDeadlineAt)} />
        <EvidenceItem label="HR note" value={request.hrNote || "-"} />
        <EvidenceItem label="Manager note" value={request.managerNote || "-"} />
        <EvidenceItem label="Employee explanation" value={request.employeeExplanation || "-"} wide />
        <EvidenceItem
          label="Final settlement"
          value={request.relatedFinalSettlementId ? `FS-${request.relatedFinalSettlementId}` : "-"}
        />
      </div>
    )}
  </Card>
);

const EvidenceItem = ({ label, value, wide }: { label: string; value: string; wide?: boolean }) => (
  <div className={`rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4 ${wide ? "md:col-span-2" : ""}`}>
    <p className="text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">{label}</p>
    <p className="mt-2 break-words text-sm font-medium text-[var(--hicas-text-main)]">{value}</p>
  </div>
);
