import type { ChangeEvent, FormEvent } from "react";
import { useState } from "react";
import { Database, Plus, Save } from "lucide-react";
import { PageHeader } from "../../../components/layout";
import { Badge, Button, Card, DataTable } from "../../../components/ui";
import type { DataTableColumn } from "../../../components/ui";
import { useSalaryVariable } from "../hooks/useSalaryVariable";
import type {
  CreateSourceCatalogPayload,
  SalaryVariable,
  SourceCatalogItem,
} from "../types/salaryVariable";

const emptyVariable: SalaryVariable = {
  code: "",
  source: "",
  description: "",
};

const emptyCatalog: CreateSourceCatalogPayload = {
  displayName: "",
  sourcePath: "",
  module: "Payroll",
  dataType: "Decimal",
  aggregationType: "Sum",
  isPeriodBased: true,
  isActive: true,
};

type MessageState = {
  type: "success" | "error";
  text: string;
};

export const SalaryVariableManager = () => {
  const { variables, catalogs, loading, defineVariable, createCatalog } =
    useSalaryVariable();
  const [variableForm, setVariableForm] = useState<SalaryVariable>(emptyVariable);
  const [catalogForm, setCatalogForm] =
    useState<CreateSourceCatalogPayload>(emptyCatalog);
  const [message, setMessage] = useState<MessageState | null>(null);

  const handleVariableChange = (
    event: ChangeEvent<HTMLInputElement | HTMLSelectElement>,
  ) => {
    const { name, value } = event.target;
    setVariableForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleCatalogChange = (
    event: ChangeEvent<HTMLInputElement | HTMLSelectElement>,
  ) => {
    const { name, value, type } = event.target;
    const checked = type === "checkbox" ? event.target.checked : undefined;

    setCatalogForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const handleVariableSubmit = async (event: FormEvent) => {
    event.preventDefault();
    try {
      const res = await defineVariable({
        ...variableForm,
        code: variableForm.code.trim().toUpperCase(),
      });
      setMessage({ type: "success", text: res.message || "Đã lưu biến lương." });
      setVariableForm(emptyVariable);
    } catch (error: unknown) {
      setMessage({ type: "error", text: String(error) });
    }
  };

  const handleCatalogSubmit = async (event: FormEvent) => {
    event.preventDefault();
    try {
      const res = await createCatalog(catalogForm);
      setMessage({
        type: "success",
        text: res.message || "Đã thêm nguồn dữ liệu lương.",
      });
      setCatalogForm(emptyCatalog);
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
  ];

  const catalogColumns: Array<DataTableColumn<SourceCatalogItem>> = [
    {
      key: "displayName",
      header: "Tên nguồn",
      render: (row) => (
        <div>
          <p className="font-semibold text-[var(--hicas-text-main)]">{row.displayName}</p>
          <p className="mt-1 font-mono text-xs text-[var(--hicas-text-secondary)]">
            {row.sourcePath}
          </p>
        </div>
      ),
    },
    { key: "module", header: "Module", render: (row) => row.module },
    {
      key: "dataType",
      header: "Kiểu dữ liệu",
      render: (row) => `${row.dataType} / ${row.aggregationType}`,
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
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        title="F0.1 Cấu hình biến lương"
        description="Quản lý biến đầu vào và nguồn dữ liệu whitelist để HR dùng trong công thức lương mà không cần can thiệp mã nguồn."
        breadcrumb={[
          { label: "Module 0" },
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
          description="Các biến này sẽ xuất hiện trong Formula Builder của Module 6."
          actions={
            <Badge variant="orange">
              {variables.length} biến / {catalogs.length} nguồn
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
            emptyDescription="Hãy thêm biến đầu tiên để dùng trong công thức lương."
          />
        </Card>

        <Card title="Thêm biến lương" description="Ánh xạ mã biến với một nguồn dữ liệu đã cấu hình.">
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
                  Chọn source catalog
                </option>
                {catalogs
                  .filter((item) => item.isActive)
                  .map((item) => (
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

            <Button type="submit" fullWidth iconLeft={<Save size={17} />}>
              Lưu biến lương
            </Button>
          </form>
        </Card>
      </section>

      <section className="grid gap-6 xl:grid-cols-[420px_minmax(0,1fr)]">
        <Card
          title="Thêm nguồn dữ liệu"
          description="Source catalog là danh sách dữ liệu được phép dùng trong công thức."
        >
          <form onSubmit={handleCatalogSubmit} className="space-y-4">
            <label className="block">
              <span className="mb-2 block text-sm font-semibold">Tên hiển thị *</span>
              <input
                required
                name="displayName"
                value={catalogForm.displayName}
                onChange={handleCatalogChange}
                className="hicas-input w-full"
                placeholder="Số giờ OT hợp lệ"
              />
            </label>

            <label className="block">
              <span className="mb-2 block text-sm font-semibold">Source path *</span>
              <input
                required
                name="sourcePath"
                value={catalogForm.sourcePath}
                onChange={handleCatalogChange}
                className="hicas-input w-full font-mono"
                placeholder="Overtime.ActualOtMinutes"
              />
            </label>

            <div className="grid gap-4 sm:grid-cols-2">
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Module</span>
                <input
                  name="module"
                  value={catalogForm.module}
                  onChange={handleCatalogChange}
                  className="hicas-input w-full"
                />
              </label>
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Kiểu dữ liệu</span>
                <select
                  name="dataType"
                  value={catalogForm.dataType}
                  onChange={handleCatalogChange}
                  className="hicas-select w-full"
                >
                  <option value="Decimal">Decimal</option>
                  <option value="Integer">Integer</option>
                  <option value="Boolean">Boolean</option>
                  <option value="Date">Date</option>
                </select>
              </label>
            </div>

            <label className="block">
              <span className="mb-2 block text-sm font-semibold">Cách tổng hợp</span>
              <select
                name="aggregationType"
                value={catalogForm.aggregationType}
                onChange={handleCatalogChange}
                className="hicas-select w-full"
              >
                <option value="Sum">Sum</option>
                <option value="Average">Average</option>
                <option value="Latest">Latest</option>
                <option value="Count">Count</option>
                <option value="None">None</option>
              </select>
            </label>

            <div className="grid gap-3 sm:grid-cols-2">
              <label className="flex items-center gap-2 text-sm font-medium">
                <input
                  type="checkbox"
                  name="isPeriodBased"
                  checked={catalogForm.isPeriodBased}
                  onChange={handleCatalogChange}
                  className="accent-[var(--hicas-orange)]"
                />
                Theo kỳ lương
              </label>
              <label className="flex items-center gap-2 text-sm font-medium">
                <input
                  type="checkbox"
                  name="isActive"
                  checked={catalogForm.isActive}
                  onChange={handleCatalogChange}
                  className="accent-[var(--hicas-orange)]"
                />
                Đang dùng
              </label>
            </div>

            <Button type="submit" fullWidth variant="secondary" iconLeft={<Plus size={17} />}>
              Thêm source catalog
            </Button>
          </form>
        </Card>

        <Card
          title="Source catalog"
          description="Nguồn dữ liệu có thể được ánh xạ vào biến lương."
          actions={<Database size={20} className="text-[var(--hicas-orange)]" />}
        >
          <DataTable
            columns={catalogColumns}
            data={catalogs}
            loading={loading}
            rowKey={(row) => row.id}
            className="border-0 shadow-none"
            emptyTitle="Chưa có source catalog"
          />
        </Card>
      </section>
    </div>
  );
};
