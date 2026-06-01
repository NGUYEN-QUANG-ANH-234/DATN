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
    <Card title="Contract flow" description="Luong hop dong duoc xu ly tai Module 3.">
      {!request ? (
        <p className="text-sm text-[var(--hicas-text-secondary)]">Chon mot ho so de xem contract flow.</p>
      ) : (
        <div className="space-y-4">
          {request.requiresContractFlow ? (
            <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-[var(--hicas-bg-soft)] p-3">
              <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <p className="text-sm font-semibold text-[var(--hicas-text-main)]">
                    Luong hop dong duoc xu ly tai Module 3
                  </p>
                  <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">
                    Module 7 chi luu link va chi execute sau khi flow duoc chap nhan/ky.
                  </p>
                </div>
                <StatusBadge
                  status={request.contractFlowStatus || "Pending"}
                  label={request.contractFlowStatus || "Pending"}
                />
              </div>
              {blockReason ? (
                <p className="mt-3 rounded-[var(--radius-md)] border border-amber-200 bg-amber-50 p-2 text-sm text-amber-800">
                  {blockReason}
                </p>
              ) : null}
            </div>
          ) : (
            <p className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-3 text-sm text-[var(--hicas-text-secondary)]">
              Ho so nay khong yeu cau contract flow.
            </p>
          )}

          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <InfoItem label="Yeu cau flow" value={request.requiresContractFlow ? "Co" : "Khong"} />
            <InfoItem label="Loai flow" value={getContractFlowTypeLabel(request.contractFlowType)} />
            <InfoItem label="Trang thai" value={request.contractFlowStatus || "-"} />
            <InfoItem
              label="Dang xu ly"
              value={isContractFlowInProgress(request) ? "Co" : "Khong"}
            />
            <InfoItem label="ContractId" value={primaryLink?.contractId ? `#${primaryLink.contractId}` : "-"} />
            <InfoItem
              label="ContractRequestId"
              value={primaryLink?.contractRequestId ? `#${primaryLink.contractRequestId}` : "-"}
            />
            <InfoItem
              label="AddendumId"
              value={primaryLink?.contractAddendumId ? `#${primaryLink.contractAddendumId}` : "-"}
            />
          </div>

          <div className="flex flex-wrap gap-2">
            {primaryLink?.contractId ? (
              <Module3Link to={`/employee-contract/hr-contracts?contractId=${primaryLink.contractId}`}>
                Mo hop dong
              </Module3Link>
            ) : null}
            {primaryLink?.contractRequestId ? (
              <Module3Link
                to={`/employee-contract/contract-requests?contractRequestId=${primaryLink.contractRequestId}`}
              >
                Mo request hop dong
              </Module3Link>
            ) : null}
            {primaryLink?.contractAddendumId ? (
              <Module3Link to={`/employee-contract/appendices?addendumId=${primaryLink.contractAddendumId}`}>
                Mo phu luc
              </Module3Link>
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
              Chua co link contract flow nao duoc tao.
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
          Contract #{link.contractId ?? "-"} / Request #{link.contractRequestId ?? "-"} / Addendum #
          {link.contractAddendumId ?? "-"}
        </p>
        <p className="mt-2 text-xs text-[var(--hicas-text-secondary)]">
          Created {formatDateTime(link.createdAt)}
          {link.completedAt ? ` / Completed ${formatDateTime(link.completedAt)}` : ""}
        </p>
      </div>
      <div className="flex flex-wrap items-center gap-2">
        <StatusBadge status={link.status || "Pending"} label={link.status || "Pending"} />
        {hasContractReference(link) ? <LinkButtons link={link} /> : null}
      </div>
    </div>
  </div>
);

const LinkButtons = ({ link }: { link: PersonnelChangeContractFlowLink }) => (
  <>
    {link.contractId ? (
      <Module3Link compact to={`/employee-contract/hr-contracts?contractId=${link.contractId}`}>
        Contract
      </Module3Link>
    ) : null}
    {link.contractRequestId ? (
      <Module3Link compact to={`/employee-contract/contract-requests?contractRequestId=${link.contractRequestId}`}>
        Request
      </Module3Link>
    ) : null}
    {link.contractAddendumId ? (
      <Module3Link compact to={`/employee-contract/appendices?addendumId=${link.contractAddendumId}`}>
        Addendum
      </Module3Link>
    ) : null}
  </>
);

const Module3Link = ({
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
  if (type === PersonnelChangeContractFlowType.NewContract) return "Hop dong moi";
  if (type === PersonnelChangeContractFlowType.ContractRenewal) return "Gia han hop dong";
  if (type === PersonnelChangeContractFlowType.ContractAddendum) return "Phu luc hop dong";
  if (type === PersonnelChangeContractFlowType.ContractTermination) return "Cham dut hop dong";
  return "None";
};
