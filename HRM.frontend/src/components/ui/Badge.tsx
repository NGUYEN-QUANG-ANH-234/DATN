import type { HTMLAttributes, ReactNode } from "react";
import { cn } from "./classNames";

export type BadgeVariant =
  | "success"
  | "warning"
  | "danger"
  | "info"
  | "orange"
  | "neutral";

export type BadgeProps = HTMLAttributes<HTMLSpanElement> & {
  variant?: BadgeVariant;
  children: ReactNode;
};

const variantClass: Record<BadgeVariant, string> = {
  success: "hicas-badge-success",
  warning: "hicas-badge-warning",
  danger: "hicas-badge-danger",
  info: "hicas-badge-info",
  orange: "hicas-badge-orange",
  neutral: "hicas-badge-neutral",
};

export const Badge = ({
  variant = "neutral",
  className,
  children,
  ...props
}: BadgeProps) => (
  <span className={cn("hicas-badge", variantClass[variant], className)} {...props}>
    {children}
  </span>
);
