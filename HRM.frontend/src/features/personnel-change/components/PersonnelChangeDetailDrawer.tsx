import { DrawerForm } from "../../../components/ui";
import { formatDate, formatDateTime } from "../../../utils/formatters";
import type {
  PersonnelChangeDetail,
  PersonnelChangeRiskSummary as RiskSummary,
  PersonnelChangeTimelineItem,
} from "../types/personnelChange";
import { ContractFlowLinkPanel } from "./ContractFlowLinkPanel";
import { PersonnelChangeRiskSummary } from "./PersonnelChangeRiskSummary";
import { PersonnelChangeStatusBadge } from "./PersonnelChangeStatusBadge";
import { PersonnelChangeTimeline } from "./PersonnelChangeTimeline";

type Props = {
  open: boolean;
  request?: PersonnelChangeDetail | null;
  riskSummary?: RiskSummary | null;
  timeline?: PersonnelChangeTimelineItem[];
  onClose: () => void;
};

export const PersonnelChangeDetailDrawer = ({
  open,
  request,
  riskSummary,
  timeline = [],
  onClose,
}: Props) => (
  <DrawerForm
    open={open}
    title={request ? `Ho so PC-${String(request.id).padStart(5, "0")}` : "Chi tiet ho so"}
    description={request?.employeeName || "Thong tin chi tiet bien dong nhan su"}
    width="xl"
    onClose={onClose}
  >
    {!request ? (
      <p className="text-sm text-[var(--hicas-text-secondary)]">Chua co ho so duoc chon.</p>
    ) : (
      <div className="space-y-5">
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          <StatusInfoItem status={request.status} />
          <InfoItem label="Nhan su" value={request.employeeName || "-"} detail={request.employeeCode} />
          <InfoItem label="Ngay yeu cau" value={formatDateTime(request.requestedAt)} />
          <InfoItem label="Ngay hieu luc" value={formatDate(request.effectiveDate)} />
          <InfoItem
            label="Phong ban hien tai"
            value={request.currentDepartmentName || request.currentDepartmentId?.toString() || "-"}
          />
          <InfoItem
            label="Phong ban moi"
            value={request.newDepartmentName || request.newDepartmentId?.toString() || "-"}
          />
          <InfoItem
            label="Chuc danh hien tai"
            value={request.currentPositionName || request.currentPositionId?.toString() || "-"}
          />
          <InfoItem
            label="Chuc danh moi"
            value={request.newPositionName || request.newPositionId?.toString() || "-"}
          />
        </div>

        <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4">
          <p className="text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">Ly do / ghi chu</p>
          <p className="mt-2 text-sm text-[var(--hicas-text-main)]">{request.reason || "-"}</p>
          {request.hrNote ? <NoteLine label="HR" value={request.hrNote} /> : null}
          {request.managerNote ? <NoteLine label="Manager" value={request.managerNote} /> : null}
          {request.directorNote ? <NoteLine label="Director" value={request.directorNote} /> : null}
          {request.rejectedReason ? (
            <p className="mt-2 text-sm text-[var(--hicas-danger)]">Rejected: {request.rejectedReason}</p>
          ) : null}
        </div>

        <ContractFlowLinkPanel request={request} />
        <PersonnelChangeRiskSummary summary={riskSummary} />
        <PersonnelChangeTimeline items={timeline.length ? timeline : request.histories ?? []} />
      </div>
    )}
  </DrawerForm>
);

const StatusInfoItem = ({ status }: Pick<PersonnelChangeDetail, "status">) => (
  <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-3">
    <p className="text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">Trang thai</p>
    <div className="mt-2">
      <PersonnelChangeStatusBadge status={status} />
    </div>
  </div>
);

const InfoItem = ({
  label,
  value,
  detail,
}: {
  label: string;
  value: string;
  detail?: string | null;
}) => (
  <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-3">
    <p className="text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">{label}</p>
    <p className="mt-2 text-sm font-semibold text-[var(--hicas-text-main)]">{value}</p>
    {detail ? <p className="mt-1 text-xs text-[var(--hicas-text-secondary)]">{detail}</p> : null}
  </div>
);

const NoteLine = ({ label, value }: { label: string; value: string }) => (
  <p className="mt-2 text-sm text-[var(--hicas-text-secondary)]">
    {label}: {value}
  </p>
);
