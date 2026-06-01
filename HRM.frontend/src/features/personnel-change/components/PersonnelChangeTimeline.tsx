import { Card } from "../../../components/ui";
import { formatDateTime } from "../../../utils/formatters";
import {
  getPersonnelChangeStatusLabel,
  type PersonnelChangeTimelineItem,
} from "../types/personnelChange";

type Props = {
  items?: PersonnelChangeTimelineItem[];
};

export const PersonnelChangeTimeline = ({ items = [] }: Props) => (
  <Card title="Timeline" description="Lich su xu ly ho so.">
    {items.length === 0 ? (
      <p className="text-sm text-[var(--hicas-text-secondary)]">Chua co lich su xu ly.</p>
    ) : (
      <ol className="space-y-3">
        {items.map((item) => (
          <li key={item.id} className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4">
            <div className="flex flex-col gap-1 sm:flex-row sm:items-center sm:justify-between">
              <p className="font-semibold text-[var(--hicas-text-main)]">{item.action}</p>
              <span className="text-xs text-[var(--hicas-text-secondary)]">{formatDateTime(item.createdAt)}</span>
            </div>
            <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">
              {getPersonnelChangeStatusLabel(item.oldStatus)} {"->"} {getPersonnelChangeStatusLabel(item.newStatus)}
            </p>
            {item.note && <p className="mt-2 text-sm text-[var(--hicas-text-main)]">{item.note}</p>}
          </li>
        ))}
      </ol>
    )}
  </Card>
);
