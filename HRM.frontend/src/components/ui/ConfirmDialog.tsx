import type { ReactNode } from "react";
import { AlertTriangle, X } from "lucide-react";
import { Button } from "./Button";

export type ConfirmDialogProps = {
  open: boolean;
  title: string;
  description?: ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  tone?: "default" | "danger";
  isLoading?: boolean;
  onConfirm: () => void;
  onClose: () => void;
};

export const ConfirmDialog = ({
  open,
  title,
  description,
  confirmLabel = "Xác nhận",
  cancelLabel = "Hủy",
  tone = "default",
  isLoading = false,
  onConfirm,
  onClose,
}: ConfirmDialogProps) => {
  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4 backdrop-blur-sm">
      <div className="w-full max-w-md rounded-[var(--radius-xl)] border border-[var(--hicas-border)] bg-white p-5 shadow-xl">
        <div className="flex items-start gap-4">
          <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-[var(--radius-lg)] bg-[var(--hicas-warning-soft)] text-[var(--hicas-warning)]">
            <AlertTriangle size={22} />
          </div>
          <div className="min-w-0 flex-1">
            <div className="flex items-start justify-between gap-3">
              <h2 className="text-lg font-semibold text-[var(--hicas-text-main)]">{title}</h2>
              <button
                type="button"
                onClick={onClose}
                className="rounded-lg p-1 text-[var(--hicas-text-muted)] hover:bg-[var(--hicas-bg-soft)] hover:text-[var(--hicas-text-main)]"
                aria-label="Đóng hộp thoại"
              >
                <X size={18} />
              </button>
            </div>
            {description && (
              <div className="mt-2 text-sm leading-6 text-[var(--hicas-text-secondary)]">
                {description}
              </div>
            )}
          </div>
        </div>
        <div className="mt-6 flex justify-end gap-3">
          <Button variant="secondary" onClick={onClose}>
            {cancelLabel}
          </Button>
          <Button
            variant={tone === "danger" ? "danger" : "primary"}
            isLoading={isLoading}
            onClick={onConfirm}
          >
            {confirmLabel}
          </Button>
        </div>
      </div>
    </div>
  );
};
