import type { ReactNode } from "react";
import { BadgeCheck, BriefcaseBusiness, CalendarDays, LineChart } from "lucide-react";
import { Card, StatusBadge } from "../../../components/ui";
import { formatDate } from "../../../utils/formatters";
import {
  PersonnelChangeType,
  getPersonnelChangeStatusLabel,
  type PersonnelChangeDetail,
  type PersonnelChangeRiskSummary as RiskSummary,
} from "../types/personnelChange";

type Props = {
  request?: PersonnelChangeDetail | null;
  summary?: RiskSummary | null;
};

export const PromotionEligibilityPanel = ({ request, summary }: Props) => {
  const isConvertOfficial = request?.changeType === PersonnelChangeType.ConvertToOfficial;

  return (
    <Card title="Eligibility" description="Thong tin nen de HR va Director doi chieu truoc khi phe duyet.">
      <div className="space-y-4">
        <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-3">
          <div className="flex items-start gap-3">
            <BadgeCheck size={18} className="mt-0.5 text-[var(--hicas-orange)]" />
            <div>
              <p className="font-semibold text-[var(--hicas-text-main)]">
                {request?.employeeName || "Chua chon ho so"}
              </p>
              <p className="text-sm text-[var(--hicas-text-secondary)]">
                {isConvertOfficial ? "Convert to official" : "Promotion"} ·{" "}
                {request ? getPersonnelChangeStatusLabel(request.status) : "No status"}
              </p>
            </div>
          </div>
        </div>

        <div className="grid gap-3 sm:grid-cols-2">
          <InfoTile
            icon={<BriefcaseBusiness size={17} />}
            label="Vi tri hien tai"
            value={request?.currentPositionName || request?.currentPositionId?.toString() || "-"}
          />
          <InfoTile
            icon={<BriefcaseBusiness size={17} />}
            label="Vi tri moi"
            value={request?.newPositionName || request?.newPositionId?.toString() || "-"}
          />
          <InfoTile
            icon={<LineChart size={17} />}
            label="Job level"
            value={`${request?.currentJobLevelName || request?.currentJobLevelId || "-"} -> ${
              request?.newJobLevelName || request?.newJobLevelId || "-"
            }`}
          />
          <InfoTile
            icon={<CalendarDays size={17} />}
            label="Ngay hieu luc"
            value={formatDate(request?.effectiveDate)}
          />
        </div>

        <div className="grid gap-3 sm:grid-cols-3">
          <Metric label="KPI score" value={summary?.latestPerformance?.totalScore?.toString() ?? "-"} />
          <Metric label="Penalty records" value={summary?.penaltySummary?.totalRecords?.toString() ?? "0"} />
          <Metric label="Seniority months" value={summary?.seniority?.totalMonths?.toString() ?? "-"} />
        </div>

        {request?.requiresContractFlow ? (
          <StatusBadge status={request.contractFlowStatus || "PendingContractFlow"} />
        ) : (
          <StatusBadge status="NoContractFlow" label="No contract flow" />
        )}
      </div>
    </Card>
  );
};

const InfoTile = ({
  icon,
  label,
  value,
}: {
  icon: ReactNode;
  label: string;
  value: string;
}) => (
  <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-3">
    <div className="flex items-center gap-2 text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">
      {icon}
      <span>{label}</span>
    </div>
    <p className="mt-2 text-sm font-semibold text-[var(--hicas-text-main)]">{value}</p>
  </div>
);

const Metric = ({ label, value }: { label: string; value: string }) => (
  <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-[var(--hicas-orange-lighter)] p-3">
    <p className="text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">{label}</p>
    <p className="mt-1 text-xl font-bold text-[var(--hicas-text-main)]">{value}</p>
  </div>
);
