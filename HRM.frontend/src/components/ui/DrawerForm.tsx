import type { ReactNode } from "react";
import { X } from "lucide-react";
import { Button } from "./Button";
import { cn } from "./classNames";

export type DrawerFormProps = {
  open: boolean;
  title: string;
  description?: string;
  children: ReactNode;
  footer?: ReactNode;
  submitLabel?: string;
  cancelLabel?: string;
  isSubmitting?: boolean;
  width?: "md" | "lg" | "xl";
  onSubmit?: () => void;
  onClose: () => void;
};

const widthClass: Record<NonNullable<DrawerFormProps["width"]>, string> = {
  md: "sm:max-w-md",
  lg: "sm:max-w-2xl",
  xl: "sm:max-w-4xl",
};

export const DrawerForm = ({
  open,
  title,
  description,
  children,
  footer,
  submitLabel = "Lưu",
  cancelLabel = "Hủy",
  isSubmitting = false,
  width = "lg",
  onSubmit,
  onClose,
}: DrawerFormProps) => {
  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex justify-end bg-black/35 backdrop-blur-sm">
      <aside className={cn("flex h-full w-full flex-col bg-white shadow-2xl", widthClass[width])}>
        <header className="flex items-start justify-between gap-4 border-b border-[var(--hicas-border)] px-4 py-4 sm:px-6 sm:py-5">
          <div>
            <h2 className="text-lg font-semibold text-[var(--hicas-text-main)] sm:text-xl">{title}</h2>
            {description && (
              <p className="mt-1 text-sm leading-6 text-[var(--hicas-text-secondary)]">
                {description}
              </p>
            )}
          </div>
          <button
            type="button"
            onClick={onClose}
            className="inline-flex h-10 w-10 shrink-0 items-center justify-center rounded-lg text-[var(--hicas-text-muted)] hover:bg-[var(--hicas-bg-soft)] hover:text-[var(--hicas-text-main)]"
            aria-label="Đóng"
          >
            <X size={20} />
          </button>
        </header>

        <div className="flex-1 overflow-y-auto px-4 py-5 sm:px-6">{children}</div>

        <footer className="sticky bottom-0 flex flex-col-reverse gap-3 border-t border-[var(--hicas-border)] bg-white px-4 py-4 sm:flex-row sm:justify-end sm:px-6">
          {footer ?? (
            <>
              <Button variant="secondary" onClick={onClose} className="w-full sm:w-auto">
                {cancelLabel}
              </Button>
              {onSubmit && (
                <Button isLoading={isSubmitting} onClick={onSubmit} className="w-full sm:w-auto">
                  {submitLabel}
                </Button>
              )}
            </>
          )}
        </footer>
      </aside>
    </div>
  );
};
