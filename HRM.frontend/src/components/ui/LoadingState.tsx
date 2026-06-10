import { Loader2 } from "lucide-react";
import { cn } from "./classNames";

export type LoadingStateProps = {
  title?: string;
  description?: string;
  className?: string;
};

export const LoadingState = ({
  title = "Đang tải dữ liệu...",
  description,
  className,
}: LoadingStateProps) => (
  <div
    className={cn(
      "rounded-[var(--radius-xl)] border border-[var(--hicas-border)] bg-white px-6 py-10 text-center",
      className,
    )}
  >
    <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-[var(--radius-lg)] bg-[var(--hicas-orange-soft)] text-[var(--hicas-orange)]">
      <Loader2 size={22} className="animate-spin" />
    </div>
    <p className="font-semibold text-[var(--hicas-text-main)]">{title}</p>
    {description ? (
      <p className="mx-auto mt-1 max-w-md text-sm leading-6 text-[var(--hicas-text-secondary)]">
        {description}
      </p>
    ) : null}
  </div>
);
