import type { ReactNode } from "react";
import { CalendarDays, FileText, LockKeyhole, WalletCards } from "lucide-react";
import { Card, StatusBadge } from "../../../components/ui";
import { formatDate, formatDateTime } from "../../../utils/formatters";
import { getPersonnelChangeStatusLabel, type PersonnelChangeDetail } from "../types/personnelChange";

type Props = {
  request?: PersonnelChangeDetail | null;
};

export const ResignationSettlementPanel = ({ request }: Props) => (
  <Card title="Settlement" description="Theo doi contract termination, final settlement va account lock.">
    <div className="space-y-4">
      <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-3">
        <p className="font-semibold text-[var(--hicas-text-main)]">
          {request ? `VT-${String(request.id).padStart(5, "0")}` : "Chua chon ho so"}
        </p>
        <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">
          {request?.employeeName || "Mo mot ho so trong bang de xem settlement."}
        </p>
        {request ? (
          <div className="mt-3">
            <StatusBadge
              status={getPersonnelChangeStatusLabel(request.status)}
              label={getPersonnelChangeStatusLabel(request.status)}
            />
          </div>
        ) : null}
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <InfoTile
          icon={<CalendarDays size={17} />}
          label="Ngay lam viec cuoi"
          value={formatDate(request?.effectiveDate)}
        />
        <InfoTile
          icon={<FileText size={17} />}
          label="Contract flow"
          value={request?.contractFlowStatus || "-"}
        />
        <InfoTile
          icon={<WalletCards size={17} />}
          label="Final settlement"
          value={request?.relatedFinalSettlementId ? `FS-${request.relatedFinalSettlementId}` : "Chua tao"}
        />
        <InfoTile
          icon={<LockKeyhole size={17} />}
          label="Account locked"
          value={request?.accountLockedAt ? formatDateTime(request.accountLockedAt) : "Chua khoa"}
        />
      </div>

      <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-[var(--hicas-orange-lighter)] p-3 text-sm text-[var(--hicas-text-secondary)]">
        {request?.employeeConsentNote || request?.reason || "Employee note va ly do nghi viec se hien thi tai day."}
      </div>
    </div>
  </Card>
);

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
