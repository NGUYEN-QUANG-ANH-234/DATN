import { StatusBadge } from "../../../components/ui";
import {
  getPersonnelChangeStatusLabel,
  type PersonnelChangeStatus,
} from "../types/personnelChange";

type Props = {
  status?: PersonnelChangeStatus | null;
};

export const PersonnelChangeStatusBadge = ({ status }: Props) => {
  const label = getPersonnelChangeStatusLabel(status);
  return <StatusBadge status={label} label={label} />;
};
