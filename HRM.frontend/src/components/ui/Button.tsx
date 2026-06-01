import type { ButtonHTMLAttributes, ReactNode } from "react";
import { Loader2 } from "lucide-react";
import { cn } from "./classNames";

type ButtonVariant = "primary" | "secondary" | "ghost" | "danger";
type ButtonSize = "sm" | "md" | "lg";

export type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: ButtonVariant;
  size?: ButtonSize;
  iconLeft?: ReactNode;
  iconRight?: ReactNode;
  isLoading?: boolean;
  fullWidth?: boolean;
};

const variantClass: Record<ButtonVariant, string> = {
  primary: "hicas-btn-primary",
  secondary: "hicas-btn-secondary",
  ghost: "hicas-btn-ghost",
  danger:
    "inline-flex items-center justify-center gap-2 rounded-[var(--radius-md)] bg-[var(--hicas-danger)] px-4 font-semibold text-white transition hover:bg-red-600 disabled:cursor-not-allowed disabled:opacity-60",
};

const sizeClass: Record<ButtonSize, string> = {
  sm: "min-h-10 px-3 text-xs",
  md: "min-h-[42px] px-[18px] text-sm",
  lg: "min-h-12 px-5 text-base",
};

export const Button = ({
  variant = "primary",
  size = "md",
  iconLeft,
  iconRight,
  isLoading = false,
  fullWidth = false,
  disabled,
  className,
  children,
  type = "button",
  ...props
}: ButtonProps) => (
  <button
    type={type}
    disabled={disabled || isLoading}
    className={cn(
      variantClass[variant],
      sizeClass[size],
      fullWidth && "w-full",
      "disabled:cursor-not-allowed disabled:opacity-60",
      className,
    )}
    {...props}
  >
    {isLoading ? <Loader2 size={16} className="animate-spin" /> : iconLeft}
    {children}
    {!isLoading && iconRight}
  </button>
);
