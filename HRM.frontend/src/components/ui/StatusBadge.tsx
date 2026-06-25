import { getStatusConfig, normalizeStatusKey, statusMap, type StatusValue } from "../../data/statusMap";
import { Badge } from "./Badge";

export type StatusBadgeProps = {
  status?: StatusValue;
  label?: string;
};

export const StatusBadge = ({ status, label }: StatusBadgeProps) => {
  const config = getStatusConfig(status, label);
  const customLabelKey = normalizeStatusKey(label);
  const shouldUseCustomLabel = Boolean(label && !statusMap[customLabelKey]);

  return <Badge variant={config.variant}>{shouldUseCustomLabel ? label : config.label}</Badge>;
};
