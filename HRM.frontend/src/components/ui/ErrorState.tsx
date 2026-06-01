import type { ReactNode } from "react";
import { AlertTriangle } from "lucide-react";
import { cn } from "./classNames";

export type ErrorStateProps = {
  title?: string;
  description?: string;
  action?: ReactNode;
  className?: string;
};

export const ErrorState = ({
  title = "Không thể tải dữ liệu",
  description = "Vui lòng kiểm tra kết nối hoặc thử lại sau.",
  action,
  className,
}: ErrorStateProps) => (
  <div
    className={cn(
      "rounded-[var(--radius-xl)] border border-[var(--hicas-danger-soft)] bg-[var(--hicas-danger-soft)] px-6 py-10 text-center",
      className,
    )}
  >
    <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-[var(--radius-lg)] bg-white text-[var(--hicas-danger)]">
      <AlertTriangle size={22} />
    </div>
    <p className="font-semibold text-[var(--hicas-danger)]">{title}</p>
    <p className="mx-auto mt-1 max-w-md text-sm leading-6 text-red-700">{description}</p>
    {action && <div className="mt-4">{action}</div>}
  </div>
);
