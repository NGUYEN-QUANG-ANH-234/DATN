import { announcements } from "../../data";
import { Badge, Card } from "../ui";

export const Announcements = () => (
  <Card title="Thông báo nội bộ">
    <div className="space-y-4">
      {announcements.map((announcement) => (
        <div
          key={announcement.title}
          className="rounded-2xl border border-[var(--hicas-border-soft)] bg-[var(--hicas-orange-lighter)] p-4"
        >
          <div className="mb-3 flex items-center justify-between gap-3">
            <h3 className="text-sm font-bold text-[var(--hicas-text-main)]">
              {announcement.title}
            </h3>
            <Badge variant="orange">{announcement.tag}</Badge>
          </div>
          <p className="text-sm leading-6 text-[var(--hicas-text-secondary)]">
            {announcement.content}
          </p>
        </div>
      ))}
    </div>
  </Card>
);
