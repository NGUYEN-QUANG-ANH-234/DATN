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
    title={request ? `Hồ sơ PC-${String(request.id).padStart(5, "0")}` : "Chi tiết hồ sơ"}
    description={request?.employeeName || "Thông tin chi tiết biến động nhân sự"}
    width="xl"
    onClose={onClose}
  >
    {!request ? (
      <p className="text-sm text-[var(--hicas-text-secondary)]">Chưa có hồ sơ được chọn.</p>
    ) : (
      <div className="space-y-5">
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          <StatusInfoItem status={request.status} />
          <InfoItem label="Nhân sự" value={request.employeeName || "-"} detail={request.employeeCode} />
          <InfoItem label="Ngày yêu cầu" value={formatDateTime(request.requestedAt)} />
          <InfoItem label="Ngày hiệu lực" value={formatDate(request.effectiveDate)} />
          <InfoItem
            label="Phòng ban hiện tại"
            value={request.currentDepartmentName || request.currentDepartmentId?.toString() || "-"}
          />
          <InfoItem
            label="Phòng ban mới"
            value={request.newDepartmentName || request.newDepartmentId?.toString() || "-"}
          />
          <InfoItem
            label="Chức danh hiện tại"
            value={request.currentPositionName || request.currentPositionId?.toString() || "-"}
          />
          <InfoItem
            label="Chức danh mới"
            value={request.newPositionName || request.newPositionId?.toString() || "-"}
          />
        </div>

        <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4">
          <p className="text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">Lý do / ghi chú</p>
          <p className="mt-2 text-sm text-[var(--hicas-text-main)]">{request.reason || "-"}</p>
          {request.hrNote ? <NoteLine label="HR" value={request.hrNote} /> : null}
          {request.managerNote ? <NoteLine label="Quản lý" value={request.managerNote} /> : null}
          {request.directorNote ? <NoteLine label="Phê duyệt" value={request.directorNote} /> : null}
          {request.rejectedReason ? (
            <p className="mt-2 text-sm text-[var(--hicas-danger)]">Từ chối: {request.rejectedReason}</p>
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
    <p className="text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">Trạng thái</p>
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
