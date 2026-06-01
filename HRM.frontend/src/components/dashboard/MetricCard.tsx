import type { LucideIcon } from "lucide-react";
import { Card } from "../ui";
import { cn } from "../ui/classNames";

interface MetricCardProps {
  label: string;
  value: string;
  change: string;
  tone: string;
  icon: LucideIcon;
}

const toneClass: Record<string, string> = {
  orange: "bg-[var(--hicas-orange-soft)] text-[var(--hicas-orange)]",
  success: "bg-[var(--hicas-success-soft)] text-[var(--hicas-success)]",
  info: "bg-[var(--hicas-info-soft)] text-[var(--hicas-info)]",
  warning: "bg-[var(--hicas-warning-soft)] text-[var(--hicas-warning)]",
};

export const MetricCard = ({ label, value, change, tone, icon: Icon }: MetricCardProps) => (
  <Card hoverable className="min-h-[148px]">
    <div className="flex items-start justify-between gap-4">
      <div className="min-w-0">
        <p className="text-sm font-medium text-[var(--hicas-text-secondary)]">{label}</p>
        <p className="mt-3 text-3xl font-bold tracking-tight text-[var(--hicas-text-main)]">
          {value}
        </p>
      </div>
      <div
        className={cn(
          "flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl",
          toneClass[tone] ?? toneClass.orange,
        )}
      >
        <Icon size={22} />
      </div>
    </div>
    <p className="mt-5 text-sm font-medium text-[var(--hicas-text-secondary)]">{change}</p>
  </Card>
);
