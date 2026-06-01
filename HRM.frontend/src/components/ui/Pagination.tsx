import { ChevronLeft, ChevronRight } from "lucide-react";
import { Button } from "./Button";

export type PaginationProps = {
  page: number;
  pageSize: number;
  totalItems: number;
  onPageChange: (page: number) => void;
};

export const Pagination = ({
  page,
  pageSize,
  totalItems,
  onPageChange,
}: PaginationProps) => {
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));
  const start = totalItems === 0 ? 0 : (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, totalItems);

  return (
    <div className="flex flex-col gap-3 border-t border-[var(--hicas-border-soft)] px-4 py-3 text-sm text-[var(--hicas-text-secondary)] sm:flex-row sm:items-center sm:justify-between">
      <span>
        Hiển thị {start}-{end} / {totalItems}
      </span>
      <div className="flex items-center gap-2">
        <Button
          variant="secondary"
          size="sm"
          iconLeft={<ChevronLeft size={16} />}
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
        >
          Trước
        </Button>
        <span className="min-w-20 text-center font-medium text-[var(--hicas-text-main)]">
          {page}/{totalPages}
        </span>
        <Button
          variant="secondary"
          size="sm"
          iconRight={<ChevronRight size={16} />}
          disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)}
        >
          Sau
        </Button>
      </div>
    </div>
  );
};
