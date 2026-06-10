import type { ChangeEvent, FormEvent } from "react";
import { useState } from "react";
import { Database, Power, Save } from "lucide-react";
import { PageHeader } from "../../../components/layout";
import { Badge, Button, Card, DataTable } from "../../../components/ui";
import type { DataTableColumn } from "../../../components/ui";
import { useSalaryVariable } from "../hooks/useSalaryVariable";
import type {
  SalaryVariable,
  SourceCatalogItem,
} from "../types/salaryVariable";

const emptyVariable: SalaryVariable = {
  code: "",
  source: "",
  description: "",
  isActive: true,
};

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

export const SalaryVariableManager = () => {
  const { variables, catalogs, loading, defineVariable, setVariableActive, setCatalogActive } =
    useSalaryVariable();
  const [variableForm, setVariableForm] = useState<SalaryVariable>(emptyVariable);
  const [message, setMessage] = useState<MessageState | null>(null);

  const activeCatalogs = catalogs.filter((item) => item.isActive);

  const handleVariableChange = (
    event: ChangeEvent<HTMLInputElement | HTMLSelectElement>,
  ) => {
    const target = event.target;
    const value =
      target instanceof HTMLInputElement && target.type === "checkbox"
        ? target.checked
        : target.value;

    setVariableForm((prev) => ({ ...prev, [target.name]: value }));
  };

  const handleVariableSubmit = async (event: FormEvent) => {
    event.preventDefault();
    try {
      const res = await defineVariable({
        ...variableForm,
        code: variableForm.code.trim().toUpperCase(),
        source: variableForm.source.trim(),
        description: variableForm.description?.trim(),
      });
      setMessage({ type: "success", text: res.message || "Đã lưu biến lương." });
      setVariableForm(emptyVariable);
    } catch (error: unknown) {
      setMessage({ type: "error", text: String(error) });
    }
  };

  const handleToggleVariable = async (row: SalaryVariable) => {
    try {
      const res = await setVariableActive(row.code, !row.isActive);
      setMessage({
        type: "success",
        text: res.message || "Đã cập nhật trạng thái biến lương.",
      });
    } catch (error: unknown) {
      setMessage({ type: "error", text: String(error) });
    }
  };

  const handleToggleCatalog = async (row: SourceCatalogItem) => {
    try {
      const res = await setCatalogActive(row.id, !row.isActive);
      setMessage({
        type: "success",
        text: res.message || "Đã cập nhật trạng thái nguồn hệ thống.",
      });
    } catch (error: unknown) {
      setMessage({ type: "error", text: String(error) });
    }
  };

  const variableColumns: Array<DataTableColumn<SalaryVariable>> = [
    {
      key: "code",
      header: "Mã biến",
      render: (row) => (
        <span className="font-mono text-sm font-semibold text-[var(--hicas-orange-dark)]">
          {row.code}
        </span>
      ),
    },
    {
      key: "source",
      header: "Nguồn dữ liệu",
      render: (row) => <span className="font-mono text-sm">{row.source}</span>,
    },
    {
      key: "description",
      header: "Mô tả",
      render: (row) => row.description || "--",
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
        <Button
          size="sm"
          variant={row.isActive ? "ghost" : "secondary"}
          iconLeft={<Power size={15} />}
          onClick={() => handleToggleVariable(row)}
        >
          {row.isActive ? "Tắt" : "Bật"}
        </Button>
      ),
    },
  ];

  const catalogColumns: Array<DataTableColumn<SourceCatalogItem>> = [
    {
      key: "displayName",
      header: "Nguồn hệ thống",
      render: (row) => (
        <div>
          <p className="font-semibold text-[var(--hicas-text-main)]">{row.displayName}</p>
          <p className="mt-1 font-mono text-xs text-[var(--hicas-text-secondary)]">
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
        <Button
          size="sm"
          variant={row.isActive ? "ghost" : "secondary"}
          iconLeft={<Power size={15} />}
          onClick={() => handleToggleCatalog(row)}
        >
          {row.isActive ? "Tắt" : "Bật"}
        </Button>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        title="Biến lương"
        description="Chọn nguồn dữ liệu và quản lý các biến dùng trong công thức lương."
        breadcrumb={[
          { label: "Cấu hình hệ thống" },
          { label: "Biến lương" },
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

      <section className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_420px]">
        <Card
          title="Danh sách biến lương"
          actions={
            <Badge variant="orange">
              {variables.length} biến / {activeCatalogs.length} nguồn đang dùng
            </Badge>
          }
        >
          <DataTable
            columns={variableColumns}
            data={variables}
            loading={loading}
            rowKey={(row, index) => `${row.code}-${index}`}
            className="border-0 shadow-none"
            emptyTitle="Chưa có biến lương"
          />
        </Card>

        <Card title="Thêm biến lương">
          <form onSubmit={handleVariableSubmit} className="space-y-4">
            <label className="block">
              <span className="mb-2 block text-sm font-semibold">Mã biến *</span>
              <input
                required
                name="code"
                value={variableForm.code}
                onChange={handleVariableChange}
                pattern="^[a-zA-Z0-9_]+$"
                title="Chỉ dùng chữ, số và dấu gạch dưới"
                className="hicas-input w-full font-mono uppercase"
                placeholder="BASE_SALARY"
              />
            </label>

            <label className="block">
              <span className="mb-2 block text-sm font-semibold">Nguồn dữ liệu *</span>
              <select
                required
                name="source"
                value={variableForm.source}
                onChange={handleVariableChange}
                className="hicas-select w-full"
              >
                <option value="" disabled>
                  Chọn nguồn hệ thống
                </option>
                {activeCatalogs.map((item) => (
                  <option key={item.id} value={item.sourcePath}>
                    [{item.module}] {item.displayName}
                  </option>
                ))}
              </select>
            </label>

            <label className="block">
              <span className="mb-2 block text-sm font-semibold">Mô tả</span>
              <input
                name="description"
                value={variableForm.description}
                onChange={handleVariableChange}
                className="hicas-input w-full"
                placeholder="Ví dụ: Lương cơ bản theo hợp đồng"
              />
            </label>

            <label className="flex items-center gap-2 text-sm font-medium">
              <input
                type="checkbox"
                name="isActive"
                checked={variableForm.isActive}
                onChange={handleVariableChange}
                className="accent-[var(--hicas-orange)]"
              />
              Đang dùng
            </label>

            <Button type="submit" fullWidth iconLeft={<Save size={17} />}>
              Lưu biến lương
            </Button>
          </form>
        </Card>
      </section>

      <Card
        title="Nguồn hệ thống"
        actions={<Database size={20} className="text-[var(--hicas-orange)]" />}
      >
        <DataTable
          columns={catalogColumns}
          data={catalogs}
          loading={loading}
          rowKey={(row) => row.id}
          className="border-0 shadow-none"
          emptyTitle="Chưa có nguồn hệ thống"
        />
      </Card>
    </div>
  );
};
