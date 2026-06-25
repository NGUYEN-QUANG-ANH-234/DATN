import { useState } from "react";
import { Database, Power, Trash2 } from "lucide-react";
import { PageHeader } from "../../../components/layout";
import { Badge, Button, Card, DataTable, Tabs } from "../../../components/ui";
import type { DataTableColumn } from "../../../components/ui";
import { useSalaryVariable } from "../hooks/useSalaryVariable";
import type { SourceCatalogItem } from "../types/salaryVariable";

const dataTypeLabels: Record<string, string> = {
  Number: "Số",
  Money: "Tiền",
  Hours: "Giờ",
  Days: "Ngày",
  Percent: "Tỷ lệ",
};

const aggregationLabels: Record<string, string> = {
  Latest: "Giá trị mới nhất",
  Sum: "Tổng",
  Count: "Đếm",
  MonthlyTotal: "Tổng theo tháng",
  Manual: "Nhập tay",
};

type MessageState = {
  type: "success" | "error";
  text: string;
};

export const PayrollSourceCatalogManager = () => {
  const { catalogs, loading, setCatalogActive, deleteCatalog } = useSalaryVariable();
  const [catalogTab, setCatalogTab] = useState<"active" | "inactive">("active");
  const [message, setMessage] = useState<MessageState | null>(null);

  const activeCatalogs = catalogs.filter((item) => item.isActive);
  const inactiveCatalogs = catalogs.filter((item) => !item.isActive);
  const visibleCatalogs = catalogTab === "active" ? activeCatalogs : inactiveCatalogs;

  const handleToggleCatalog = async (row: SourceCatalogItem) => {
    try {
      const res = await setCatalogActive(row.id, !row.isActive);
      setMessage({
        type: "success",
        text: res.message || "Đã cập nhật trạng thái nguồn dữ liệu.",
      });
    } catch (error: unknown) {
      setMessage({ type: "error", text: String(error) });
    }
  };

  const handleDeleteCatalog = async (row: SourceCatalogItem) => {
    const confirmed = window.confirm(
      `Xóa hẳn nguồn dữ liệu "${row.displayName}"? Thao tác này sẽ ẩn nguồn khỏi danh mục cấu hình lương và không thể hoàn tác.`,
    );
    if (!confirmed) return;

    try {
      const res = await deleteCatalog(row.id);
      setMessage({
        type: "success",
        text: res.message || "Đã xóa nguồn dữ liệu lương.",
      });
    } catch (error: unknown) {
      setMessage({ type: "error", text: String(error) });
    }
  };

  const catalogColumns: Array<DataTableColumn<SourceCatalogItem>> = [
    {
      key: "displayName",
      header: "Nguồn dữ liệu",
      render: (row) => (
        <div>
          <p className="font-semibold text-[var(--hicas-text-main)]">{row.displayName}</p>
          <p className="mt-1 break-all font-mono text-xs text-[var(--hicas-text-secondary)]">
            {row.sourcePath}
          </p>
        </div>
      ),
    },
    { key: "module", header: "Phân hệ", render: (row) => row.module },
    {
      key: "dataType",
      header: "Kiểu / Tổng hợp",
      render: (row) =>
        `${dataTypeLabels[row.dataType] ?? row.dataType} / ${
          aggregationLabels[row.aggregationType] ?? row.aggregationType
        }`,
    },
    {
      key: "period",
      header: "Theo kỳ",
      render: (row) => (
        <Badge variant={row.isPeriodBased ? "info" : "neutral"}>
          {row.isPeriodBased ? "Có" : "Không"}
        </Badge>
      ),
    },
    {
      key: "status",
      header: "Trạng thái",
      render: (row) => (
        <Badge variant={row.isActive ? "success" : "neutral"}>
          {row.isActive ? "Đang dùng" : "Tạm tắt"}
        </Badge>
      ),
    },
    {
      key: "actions",
      header: "",
      className: "text-right",
      render: (row) => (
        <div className="flex flex-wrap justify-end gap-2">
        <Button
          size="sm"
          variant={row.isActive ? "ghost" : "secondary"}
          iconLeft={<Power size={15} />}
          onClick={() => handleToggleCatalog(row)}
        >
          {row.isActive ? "Tắt" : "Bật"}
        </Button>
          {!row.isActive && (
            <Button
              size="sm"
              variant="danger"
              iconLeft={<Trash2 size={15} />}
              onClick={() => handleDeleteCatalog(row)}
            >
              Xóa hẳn
            </Button>
          )}
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        title="Nguồn dữ liệu lương"
        description="Bật hoặc tắt các nguồn dữ liệu được dùng để tạo biến lương."
        breadcrumb={[
          { label: "Cấu hình hệ thống" },
          { label: "Nguồn dữ liệu lương" },
        ]}
      />

      {message && (
        <div
          className={`rounded-2xl border px-4 py-3 text-sm font-medium ${
            message.type === "error"
              ? "border-[var(--hicas-danger)] bg-[var(--hicas-danger-soft)] text-[var(--hicas-danger)]"
              : "border-[var(--hicas-success)] bg-[var(--hicas-success-soft)] text-[var(--hicas-success)]"
          }`}
        >
          {message.text}
        </div>
      )}

      <section className="grid gap-4 md:grid-cols-3">
        <div className="rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-white p-5 shadow-sm">
          <p className="text-sm font-semibold text-[var(--hicas-text-secondary)]">Tổng nguồn</p>
          <p className="mt-2 text-3xl font-bold text-[var(--hicas-text-main)]">
            {catalogs.length}
          </p>
          <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">nguồn dữ liệu</p>
        </div>
        <div className="rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-white p-5 shadow-sm">
          <p className="text-sm font-semibold text-[var(--hicas-text-secondary)]">Đang dùng</p>
          <p className="mt-2 text-3xl font-bold text-[var(--hicas-orange-dark)]">
            {activeCatalogs.length}
          </p>
          <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">nguồn dữ liệu</p>
        </div>
        <div className="rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-white p-5 shadow-sm">
          <p className="text-sm font-semibold text-[var(--hicas-text-secondary)]">Tạm tắt</p>
          <p className="mt-2 text-3xl font-bold text-[var(--hicas-text-main)]">
            {inactiveCatalogs.length}
          </p>
          <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">nguồn dữ liệu</p>
        </div>
      </section>

      <Card
        title="Danh sách nguồn dữ liệu"
        actions={<Database size={20} className="text-[var(--hicas-orange)]" />}
      >
        <div className="mb-4">
          <Tabs
            value={catalogTab}
            onChange={(value) => setCatalogTab(value as "active" | "inactive")}
            items={[
              {
                value: "active",
                label: "Đang dùng",
                badge: <Badge variant="success">{activeCatalogs.length}</Badge>,
              },
              {
                value: "inactive",
                label: "Tạm tắt",
                badge: <Badge variant="neutral">{inactiveCatalogs.length}</Badge>,
              },
            ]}
          />
        </div>
        <div className="max-h-[560px] overflow-y-auto pr-1">
          <DataTable
            columns={catalogColumns}
            data={visibleCatalogs}
            loading={loading}
            rowKey={(row) => row.id}
            className="border-0 shadow-none"
            emptyTitle="Chưa có nguồn dữ liệu"
          />
        </div>
      </Card>
    </div>
  );
};
