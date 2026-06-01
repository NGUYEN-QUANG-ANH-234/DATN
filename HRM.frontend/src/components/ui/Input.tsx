import type { InputHTMLAttributes, ReactNode } from "react";
import { cn } from "./classNames";

export type InputProps = InputHTMLAttributes<HTMLInputElement> & {
  label?: string;
  helperText?: string;
  error?: string;
  iconLeft?: ReactNode;
};

export const Input = ({
  label,
  helperText,
  error,
  iconLeft,
  className,
  id,
  ...props
}: InputProps) => {
  const inputId = id || props.name;

  return (
    <label className="block" htmlFor={inputId}>
      {label && (
        <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">
          {label}
        </span>
      )}
      <span className="relative block">
        {iconLeft && (
          <span className="pointer-events-none absolute left-3 top-1/2 flex -translate-y-1/2 text-[var(--hicas-text-muted)]">
            {iconLeft}
          </span>
        )}
        <input
          id={inputId}
          className={cn(
            "hicas-input",
            Boolean(iconLeft) && "hicas-input-icon-left",
            error && "border-[var(--hicas-danger)]",
            className,
          )}
          {...props}
        />
      </span>
      {(error || helperText) && (
        <span className={cn("mt-1 block text-xs", error ? "text-[var(--hicas-danger)]" : "text-[var(--hicas-text-secondary)]")}>
          {error || helperText}
        </span>
      )}
    </label>
  );
};
