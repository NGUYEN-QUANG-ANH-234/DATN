import type { ReactNode } from "react";
import { cn } from "./classNames";

export type TabItem = {
  value: string;
  label: ReactNode;
  badge?: ReactNode;
  disabled?: boolean;
};

export type TabsProps = {
  items: TabItem[];
  value: string;
  onChange: (value: string) => void;
};

export const Tabs = ({ items, value, onChange }: TabsProps) => (
  <div className="flex flex-wrap gap-2 border-b border-[var(--hicas-border)]">
    {items.map((item) => {
      const active = item.value === value;

      return (
        <button
          key={item.value}
          type="button"
          disabled={item.disabled}
          onClick={() => onChange(item.value)}
          className={cn(
            "inline-flex min-h-11 items-center gap-2 border-b-2 px-3 text-sm font-semibold transition disabled:cursor-not-allowed disabled:opacity-50",
            active
              ? "border-[var(--hicas-orange)] text-[var(--hicas-orange-dark)]"
              : "border-transparent text-[var(--hicas-text-secondary)] hover:text-[var(--hicas-text-main)]",
          )}
        >
          {item.label}
          {item.badge}
        </button>
      );
    })}
  </div>
);
