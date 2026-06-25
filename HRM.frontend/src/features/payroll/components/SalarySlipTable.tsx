import { useEffect, useMemo, useRef, useState } from "react";
import { Eye, Search } from "lucide-react";
import { Button, DataTable, StatusBadge } from "../../../components/ui";
import type { DataTableColumn } from "../../../components/ui";
import type { SalarySlip, SalarySlipTableProps } from "../types/payroll";
import { formatMoney, formatNumber, getPayrollStatusLabel, normalizePayrollStatus } from "../utils";

const formatMinutes = (value?: number) => `${value ?? 0} phút`;

const normalizeSearchText = (value?: string | number | null) =>
  String(value ?? "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .trim()
    .toLowerCase();

const matchesSalarySlipQuery = (slip: SalarySlip, query: string) => {
  if (!query) return true;

  return [
    slip.employeeCode,
    slip.employeeName,
    slip.departmentName,
    slip.positionName,
    slip.period,
    slip.statusText,
    normalizePayrollStatus(slip.status),
  ]
    .map(normalizeSearchText)
    .some((value) => value.includes(query));
};

export const SalarySlipTable = ({
  slips,
  selectedIds,
  loading = false,
  emptyText,
  onToggle,
  onSelectMany,
  onUnselectMany,
  onClearSelected,
  onOpenDetail,
}: SalarySlipTableProps) => {
  const selectAllRef = useRef<HTMLInputElement | null>(null);
  const [query, setQuery] = useState("");

  const normalizedQuery = normalizeSearchText(query);
  const filteredSlips = useMemo(
    () => slips.filter((slip) => matchesSalarySlipQuery(slip, normalizedQuery)),
    [normalizedQuery, slips],
  );
  const visibleIds = useMemo(() => filteredSlips.map((slip) => slip.id), [filteredSlips]);
  const selectedVisibleCount = visibleIds.filter((id) => selectedIds.includes(id)).length;
  const allVisibleSelected = visibleIds.length > 0 && selectedVisibleCount === visibleIds.length;
  const hasVisibleSelection = selectedVisibleCount > 0;

  useEffect(() => {
    if (selectAllRef.current) {
      selectAllRef.current.indeterminate = hasVisibleSelection && !allVisibleSelected;
    }
  }, [allVisibleSelected, hasVisibleSelection]);

  const selectVisible = () => {
    if (visibleIds.length === 0) return;

    if (allVisibleSelected) {
      if (onUnselectMany) {
        onUnselectMany(visibleIds);
        return;
      }

      visibleIds.filter((id) => selectedIds.includes(id)).forEach(onToggle);
      return;
    }

    const idsToSelect = visibleIds.filter((id) => !selectedIds.includes(id));
    if (onSelectMany) {
      onSelectMany(idsToSelect);
      return;
    }

    idsToSelect.forEach(onToggle);
  };

  const clearSelection = () => {
    if (onClearSelected) {
      onClearSelected();
      return;
    }

    selectedIds.forEach(onToggle);
  };

  const columns: Array<DataTableColumn<SalarySlip>> = [
    {
      key: "select",
      header: (
        <label className="flex items-center gap-2 text-xs font-semibold text-[var(--hicas-text-secondary)]">
          <input
            ref={selectAllRef}
            type="checkbox"
            checked={allVisibleSelected}
            disabled={visibleIds.length === 0}
            onChange={selectVisible}
            className="h-4 w-4 accent-[var(--hicas-orange)]"
            aria-label="Chọn tất cả phiếu đang hiển thị"
          />
          Chọn
        </label>
      ),
      render: (slip) => (
        <input
          type="checkbox"
          checked={selectedIds.includes(slip.id)}
          onChange={() => onToggle(slip.id)}
          className="h-4 w-4 accent-[var(--hicas-orange)]"
          aria-label={`Chọn phiếu lương của ${slip.employeeName}`}
        />
      ),
      headerClassName: "w-24",
      className: "w-24",
    },
    {
      key: "employee",
      header: "Nhân viên",
      render: (slip) => (
        <div>
          <p className="font-semibold text-[var(--hicas-text-main)]">{slip.employeeName}</p>
          <p className="text-xs text-[var(--hicas-text-secondary)]">{slip.employeeCode}</p>
        </div>
      ),
    },
    { key: "department", header: "Phòng ban", render: (slip) => slip.departmentName || "Chưa có" },
    { key: "standardWorkdays", header: "Công chuẩn", render: (slip) => formatNumber(slip.standardWorkDays) },
    { key: "actualWorkdays", header: "Công thực tế", render: (slip) => formatNumber(slip.actualWorkDays) },
    { key: "payableHours", header: "Giờ tính lương", render: (slip) => formatNumber(slip.payableWorkHours) },
    { key: "late", header: "Đi muộn", render: (slip) => formatMinutes(slip.lateMinutes) },
    { key: "early", header: "Về sớm", render: (slip) => formatMinutes(slip.earlyLeaveMinutes) },
    { key: "gross", header: "Tổng thu nhập", render: (slip) => formatMoney(slip.grossIncome) },
    { key: "insurance", header: "Bảo hiểm", render: (slip) => formatMoney(slip.employeeInsuranceAmount) },
    { key: "pit", header: "Thuế TNCN", render: (slip) => formatMoney(slip.pitAmount) },
    {
      key: "net",
      header: "Thực nhận",
      render: (slip) => (
        <span className="font-bold text-[var(--hicas-text-main)]">{formatMoney(slip.netSalary)}</span>
      ),
    },
    {
      key: "status",
      header: "Trạng thái",
      render: (slip) => (
        <StatusBadge
          status={normalizePayrollStatus(slip.status)}
          label={slip.statusText || getPayrollStatusLabel(slip.status)}
        />
      ),
    },
    {
      key: "actions",
      header: "Thao tác",
      render: (slip) => (
        <Button size="sm" variant="ghost" onClick={() => onOpenDetail(slip.id)}>
          <Eye size={16} />
          Chi tiết
        </Button>
      ),
    },
  ];

  return (
    <div className="space-y-3">
      <div className="flex flex-col gap-3 rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-[var(--hicas-bg-soft)] p-3 lg:flex-row lg:items-center lg:justify-between">
        <div className="relative w-full lg:max-w-md">
          <Search
            size={18}
            className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[var(--hicas-text-muted)]"
          />
          <input
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Tìm mã nhân viên, tên, phòng ban"
            className="min-h-11 w-full rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white pl-10 pr-3 text-sm font-medium text-[var(--hicas-text-main)] outline-none transition focus:border-[var(--hicas-orange)] focus:ring-2 focus:ring-orange-100"
          />
        </div>

        <div className="flex flex-col gap-3 text-sm text-[var(--hicas-text-secondary)] sm:flex-row sm:items-center sm:justify-between lg:justify-end">
          <span>
            Hiển thị <strong className="text-[var(--hicas-text-main)]">{filteredSlips.length}</strong> /
            {" "}
            {slips.length} phiếu, đã chọn{" "}
            <strong className="text-[var(--hicas-orange)]">{selectedIds.length}</strong>
          </span>
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              size="sm"
              variant="secondary"
              disabled={visibleIds.length === 0}
              onClick={selectVisible}
            >
              {allVisibleSelected ? "Bỏ chọn đang hiển thị" : "Chọn tất cả đang hiển thị"}
            </Button>
            <Button
              type="button"
              size="sm"
              variant="ghost"
              disabled={selectedIds.length === 0}
              onClick={clearSelection}
            >
              Bỏ chọn tất cả
            </Button>
          </div>
        </div>
      </div>

      <DataTable
        columns={columns}
        data={filteredSlips}
        loading={loading}
        rowKey={(row) => row.id}
        emptyTitle={normalizedQuery ? "Không tìm thấy phiếu lương phù hợp" : emptyText}
        emptyDescription={
          normalizedQuery
            ? "Thử tìm bằng mã nhân viên, họ tên hoặc phòng ban khác."
            : undefined
        }
        stickyHeader
        scrollContainerClassName="max-h-[620px] overflow-auto"
        tableClassName="min-w-[1180px]"
      />
    </div>
  );
};
