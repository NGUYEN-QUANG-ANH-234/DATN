import { Button, DataTable, type DataTableColumn } from "../../../components/ui";
import { formatDate } from "../../../utils/formatters";
import {
  PersonnelChangeType,
  type PersonnelChangeListItem,
  type PersonnelChangeWorkflowKind,
} from "../types/personnelChange";
import { PersonnelChangeStatusBadge } from "./PersonnelChangeStatusBadge";

type Props = {
  records: PersonnelChangeListItem[];
  kind: PersonnelChangeWorkflowKind;
  loading?: boolean;
  emptyTitle?: string;
  emptyDescription?: string;
  onOpen: (id: number) => void;
};

export const PersonnelChangeList = ({
  records,
  kind,
  loading,
  emptyTitle = "Chưa có hồ sơ biến động",
  emptyDescription = "Các hồ sơ biến động nhân sự sẽ hiển thị tại đây.",
  onOpen,
}: Props) => {
  const columns: Array<DataTableColumn<PersonnelChangeListItem>> = [
    {
      key: "id",
      header: "Hồ sơ",
      render: (row) => <span className="font-semibold">{formatRequestCode(row, kind)}</span>,
    },
    {
      key: "employee",
      header: "Nhân sự",
      render: (row) => row.employeeName || (row.employeeId ? `#${row.employeeId}` : "Chờ xử lý"),
    },
    {
      key: "type",
      header: "Loại",
      render: (row) => getChangeTypeLabel(row),
    },
    {
      key: "reason",
      header: "Lý do",
      render: (row) => row.reason || "-",
    },
    {
      key: "effectiveDate",
      header: "Hiệu lực",
      render: (row) => formatDate(row.effectiveDate),
    },
    {
      key: "status",
      header: "Trạng thái",
      render: (row) => <PersonnelChangeStatusBadge status={row.status} />,
    },
    {
      key: "action",
      header: "",
      render: (row) => (
        <Button variant="secondary" size="sm" onClick={() => onOpen(row.id)}>
          Mở
        </Button>
      ),
    },
  ];

  return (
    <DataTable
      columns={columns}
      data={records}
      loading={loading}
      rowKey={(row) => row.id}
      emptyTitle={emptyTitle}
      emptyDescription={emptyDescription}
    />
  );
};

const getChangeTypeLabel = (row: PersonnelChangeListItem) => {
  if (row.changeType === PersonnelChangeType.ConvertToOfficial) return "Chuyển chính thức";
  if (row.changeType === PersonnelChangeType.Promotion) {
    return row.promotionType === 2 ? "Nâng cấp bậc" : "Thăng tiến chức danh";
  }
  if (row.changeType === PersonnelChangeType.SeniorAppointment) return "Bổ nhiệm cấp cao";
  if (row.changeType === PersonnelChangeType.VoluntaryTermination) return "Nghỉ việc chủ động";
  if (row.changeType === PersonnelChangeType.Dismissal) return "Kỷ luật hoặc sa thải";
  if (row.changeType === PersonnelChangeType.InternalTransfer) return "Thuyên chuyển nội bộ";
  return "Biến động nhân sự";
};

const formatRequestCode = (row: PersonnelChangeListItem, kind: PersonnelChangeWorkflowKind) => {
  const prefix =
    row.changeType === PersonnelChangeType.ConvertToOfficial
      ? "CO"
      : kindPrefix[kind] ?? "PC";
  return `${prefix}-${String(row.id).padStart(5, "0")}`;
};

const kindPrefix: Record<PersonnelChangeWorkflowKind, string> = {
  promotion: "PR",
  "senior-appointment": "SA",
  termination: "VT",
  dismissal: "DS",
  "internal-transfer": "IT",
};

export { formatRequestCode };
