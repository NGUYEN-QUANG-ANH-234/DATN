import type { PayrollMetricCardProps } from "../types/payroll";

export const PayrollMetricCard = ({ label, value, strong = false }: PayrollMetricCardProps) => (
  <div className="hicas-card px-4 py-4">
    <p className="text-xs font-semibold uppercase tracking-[0.08em] text-[var(--hicas-text-secondary)]">
      {label}
    </p>
    <p
      className={`mt-2 truncate ${
        strong ? "text-xl font-bold" : "text-base font-semibold"
      } text-[var(--hicas-text-main)]`}
    >
      {value}
    </p>
  </div>
);
