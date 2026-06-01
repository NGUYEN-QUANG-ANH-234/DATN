import { recentActivities } from "../../data";
import { Badge, Card } from "../ui";
import type { BadgeVariant } from "../ui/Badge";

export const RecentActivities = () => (
  <Card title="Hoạt động gần đây">
    <div className="space-y-4">
      {recentActivities.map((activity) => (
        <div
          key={`${activity.name}-${activity.action}`}
          className="flex items-start justify-between gap-4 rounded-2xl border border-[var(--hicas-border-soft)] p-3"
        >
          <div className="min-w-0">
            <p className="truncate text-sm font-semibold text-[var(--hicas-text-main)]">
              {activity.name}
            </p>
            <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">{activity.action}</p>
            <p className="mt-1 text-xs text-[var(--hicas-text-muted)]">{activity.time}</p>
          </div>
          <Badge variant={activity.variant as BadgeVariant}>{activity.status}</Badge>
        </div>
      ))}
    </div>
  </Card>
);
