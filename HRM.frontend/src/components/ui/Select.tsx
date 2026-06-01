import type { SelectHTMLAttributes } from "react";
import { cn } from "./classNames";

export type SelectOption = {
  value: string | number;
  label: string;
  disabled?: boolean;
};

export type SelectProps = SelectHTMLAttributes<HTMLSelectElement> & {
  label?: string;
  helperText?: string;
  error?: string;
  options: SelectOption[];
  placeholder?: string;
};

export const Select = ({
  label,
  helperText,
  error,
  options,
  placeholder,
  className,
  id,
  ...props
}: SelectProps) => {
  const selectId = id || props.name;

  return (
    <label className="block" htmlFor={selectId}>
      {label && (
        <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">
          {label}
        </span>
      )}
      <select
        id={selectId}
        className={cn("hicas-select", error && "border-[var(--hicas-danger)]", className)}
        {...props}
      >
        {placeholder && <option value="">{placeholder}</option>}
        {options.map((option) => (
          <option key={option.value} value={option.value} disabled={option.disabled}>
            {option.label}
          </option>
        ))}
      </select>
      {(error || helperText) && (
        <span className={cn("mt-1 block text-xs", error ? "text-[var(--hicas-danger)]" : "text-[var(--hicas-text-secondary)]")}>
          {error || helperText}
        </span>
      )}
    </label>
  );
};
