import { Link } from "react-router-dom";
import { quickActions } from "../../data";
import { Card } from "../ui";

export const QuickActions = () => (
  <Card title="Thao tác nhanh">
    <div className="grid grid-cols-2 gap-3">
      {quickActions.map((action) => {
        const Icon = action.icon;

        return (
          <Link
            key={action.label}
            to={action.path}
            className="group rounded-2xl border border-[var(--hicas-border)] bg-white p-4 transition hover:border-[var(--hicas-orange)] hover:bg-[var(--hicas-orange-lighter)]"
          >
            <span className="mb-3 flex h-11 w-11 items-center justify-center rounded-xl bg-[var(--hicas-orange-soft)] text-[var(--hicas-orange)] transition group-hover:shadow-[var(--shadow-orange)]">
              <Icon size={20} />
            </span>
            <span className="text-sm font-semibold text-[var(--hicas-text-main)]">
              {action.label}
            </span>
          </Link>
        );
      })}
    </div>
  </Card>
);
