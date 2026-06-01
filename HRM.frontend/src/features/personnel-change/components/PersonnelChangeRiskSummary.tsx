import { Card } from "../../../components/ui";
import type { PersonnelChangeRiskSummary as RiskSummary } from "../types/personnelChange";

type Props = {
  summary?: RiskSummary | null;
};

export const PersonnelChangeRiskSummary = ({ summary }: Props) => {
  const employee = summary?.employee;
  const contract = summary?.currentContract;
  const penalty = summary?.penaltySummary;

  return (
    <Card title="Risk summary" description="Du lieu tham chieu cho ho so bien dong.">
      {!summary ? (
        <p className="text-sm text-[var(--hicas-text-secondary)]">Chon mot ho so de xem risk summary.</p>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <SummaryItem label="Nhan su" value={employee?.fullName ?? "Chua chon"} detail={employee?.employeeCode} />
          <SummaryItem label="Phong ban" value={employee?.departmentName ?? "-"} detail={employee?.positionName} />
          <SummaryItem label="Hop dong" value={contract?.contractNumber ?? "-"} detail={contract?.status} />
          <SummaryItem
            label="Risk"
            value={`${penalty?.personnelImpactRecords ?? 0} ho so`}
            detail={`Penalty ${penalty?.totalPenaltyPoint ?? 0}`}
          />
        </div>
      )}
    </Card>
  );
};

const SummaryItem = ({ label, value, detail }: { label: string; value: string; detail?: string | null }) => (
  <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4">
    <p className="text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">{label}</p>
    <p className="mt-2 text-base font-semibold text-[var(--hicas-text-main)]">{value}</p>
    {detail && <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">{detail}</p>}
  </div>
);
