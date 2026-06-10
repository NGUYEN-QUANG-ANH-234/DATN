import type { ReactNode } from "react";
import { ExternalLink } from "lucide-react";
import { Card } from "../../../components/ui";
import { formatDate } from "../../../utils/formatters";
import type { PersonnelChangeDetail } from "../types/personnelChange";

type Props = {
  request?: PersonnelChangeDetail | null;
};

export const DismissalEvidencePanel = ({ request }: Props) => (
  <Card
    title="Hồ sơ chứng cứ"
    description="Tóm tắt vi phạm, minh chứng, hạn giải trình và phản hồi liên quan."
  >
    {!request ? (
      <p className="text-sm text-[var(--hicas-text-secondary)]">Chọn một hồ sơ để xem chứng cứ.</p>
    ) : (
      <div className="grid gap-4 md:grid-cols-2">
        <EvidenceItem
          label="Hồ sơ vi phạm"
          value={request.sourcePenaltyRecordId ? `Biên bản #${request.sourcePenaltyRecordId}` : "-"}
        />
        <EvidenceItem label="Tệp minh chứng" value={request.evidenceFilePath ? "Đã tải lên" : "-"}>
          {request.evidenceFilePath ? (
            <a
              href={request.evidenceFilePath}
              target="_blank"
              rel="noreferrer"
              className="mt-2 inline-flex items-center gap-1 text-sm font-semibold text-[var(--hicas-primary)] hover:underline"
            >
              Mở minh chứng
              <ExternalLink size={14} />
            </a>
          ) : null}
        </EvidenceItem>
        <EvidenceItem label="Đã thông báo lúc" value={formatDate(request.employeeNotifiedAt)} />
        <EvidenceItem label="Hạn giải trình" value={formatDate(request.responseDeadlineAt)} />
        <EvidenceItem label="Ghi chú HR" value={request.hrNote || "-"} />
        <EvidenceItem label="Ghi chú quản lý" value={request.managerNote || "-"} />
        <EvidenceItem label="Giải trình nhân viên" value={request.employeeExplanation || "-"} wide />
        <EvidenceItem
          label="Quyết toán cuối cùng"
          value={request.relatedFinalSettlementId ? `FS-${request.relatedFinalSettlementId}` : "-"}
        />
      </div>
    )}
  </Card>
);

const EvidenceItem = ({
  label,
  value,
  wide,
  children,
}: {
  label: string;
  value: string;
  wide?: boolean;
  children?: ReactNode;
}) => (
  <div
    className={`rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4 ${
      wide ? "md:col-span-2" : ""
    }`}
  >
    <p className="text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">{label}</p>
    <p className="mt-2 break-words text-sm font-medium text-[var(--hicas-text-main)]">{value}</p>
    {children}
  </div>
);
