import React, { useCallback, useEffect, useMemo, useState } from "react";
import {
  CalendarDays,
  Download,
  Eye,
  FileText,
  History,
  RefreshCw,
  X,
} from "lucide-react";
import { PageHeader } from "../../../components/layout";
import { Badge, Button, Card, EmptyState, LoadingState } from "../../../components/ui";
import type { BadgeVariant } from "../../../components/ui";
import { useNotification } from "../../../core/context/NotificationContext";
import {
  contractApi,
  type ContractDocumentPreviewDto,
  type ContractDto,
} from "../api/contractApi";

const CONTRACT_TYPE_LABELS: Record<string, string> = {
  Probation: "Hợp đồng thử việc",
  FixedTerm: "Hợp đồng xác định thời hạn",
  Definite: "Hợp đồng xác định thời hạn",
  Indefinite: "Hợp đồng không xác định thời hạn",
  PartTime: "Hợp đồng bán thời gian",
};

const STATUS_LABELS: Record<string, string> = {
  PendingDept: "Chờ trưởng phòng",
  PendingHR: "Chờ HR soạn thảo",
  PendingManagerContentReview: "Chờ trưởng phòng duyệt nội dung",
  PendingEmployee: "Chờ người lao động xác nhận",
  PendingHRRevision: "Chờ HR chỉnh sửa",
  Draft: "Chờ người lao động xác nhận",
  Negotiating: "Đang thương lượng",
  PendingDirector: "Chờ giám đốc duyệt",
  ApprovedByDirector: "Đã duyệt, chờ phát hành",
  Active: "Đang hiệu lực",
  Expired: "Hết hạn",
  Rejected: "Từ chối",
  Draft_Cancelled: "Bản nháp đã hủy",
};

const PROCESS_STATUSES = new Set([
  "PendingDept",
  "PendingHR",
  "PendingManagerContentReview",
  "PendingEmployee",
  "PendingHRRevision",
  "Draft",
  "Negotiating",
  "PendingDirector",
  "ApprovedByDirector",
]);

const EMPLOYEE_VISIBLE_NOTE_STATUSES = new Set(["Draft", "PendingEmployee", "PendingHRRevision", "Negotiating"]);

const statusVariant = (status?: string): BadgeVariant => {
  switch (status) {
    case "Active":
      return "success";
    case "ApprovedByDirector":
    case "PendingDirector":
      return "info";
    case "Draft":
    case "PendingEmployee":
    case "PendingDept":
    case "PendingHR":
    case "PendingManagerContentReview":
    case "PendingHRRevision":
    case "Negotiating":
      return "warning";
    case "Rejected":
    case "Draft_Cancelled":
      return "danger";
    default:
      return "neutral";
  }
};

const formatCurrency = (value?: number | null) =>
  new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(value || 0);

const formatDate = (value?: string | null) =>
  value ? new Date(value).toLocaleDateString("vi-VN") : "Chưa cập nhật";

const fallback = (value?: string | null) => value || "Chưa cập nhật";

const unwrapData = <T,>(response: unknown): T => {
  const raw = response as { data?: T; Data?: T };
  return (raw.data ?? raw.Data ?? []) as T;
};

const sortByStartDateDesc = (items: ContractDto[]) =>
  [...items].sort((a, b) => {
    const left = a.startDate ? new Date(a.startDate).getTime() : 0;
    const right = b.startDate ? new Date(b.startDate).getTime() : 0;
    return right - left || b.id - a.id;
  });

const getContractTitle = (contract: ContractDto) =>
  contract.legalDocumentNumber || contract.contractNumber || `Hợp đồng #${contract.id}`;

const saveBlob = (blob: Blob, fileName: string) => {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
};

type SummaryItemProps = {
  label: string;
  value: React.ReactNode;
};

const SummaryItem = ({ label, value }: SummaryItemProps) => (
  <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border-soft)] bg-[var(--hicas-bg)] px-4 py-3">
    <p className="text-xs font-medium text-[var(--hicas-text-secondary)]">{label}</p>
    <p className="mt-1 break-words text-sm font-semibold text-[var(--hicas-text-main)]">
      {value}
    </p>
  </div>
);

export const MyContracts: React.FC = () => {
  const [contracts, setContracts] = useState<ContractDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [preview, setPreview] = useState<ContractDocumentPreviewDto | null>(null);
  const [previewLoadingId, setPreviewLoadingId] = useState<number | null>(null);
  const [downloadLoadingKey, setDownloadLoadingKey] = useState<string | null>(null);
  const { triggerAlert } = useNotification();

  const fetchContracts = useCallback(async () => {
    setLoading(true);
    try {
      const response = await contractApi.getMyContracts();
      setContracts(unwrapData<ContractDto[]>(response));
    } catch (error) {
      const message = error instanceof Error ? error.message : "Vui lòng thử lại.";
      triggerAlert("error", "Không thể tải hợp đồng", message);
    } finally {
      setLoading(false);
    }
  }, [triggerAlert]);

  useEffect(() => {
    fetchContracts();
  }, [fetchContracts]);

  const { currentContracts, processContracts, archivedCount } = useMemo(() => {
    const current = sortByStartDateDesc(
      contracts.filter((contract) => contract.status === "Active"),
    );
    const inProcess = sortByStartDateDesc(
      contracts.filter(
        (contract) => contract.status !== "Active" && PROCESS_STATUSES.has(contract.status),
      ),
    );

    return {
      currentContracts: current,
      processContracts: inProcess,
      archivedCount: contracts.filter(
        (contract) => contract.status !== "Active" && !PROCESS_STATUSES.has(contract.status),
      ).length,
    };
  }, [contracts]);

  const handlePreview = async (contract: ContractDto) => {
    setPreviewLoadingId(contract.id);
    try {
      const response = await contractApi.previewDocument(contract.id);
      setPreview(unwrapData<ContractDocumentPreviewDto>(response));
    } catch (error) {
      const message =
        error instanceof Error
          ? error.message
          : "Hợp đồng chưa có đủ dữ liệu pháp lý để xem trước.";
      triggerAlert("error", "Không thể xem trước hợp đồng", message);
    } finally {
      setPreviewLoadingId(null);
    }
  };

  const handleDownload = async (contract: ContractDto, type: "doc" | "pdf") => {
    const key = `${contract.id}-${type}`;
    setDownloadLoadingKey(key);
    try {
      const blob =
        type === "doc"
          ? await contractApi.downloadDocumentDoc(contract.id)
          : await contractApi.downloadDocumentPdf(contract.id);
      saveBlob(blob, `${getContractTitle(contract)}.${type}`);
    } catch (error) {
      const message =
        error instanceof Error
          ? error.message
          : "Văn bản hợp đồng chưa sẵn sàng để tải xuống.";
      triggerAlert("error", "Không thể tải văn bản", message);
    } finally {
      setDownloadLoadingKey(null);
    }
  };

  const renderDocumentActions = (contract: ContractDto) => {
    const canDownloadPdf = Boolean(contract.documentPdfFilePath);

    return (
      <div className="flex flex-wrap gap-2">
        <Button
          variant="secondary"
          iconLeft={<Eye size={16} />}
          isLoading={previewLoadingId === contract.id}
          onClick={() => handlePreview(contract)}
        >
          Xem trước
        </Button>
        <Button
          variant="secondary"
          iconLeft={<Download size={16} />}
          isLoading={downloadLoadingKey === `${contract.id}-doc`}
          onClick={() => handleDownload(contract, "doc")}
        >
          Tải DOC
        </Button>
        <Button
          variant="secondary"
          iconLeft={<Download size={16} />}
          isLoading={downloadLoadingKey === `${contract.id}-pdf`}
          disabled={!canDownloadPdf}
          onClick={() => handleDownload(contract, "pdf")}
        >
          Tải PDF
        </Button>
      </div>
    );
  };

  if (loading) {
    return <LoadingState title="Đang tải hợp đồng của bạn..." />;
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Hợp đồng của tôi"
        description="Theo dõi hợp đồng đang hiệu lực và tải văn bản khi cần."
        breadcrumb={[
          { label: "Hồ sơ & hợp đồng" },
          { label: "Hợp đồng của tôi" },
        ]}
        actions={
          <div className="flex flex-wrap gap-2">
            <Button
              variant="secondary"
              iconLeft={<History size={16} />}
              onClick={() => window.location.assign("/employee-contract/history?type=CONTRACT")}
            >
              Lịch sử hợp đồng
            </Button>
            <Button
              variant="secondary"
              iconLeft={<RefreshCw size={16} />}
              onClick={fetchContracts}
              disabled={loading}
            >
              Làm mới
            </Button>
          </div>
        }
      />

      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <p className="text-sm text-[var(--hicas-text-secondary)]">Đang hiệu lực</p>
          <p className="mt-2 text-3xl font-bold text-green-600">
            {currentContracts.length}
          </p>
        </Card>
        <Card>
          <p className="text-sm text-[var(--hicas-text-secondary)]">Đang trong quy trình</p>
          <p className="mt-2 text-3xl font-bold text-[var(--hicas-orange-dark)]">
            {processContracts.length}
          </p>
        </Card>
        <Card>
          <p className="text-sm text-[var(--hicas-text-secondary)]">Theo dõi tại lịch sử</p>
          <p className="mt-2 text-3xl font-bold text-[var(--hicas-text-main)]">
            {archivedCount}
          </p>
        </Card>
      </div>

      {processContracts.length > 0 && (
        <Card
          title="Đang chờ xử lý"
          description="Các hợp đồng đang được xác nhận, thương lượng hoặc phát hành."
          actions={<CalendarDays size={20} className="text-[var(--hicas-orange)]" />}
        >
          <div className="space-y-3">
            {processContracts.map((contract) => {
              const shouldShowNegotiationNote =
                EMPLOYEE_VISIBLE_NOTE_STATUSES.has(contract.status) &&
                Boolean(contract.negotiationNote);

              return (
                <div
                  key={contract.id}
                  className="rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-white p-4"
                >
                  <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
                    <div>
                      <div className="flex flex-wrap gap-2">
                        <Badge variant={statusVariant(contract.status)}>
                          {STATUS_LABELS[contract.status] ?? contract.status}
                        </Badge>
                        <Badge variant="neutral">
                          {CONTRACT_TYPE_LABELS[contract.contractType] ?? contract.contractType}
                        </Badge>
                      </div>
                      <h3 className="mt-2 text-base font-semibold text-[var(--hicas-text-main)]">
                        {getContractTitle(contract)}
                      </h3>
                      <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">
                        Hiệu lực dự kiến: {formatDate(contract.startDate)}
                        {contract.endDate ? ` - ${formatDate(contract.endDate)}` : ""}
                      </p>
                    </div>
                    {renderDocumentActions(contract)}
                  </div>

                  {shouldShowNegotiationNote && (
                    <div className="mt-3 rounded-[var(--radius-md)] border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-800">
                      <span className="font-semibold">Ghi chú thương lượng: </span>
                      {contract.negotiationNote}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </Card>
      )}

      <Card
        title="Hợp đồng đang hiệu lực"
        description="Chỉ hiển thị thông tin chính của hợp đồng hiện tại. Nội dung đầy đủ nằm trong văn bản hợp đồng."
        actions={<FileText size={20} className="text-[var(--hicas-orange)]" />}
      >
        {currentContracts.length === 0 ? (
          <EmptyState
            title="Chưa có hợp đồng đang hiệu lực"
            description="Khi hợp đồng được phát hành và có hiệu lực, thông tin chính sẽ hiển thị tại đây."
          />
        ) : (
          <div className="space-y-4">
            {currentContracts.map((contract) => (
              <div
                key={contract.id}
                className="rounded-[var(--radius-xl)] border border-[var(--hicas-border)] bg-white p-5"
              >
                <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                  <div>
                    <div className="flex flex-wrap gap-2">
                      <Badge variant="success">Đang hiệu lực</Badge>
                      <Badge variant="neutral">
                        {CONTRACT_TYPE_LABELS[contract.contractType] ?? contract.contractType}
                      </Badge>
                    </div>
                    <h2 className="mt-3 text-xl font-bold text-[var(--hicas-text-main)]">
                      {getContractTitle(contract)}
                    </h2>
                    <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">
                      Phiên bản {contract.version || 1}
                    </p>
                  </div>
                  {renderDocumentActions(contract)}
                </div>

                <div className="mt-5 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                  <SummaryItem
                    label="Thời hạn"
                    value={`${formatDate(contract.startDate)} - ${
                      contract.endDate ? formatDate(contract.endDate) : "Không xác định"
                    }`}
                  />
                  <SummaryItem
                    label="Chức danh"
                    value={fallback(contract.jobTitle || contract.employeePositionSnapshot)}
                  />
                  <SummaryItem
                    label="Phòng ban"
                    value={fallback(contract.employeeDepartmentSnapshot)}
                  />
                  <SummaryItem
                    label="Địa điểm làm việc"
                    value={fallback(contract.workLocation)}
                  />
                  <SummaryItem
                    label="Lương cơ bản"
                    value={formatCurrency(contract.basicSalary)}
                  />
                  <SummaryItem
                    label="Lương đóng BHXH"
                    value={formatCurrency(contract.insuranceSalary)}
                  />
                  <SummaryItem
                    label="Ngày trả lương"
                    value={fallback(contract.salaryPaymentDate)}
                  />
                  <SummaryItem
                    label="Ngày phát hành"
                    value={formatDate(contract.issuedAt)}
                  />
                </div>
              </div>
            ))}
          </div>
        )}
      </Card>

      {preview && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/45 px-4 py-6">
          <div className="flex max-h-full w-full max-w-5xl flex-col rounded-[var(--radius-xl)] bg-white shadow-2xl">
            <div className="flex items-start justify-between gap-4 border-b border-[var(--hicas-border-soft)] px-5 py-4">
              <div>
                <div className="flex items-center gap-2 text-sm font-semibold text-[var(--hicas-orange-dark)]">
                  <FileText size={16} />
                  Xem trước hợp đồng
                </div>
                <h3 className="mt-1 text-lg font-bold text-[var(--hicas-text-main)]">
                  {preview.documentNumber}
                </h3>
              </div>
              <Button
                variant="ghost"
                iconLeft={<X size={18} />}
                onClick={() => setPreview(null)}
                aria-label="Đóng xem trước"
              />
            </div>
            <div className="overflow-auto bg-[var(--hicas-bg)] p-4">
              <div
                className="mx-auto rounded-[var(--radius-md)] bg-white shadow-sm"
                dangerouslySetInnerHTML={{ __html: preview.html }}
              />
            </div>
            <div className="flex flex-wrap justify-end gap-2 border-t border-[var(--hicas-border-soft)] px-5 py-4">
              <Button variant="secondary" onClick={() => setPreview(null)}>
                Đóng
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
