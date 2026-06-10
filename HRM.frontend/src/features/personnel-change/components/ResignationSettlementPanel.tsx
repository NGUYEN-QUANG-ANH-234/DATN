import type { ReactNode } from "react";
import { CalendarDays, FileText, LockKeyhole, WalletCards } from "lucide-react";
import { Card, StatusBadge } from "../../../components/ui";
import { formatDate, formatDateTime } from "../../../utils/formatters";
import { getPersonnelChangeStatusLabel, type PersonnelChangeDetail } from "../types/personnelChange";

type Props = {
  request?: PersonnelChangeDetail | null;
};

export const ResignationSettlementPanel = ({ request }: Props) => (
  <Card title="Quyết toán nghỉ việc" description="Theo dõi hợp đồng, quyết toán cuối cùng và khóa tài khoản.">
    <div className="space-y-4">
      <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-3">
        <p className="font-semibold text-[var(--hicas-text-main)]">
          {request ? `VT-${String(request.id).padStart(5, "0")}` : "Chưa chọn hồ sơ"}
        </p>
        <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">
          {request?.employeeName || "Mở một hồ sơ trong bảng để xem thông tin quyết toán."}
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
          label="Ngày làm việc cuối"
          value={formatDate(request?.effectiveDate)}
        />
        <InfoTile
          icon={<FileText size={17} />}
          label="Xử lý hợp đồng"
          value={request?.contractFlowStatus || "-"}
        />
        <InfoTile
          icon={<WalletCards size={17} />}
          label="Quyết toán cuối cùng"
          value={request?.relatedFinalSettlementId ? `FS-${request.relatedFinalSettlementId}` : "Chưa tạo"}
        />
        <InfoTile
          icon={<LockKeyhole size={17} />}
          label="Khóa tài khoản"
          value={request?.accountLockedAt ? formatDateTime(request.accountLockedAt) : "Chưa khóa"}
        />
      </div>

      <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-[var(--hicas-orange-lighter)] p-3 text-sm text-[var(--hicas-text-secondary)]">
        {request?.employeeConsentNote || request?.reason || "Ghi chú của nhân viên và lý do nghỉ việc sẽ hiển thị tại đây."}
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
