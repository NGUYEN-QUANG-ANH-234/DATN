import { useEffect, useMemo, useState } from "react";
import { Button, Card, DataTable, type DataTableColumn } from "../../../components/ui";
import { formatDate } from "../../../utils/formatters";
import { personnelChangeApi } from "../api/personnelChangeApi";
import {
  PersonnelChangeStatus,
  PersonnelChangeType,
  type PersonnelChangeDetail,
  type PersonnelChangeListItem,
  type PersonnelChangeRiskSummary,
  type PersonnelChangeTimelineItem,
} from "../types/personnelChange";
import { PersonnelChangeDetailDrawer } from "./PersonnelChangeDetailDrawer";
import { PersonnelChangeStatusBadge } from "./PersonnelChangeStatusBadge";

const changeTypeOptions = [
  { value: "all", label: "Tất cả loại hồ sơ" },
  { value: String(PersonnelChangeType.Promotion), label: "Thăng tiến" },
  { value: String(PersonnelChangeType.ConvertToOfficial), label: "Chuyển chính thức" },
  { value: String(PersonnelChangeType.SeniorAppointment), label: "Bổ nhiệm cấp cao" },
  { value: String(PersonnelChangeType.VoluntaryTermination), label: "Nghỉ việc chủ động" },
  { value: String(PersonnelChangeType.Dismissal), label: "Kỷ luật và sa thải" },
  { value: String(PersonnelChangeType.InternalTransfer), label: "Thuyên chuyển nội bộ" },
];

const statusOptions = [
  { value: "all", label: "Tất cả trạng thái" },
  { value: String(PersonnelChangeStatus.PendingHRReview), label: "Chờ HR xử lý" },
  { value: String(PersonnelChangeStatus.PendingManagerReview), label: "Chờ quản lý duyệt" },
  { value: String(PersonnelChangeStatus.PendingCurrentManagerOpinion), label: "Chờ ý kiến quản lý" },
  { value: String(PersonnelChangeStatus.PendingEmployeeConsent), label: "Chờ nhân viên xác nhận" },
  { value: String(PersonnelChangeStatus.PendingEmployeeExplanation), label: "Chờ giải trình" },
  { value: String(PersonnelChangeStatus.PendingDirectorApproval), label: "Chờ giám đốc duyệt" },
  { value: String(PersonnelChangeStatus.PendingContractFlow), label: "Chờ xử lý hợp đồng" },
  { value: String(PersonnelChangeStatus.ReadyToExecute), label: "Sẵn sàng thực thi" },
  { value: String(PersonnelChangeStatus.Completed), label: "Hoàn tất" },
  { value: String(PersonnelChangeStatus.Rejected), label: "Từ chối" },
  { value: String(PersonnelChangeStatus.Cancelled), label: "Đã hủy" },
  { value: String(PersonnelChangeStatus.Escalated), label: "Quá hạn xử lý" },
];

const finalStatuses = new Set<number>([
  PersonnelChangeStatus.Completed,
  PersonnelChangeStatus.Rejected,
  PersonnelChangeStatus.Cancelled,
]);

const attentionStatuses = new Set<number>([
  PersonnelChangeStatus.PendingDirectorApproval,
  PersonnelChangeStatus.PendingContractFlow,
  PersonnelChangeStatus.ContractNegotiating,
  PersonnelChangeStatus.ContractRejected,
  PersonnelChangeStatus.Escalated,
]);

export const PersonnelChangeTrackingPage = () => {
  const [records, setRecords] = useState<PersonnelChangeListItem[]>([]);
  const [selected, setSelected] = useState<PersonnelChangeDetail | null>(null);
  const [riskSummary, setRiskSummary] = useState<PersonnelChangeRiskSummary | null>(null);
  const [timeline, setTimeline] = useState<PersonnelChangeTimelineItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [detailOpen, setDetailOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [changeType, setChangeType] = useState("all");
  const [status, setStatus] = useState("all");

  const loadRecords = async () => {
    setLoading(true);
    try {
      const response = await personnelChangeApi.getList();
      setRecords(
        [...(response.data ?? [])].sort(
          (left, right) =>
            new Date(right.requestedAt).getTime() - new Date(left.requestedAt).getTime(),
        ),
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadRecords();
  }, []);

  const filteredRecords = useMemo(() => {
    const keyword = search.trim().toLowerCase();
    return records.filter((record) => {
      const matchesType = changeType === "all" || String(record.changeType) === changeType;
      const matchesStatus = status === "all" || String(record.status) === status;
      const matchesKeyword =
        !keyword ||
        String(record.id).includes(keyword) ||
        (record.employeeCode ?? "").toLowerCase().includes(keyword) ||
        (record.employeeName ?? "").toLowerCase().includes(keyword) ||
        (record.reason ?? "").toLowerCase().includes(keyword) ||
        (record.requestedByName ?? "").toLowerCase().includes(keyword);

      return matchesType && matchesStatus && matchesKeyword;
    });
  }, [changeType, records, search, status]);

  const metrics = useMemo(() => {
    const inProgress = records.filter((item) => !finalStatuses.has(item.status)).length;
    const completed = records.filter((item) => item.status === PersonnelChangeStatus.Completed).length;
    const attention = records.filter((item) => attentionStatuses.has(item.status)).length;
    const contractRelated = records.filter((item) => item.requiresContractFlow).length;

    return { total: records.length, inProgress, attention, completed, contractRelated };
  }, [records]);

  const openDetail = async (id: number) => {
    setLoading(true);
    try {
      const [detailRes, riskRes, timelineRes] = await Promise.all([
        personnelChangeApi.getDetail(id),
        personnelChangeApi.getRiskSummary(id),
        personnelChangeApi.getTimeline(id),
      ]);

      setSelected(detailRes.data);
      setRiskSummary(riskRes.data);
      setTimeline(timelineRes.data ?? []);
      setDetailOpen(true);
    } finally {
      setLoading(false);
    }
  };

  const columns: Array<DataTableColumn<PersonnelChangeListItem>> = [
    {
      key: "code",
      header: "Hồ sơ",
      render: (row) => <span className="font-semibold text-[var(--hicas-text-main)]">{formatTrackingCode(row)}</span>,
    },
    {
      key: "employee",
      header: "Nhân sự",
      render: (row) => (
        <div>
          <p className="font-semibold text-[var(--hicas-text-main)]">
            {row.employeeName || "Chưa chọn nhân sự"}
          </p>
          <p className="text-xs text-[var(--hicas-text-secondary)]">
            {row.employeeCode || (row.employeeId ? `#${row.employeeId}` : "Đang xử lý")}
          </p>
        </div>
      ),
    },
    {
      key: "type",
      header: "Loại biến động",
      render: (row) => getChangeTypeLabel(row),
    },
    {
      key: "status",
      header: "Trạng thái",
      render: (row) => <PersonnelChangeStatusBadge status={row.status} />,
    },
    {
      key: "requestedAt",
      header: "Ngày tạo",
      render: (row) => formatDate(row.requestedAt),
    },
    {
      key: "effectiveDate",
      header: "Hiệu lực",
      render: (row) => formatDate(row.effectiveDate),
    },
    {
      key: "requestedBy",
      header: "Người tạo",
      render: (row) => row.requestedByName || `#${row.requestedByAccountId}`,
    },
    {
      key: "contract",
      header: "Hợp đồng",
      render: (row) => (row.requiresContractFlow ? "Có liên quan" : "Không áp dụng"),
    },
    {
      key: "action",
      header: "",
      render: (row) => (
        <Button variant="secondary" size="sm" onClick={() => void openDetail(row.id)}>
          Xem
        </Button>
      ),
    },
  ];

  return (
    <div className="space-y-5">
      <Card
        title="Theo dõi biến động"
        description="Quan sát toàn bộ hồ sơ biến động, trạng thái xử lý, lịch sử thao tác và dữ liệu tham chiếu."
        actions={
          <Button variant="secondary" onClick={() => void loadRecords()}>
            Làm mới
          </Button>
        }
      >
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
          <TrackingMetric label="Tổng hồ sơ" value={metrics.total} />
          <TrackingMetric label="Đang xử lý" value={metrics.inProgress} />
          <TrackingMetric label="Cần chú ý" value={metrics.attention} tone="warning" />
          <TrackingMetric label="Hoàn tất" value={metrics.completed} tone="success" />
          <TrackingMetric label="Liên quan hợp đồng" value={metrics.contractRelated} />
        </div>
      </Card>

      <Card title="Bộ lọc" description="Lọc nhanh theo loại hồ sơ, trạng thái hoặc thông tin nhân sự.">
        <div className="grid gap-3 lg:grid-cols-[1.3fr_1fr_1fr_auto]">
          <label className="space-y-1">
            <span className="text-sm font-semibold text-[var(--hicas-text-main)]">Tìm kiếm</span>
            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Nhập tên, mã nhân viên, mã hồ sơ hoặc lý do"
              className="h-11 w-full rounded-[var(--radius-md)] border border-[var(--hicas-border)] px-3 text-sm outline-none focus:border-[var(--hicas-primary)]"
            />
          </label>
          <label className="space-y-1">
            <span className="text-sm font-semibold text-[var(--hicas-text-main)]">Loại hồ sơ</span>
            <select
              value={changeType}
              onChange={(event) => setChangeType(event.target.value)}
              className="h-11 w-full rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white px-3 text-sm outline-none focus:border-[var(--hicas-primary)]"
            >
              {changeTypeOptions.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label className="space-y-1">
            <span className="text-sm font-semibold text-[var(--hicas-text-main)]">Trạng thái</span>
            <select
              value={status}
              onChange={(event) => setStatus(event.target.value)}
              className="h-11 w-full rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white px-3 text-sm outline-none focus:border-[var(--hicas-primary)]"
            >
              {statusOptions.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <div className="flex items-end">
            <Button
              variant="secondary"
              onClick={() => {
                setSearch("");
                setChangeType("all");
                setStatus("all");
              }}
            >
              Xóa lọc
            </Button>
          </div>
        </div>
      </Card>

      <DataTable
        columns={columns}
        data={filteredRecords}
        loading={loading}
        rowKey={(row) => row.id}
        emptyTitle="Chưa có hồ sơ biến động"
        emptyDescription="Các hồ sơ phù hợp với phạm vi quyền xem sẽ hiển thị tại đây."
        scrollContainerClassName="max-h-[640px] overflow-auto"
        stickyHeader
        tableClassName="min-w-[1080px]"
      />

      <PersonnelChangeDetailDrawer
        open={detailOpen}
        request={selected}
        riskSummary={riskSummary}
        timeline={timeline}
        onClose={() => setDetailOpen(false)}
      />
    </div>
  );
};

const TrackingMetric = ({
  label,
  value,
  tone = "neutral",
}: {
  label: string;
  value: number;
  tone?: "neutral" | "warning" | "success";
}) => {
  const toneClass =
    tone === "warning"
      ? "border-orange-200 bg-orange-50 text-orange-700"
      : tone === "success"
        ? "border-emerald-200 bg-emerald-50 text-emerald-700"
        : "border-[var(--hicas-border)] bg-white text-[var(--hicas-text-main)]";

  return (
    <div className={`rounded-[var(--radius-md)] border p-4 ${toneClass}`}>
      <p className="text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">{label}</p>
      <p className="mt-2 text-2xl font-bold">{value}</p>
    </div>
  );
};

const changeTypeLabels: Record<number, string> = {
  [PersonnelChangeType.ConvertToOfficial]: "Chuyển chính thức",
  [PersonnelChangeType.Promotion]: "Thăng tiến",
  [PersonnelChangeType.SeniorAppointment]: "Bổ nhiệm cấp cao",
  [PersonnelChangeType.VoluntaryTermination]: "Nghỉ việc chủ động",
  [PersonnelChangeType.Dismissal]: "Kỷ luật và sa thải",
  [PersonnelChangeType.InternalTransfer]: "Thuyên chuyển nội bộ",
};

const typePrefixes: Record<number, string> = {
  [PersonnelChangeType.ConvertToOfficial]: "CO",
  [PersonnelChangeType.Promotion]: "PR",
  [PersonnelChangeType.SeniorAppointment]: "SA",
  [PersonnelChangeType.VoluntaryTermination]: "VT",
  [PersonnelChangeType.Dismissal]: "DS",
  [PersonnelChangeType.InternalTransfer]: "IT",
};

const getChangeTypeLabel = (row: PersonnelChangeListItem) =>
  changeTypeLabels[row.changeType] ?? "Biến động nhân sự";

const formatTrackingCode = (row: PersonnelChangeListItem) => {
  const prefix = typePrefixes[row.changeType] ?? "PC";
  return `${prefix}-${String(row.id).padStart(5, "0")}`;
};
