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
    <Card title="Điều kiện tham chiếu" description="Thông tin nền để HR và cấp phê duyệt đối chiếu trước khi xử lý.">
      <div className="space-y-4">
        <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-3">
          <div className="flex items-start gap-3">
            <BadgeCheck size={18} className="mt-0.5 text-[var(--hicas-orange)]" />
            <div>
              <p className="font-semibold text-[var(--hicas-text-main)]">
                {request?.employeeName || "Chưa chọn hồ sơ"}
              </p>
              <p className="text-sm text-[var(--hicas-text-secondary)]">
                {isConvertOfficial ? "Chuyển chính thức" : "Thăng tiến"} ·{" "}
                {request ? getPersonnelChangeStatusLabel(request.status) : "Chưa có trạng thái"}
              </p>
            </div>
          </div>
        </div>

        <div className="grid gap-3 sm:grid-cols-2">
          <InfoTile
            icon={<BriefcaseBusiness size={17} />}
            label="Vị trí hiện tại"
            value={request?.currentPositionName || request?.currentPositionId?.toString() || "-"}
          />
          <InfoTile
            icon={<BriefcaseBusiness size={17} />}
            label="Vị trí mới"
            value={request?.newPositionName || request?.newPositionId?.toString() || "-"}
          />
          <InfoTile
            icon={<LineChart size={17} />}
            label="Cấp bậc"
            value={`${request?.currentJobLevelName || request?.currentJobLevelId || "-"} -> ${
              request?.newJobLevelName || request?.newJobLevelId || "-"
            }`}
          />
          <InfoTile
            icon={<CalendarDays size={17} />}
            label="Ngày hiệu lực"
            value={formatDate(request?.effectiveDate)}
          />
        </div>

        <div className="grid gap-3 sm:grid-cols-3">
          <Metric label="Điểm KPI" value={summary?.latestPerformance?.totalScore?.toString() ?? "-"} />
          <Metric label="Hồ sơ vi phạm" value={summary?.penaltySummary?.totalRecords?.toString() ?? "0"} />
          <Metric label="Thâm niên" value={summary?.seniority?.totalMonths?.toString() ?? "-"} />
        </div>

        {request?.requiresContractFlow ? (
          <StatusBadge status={request.contractFlowStatus || "PendingContractFlow"} />
        ) : (
          <StatusBadge status="NoContractFlow" label="Không cần hợp đồng" />
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
