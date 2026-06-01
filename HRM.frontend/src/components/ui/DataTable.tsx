import type { ReactNode } from "react";
import { EmptyState } from "./EmptyState";
import { Pagination } from "./Pagination";
import { cn } from "./classNames";

export type DataTableColumn<T> = {
  key: string;
  header: ReactNode;
  render: (row: T, index: number) => ReactNode;
  className?: string;
  headerClassName?: string;
};

export type DataTableProps<T> = {
  columns: Array<DataTableColumn<T>>;
  data: T[];
  rowKey: (row: T, index: number) => string | number;
  loading?: boolean;
  emptyTitle?: string;
  emptyDescription?: string;
  className?: string;
  tableClassName?: string;
  page?: number;
  pageSize?: number;
  totalItems?: number;
  onPageChange?: (page: number) => void;
};

export const DataTable = <T,>({
  columns,
  data,
  rowKey,
  loading = false,
  emptyTitle = "Không có dữ liệu",
  emptyDescription,
  className,
  tableClassName,
  page,
  pageSize,
  totalItems,
  onPageChange,
}: DataTableProps<T>) => {
  const showPagination =
    page !== undefined &&
    pageSize !== undefined &&
    totalItems !== undefined &&
    onPageChange !== undefined;

  return (
    <div className={cn("hicas-card overflow-hidden", className)} aria-busy={loading}>
      <div className="overflow-x-auto">
        <table className={cn("hicas-table min-w-[760px] text-left md:min-w-full", tableClassName)}>
          <thead>
            <tr>
              {columns.map((column) => (
                <th key={column.key} className={cn("px-4 py-3", column.headerClassName)}>
                  {column.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loading &&
              Array.from({ length: 5 }).map((_, index) => (
                <tr key={`loading-${index}`}>
                  {columns.map((column) => (
                    <td key={column.key} className="px-4 py-3">
                      <div className="h-3 w-full animate-pulse rounded bg-[var(--hicas-bg-soft)]" />
                    </td>
                  ))}
                </tr>
              ))}

            {!loading &&
              data.map((row, index) => (
                <tr key={rowKey(row, index)}>
                  {columns.map((column) => (
                    <td key={column.key} className={cn("px-4 py-3", column.className)}>
                      {column.render(row, index)}
                    </td>
                  ))}
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      {!loading && data.length === 0 && (
        <div className="p-4 sm:p-6">
          <EmptyState title={emptyTitle} description={emptyDescription} />
        </div>
      )}

      {showPagination && (
        <Pagination
          page={page}
          pageSize={pageSize}
          totalItems={totalItems}
          onPageChange={onPageChange}
        />
      )}
    </div>
  );
};
