import { ExternalLink } from "lucide-react";
import { Link } from "react-router-dom";
import { Card, StatusBadge } from "../../../components/ui";
import { formatDateTime } from "../../../utils/formatters";
import {
  PersonnelChangeContractFlowType,
  type PersonnelChangeContractFlowLink,
  type PersonnelChangeDetail,
} from "../types/personnelChange";
import {
  getContractFlowExecutionBlockReason,
  getPrimaryContractLink,
  hasContractReference,
  isContractFlowInProgress,
} from "../utils/contractFlow";

type Props = {
  request?: PersonnelChangeDetail | null;
};

export const ContractFlowLinkPanel = ({ request }: Props) => {
  const links = request?.contractLinks ?? [];
  const primaryLink = getPrimaryContractLink(request);
  const blockReason = getContractFlowExecutionBlockReason(request);

  return (
    <Card title="Liên kết hợp đồng" description="Hợp đồng liên quan được xử lý tại Hồ sơ & hợp đồng.">
      {!request ? (
        <p className="text-sm text-[var(--hicas-text-secondary)]">Chọn một hồ sơ để xem liên kết hợp đồng.</p>
      ) : (
        <div className="space-y-4">
          {request.requiresContractFlow ? (
            <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-[var(--hicas-bg-soft)] p-3">
              <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <p className="text-sm font-semibold text-[var(--hicas-text-main)]">
                    Hợp đồng liên quan được xử lý tại Hồ sơ & hợp đồng
                  </p>
                  <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">
                    Hồ sơ biến động chỉ lưu liên kết và chỉ thực thi sau khi hợp đồng được chấp thuận hoặc ký.
                  </p>
                </div>
                <StatusBadge status={request.contractFlowStatus || "Pending"} />
              </div>
              {blockReason ? (
                <p className="mt-3 rounded-[var(--radius-md)] border border-amber-200 bg-amber-50 p-2 text-sm text-amber-800">
                  {blockReason}
                </p>
              ) : null}
            </div>
          ) : (
            <p className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-3 text-sm text-[var(--hicas-text-secondary)]">
              Hồ sơ này không yêu cầu xử lý hợp đồng.
            </p>
          )}

          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <InfoItem label="Cần hợp đồng" value={request.requiresContractFlow ? "Có" : "Không"} />
            <InfoItem label="Loại xử lý" value={getContractFlowTypeLabel(request.contractFlowType)} />
            <InfoItem label="Trạng thái" value={request.contractFlowStatus || "-"} />
            <InfoItem
              label="Đang xử lý"
              value={isContractFlowInProgress(request) ? "Có" : "Không"}
            />
            <InfoItem label="Mã hợp đồng" value={primaryLink?.contractId ? `#${primaryLink.contractId}` : "-"} />
            <InfoItem
              label="Mã yêu cầu hợp đồng"
              value={primaryLink?.contractRequestId ? `#${primaryLink.contractRequestId}` : "-"}
            />
            <InfoItem
              label="Mã phụ lục"
              value={primaryLink?.contractAddendumId ? `#${primaryLink.contractAddendumId}` : "-"}
            />
          </div>

          <div className="flex flex-wrap gap-2">
            {primaryLink?.contractId ? (
              <ContractLinkButton to={`/employee-contract/hr-contracts?contractId=${primaryLink.contractId}`}>
                Mở hợp đồng
              </ContractLinkButton>
            ) : null}
            {primaryLink?.contractRequestId ? (
              <ContractLinkButton
                to={`/employee-contract/contract-requests?contractRequestId=${primaryLink.contractRequestId}`}
              >
                Mở yêu cầu hợp đồng
              </ContractLinkButton>
            ) : null}
            {primaryLink?.contractAddendumId ? (
              <ContractLinkButton to={`/employee-contract/appendices?addendumId=${primaryLink.contractAddendumId}`}>
                Mở phụ lục
              </ContractLinkButton>
            ) : null}
          </div>

          {links.length > 0 ? (
            <div className="space-y-3">
              {links.map((link) => (
                <ContractLinkItem key={link.id} link={link} />
              ))}
            </div>
          ) : (
            <p className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-3 text-sm text-[var(--hicas-text-secondary)]">
              Chưa có liên kết hợp đồng nào.
            </p>
          )}
        </div>
      )}
    </Card>
  );
};

const ContractLinkItem = ({ link }: { link: PersonnelChangeContractFlowLink }) => (
  <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-3">
    <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
      <div>
        <p className="font-semibold text-[var(--hicas-text-main)]">
          {getContractFlowTypeLabel(link.contractFlowType)}
        </p>
        <p className="text-sm text-[var(--hicas-text-secondary)]">
          Hợp đồng #{link.contractId ?? "-"} / Yêu cầu #{link.contractRequestId ?? "-"} / Phụ lục #
          {link.contractAddendumId ?? "-"}
        </p>
        <p className="mt-2 text-xs text-[var(--hicas-text-secondary)]">
          Tạo lúc {formatDateTime(link.createdAt)}
          {link.completedAt ? ` / Hoàn tất ${formatDateTime(link.completedAt)}` : ""}
        </p>
      </div>
      <div className="flex flex-wrap items-center gap-2">
        <StatusBadge status={link.status || "Pending"} />
        {hasContractReference(link) ? <LinkButtons link={link} /> : null}
      </div>
    </div>
  </div>
);

const LinkButtons = ({ link }: { link: PersonnelChangeContractFlowLink }) => (
  <>
    {link.contractId ? (
      <ContractLinkButton compact to={`/employee-contract/hr-contracts?contractId=${link.contractId}`}>
        Hợp đồng
      </ContractLinkButton>
    ) : null}
    {link.contractRequestId ? (
      <ContractLinkButton compact to={`/employee-contract/contract-requests?contractRequestId=${link.contractRequestId}`}>
        Yêu cầu
      </ContractLinkButton>
    ) : null}
    {link.contractAddendumId ? (
      <ContractLinkButton compact to={`/employee-contract/appendices?addendumId=${link.contractAddendumId}`}>
        Phụ lục
      </ContractLinkButton>
    ) : null}
  </>
);

const ContractLinkButton = ({
  to,
  compact,
  children,
}: {
  to: string;
  compact?: boolean;
  children: string;
}) => (
  <Link
    to={to}
    className={`inline-flex items-center justify-center gap-2 rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white font-semibold text-[var(--hicas-text-main)] transition hover:border-[var(--hicas-primary)] hover:text-[var(--hicas-primary)] ${
      compact ? "min-h-9 px-3 text-xs" : "min-h-10 px-4 text-sm"
    }`}
  >
    <ExternalLink size={14} />
    {children}
  </Link>
);

const InfoItem = ({ label, value }: { label: string; value: string }) => (
  <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-3">
    <p className="text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">{label}</p>
    <p className="mt-1 text-sm font-semibold text-[var(--hicas-text-main)]">{value}</p>
  </div>
);

const getContractFlowTypeLabel = (type?: PersonnelChangeContractFlowType | null) => {
  if (type === PersonnelChangeContractFlowType.NewContract) return "Hợp đồng mới";
  if (type === PersonnelChangeContractFlowType.ContractRenewal) return "Gia hạn hợp đồng";
  if (type === PersonnelChangeContractFlowType.ContractAddendum) return "Phụ lục hợp đồng";
  if (type === PersonnelChangeContractFlowType.ContractTermination) return "Chấm dứt hợp đồng";
  return "Không áp dụng";
};
