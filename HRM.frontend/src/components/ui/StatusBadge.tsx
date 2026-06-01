import { getStatusConfig } from "../../data/statusMap";
import { Badge } from "./Badge";

export type StatusBadgeProps = {
  status?: string | null;
  label?: string;
};

export const StatusBadge = ({ status, label }: StatusBadgeProps) => {
  const config = getStatusConfig(status, label);
  return <Badge variant={config.variant}>{label || config.label}</Badge>;
};
