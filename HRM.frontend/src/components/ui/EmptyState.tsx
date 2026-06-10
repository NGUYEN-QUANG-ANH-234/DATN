import type { ReactNode } from "react";
import { Inbox } from "lucide-react";
import { cn } from "./classNames";

export type EmptyStateProps = {
  title?: string;
  description?: string;
  icon?: ReactNode;
  action?: ReactNode;
  className?: string;
};

export const EmptyState = ({
  title = "Chưa có dữ liệu",
  description,
  icon,
  action,
  className,
}: EmptyStateProps) => (
  <div
    className={cn(
      "rounded-[var(--radius-xl)] border border-dashed border-[var(--hicas-border)] bg-white px-6 py-10 text-center",
      className,
    )}
  >
    <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-[var(--radius-lg)] bg-[var(--hicas-orange-soft)] text-[var(--hicas-orange)]">
      {icon || <Inbox size={22} />}
    </div>
    <p className="font-semibold text-[var(--hicas-text-main)]">{title}</p>
    {description && (
      <p className="mx-auto mt-1 max-w-md text-sm leading-6 text-[var(--hicas-text-secondary)]">
        {description}
      </p>
    )}
    {action && <div className="mt-4">{action}</div>}
  </div>
);
