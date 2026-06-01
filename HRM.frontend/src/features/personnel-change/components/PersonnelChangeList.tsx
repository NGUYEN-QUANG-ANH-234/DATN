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
  emptyTitle = "Chua co ho so bien dong",
  emptyDescription = "Cac ho so bien dong nhan su se hien thi tai day.",
  onOpen,
}: Props) => {
  const columns: Array<DataTableColumn<PersonnelChangeListItem>> = [
    {
      key: "id",
      header: "Ho so",
      render: (row) => <span className="font-semibold">{formatRequestCode(row, kind)}</span>,
    },
    {
      key: "employee",
      header: "Nhan su",
      render: (row) => row.employeeName || (row.employeeId ? `#${row.employeeId}` : "Cho xu ly"),
    },
    {
      key: "type",
      header: "Loai",
      render: (row) => getChangeTypeLabel(row),
    },
    {
      key: "reason",
      header: "Ly do",
      render: (row) => row.reason || "-",
    },
    {
      key: "effectiveDate",
      header: "Hieu luc",
      render: (row) => formatDate(row.effectiveDate),
    },
    {
      key: "status",
      header: "Trang thai",
      render: (row) => <PersonnelChangeStatusBadge status={row.status} />,
    },
    {
      key: "action",
      header: "",
      render: (row) => (
        <Button variant="secondary" size="sm" onClick={() => onOpen(row.id)}>
          Mo
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
  if (row.changeType === PersonnelChangeType.ConvertToOfficial) return "Convert official";
  if (row.changeType === PersonnelChangeType.Promotion) {
    return row.promotionType === 2 ? "Job level promotion" : "Position promotion";
  }
  if (row.changeType === PersonnelChangeType.SeniorAppointment) return "Senior appointment";
  if (row.changeType === PersonnelChangeType.VoluntaryTermination) return "Resignation";
  if (row.changeType === PersonnelChangeType.Dismissal) return "Dismissal";
  if (row.changeType === PersonnelChangeType.InternalTransfer) return "Internal transfer";
  return "Personnel change";
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
