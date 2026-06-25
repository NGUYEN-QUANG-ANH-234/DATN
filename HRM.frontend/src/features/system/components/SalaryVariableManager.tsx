import type { ChangeEvent, FormEvent } from "react";
import { useState } from "react";
import { Power, Save, Trash2 } from "lucide-react";
import { PageHeader } from "../../../components/layout";
import { Badge, Button, Card, DataTable, Tabs } from "../../../components/ui";
import type { DataTableColumn } from "../../../components/ui";
import { useSalaryVariable } from "../hooks/useSalaryVariable";
import type { SalaryVariable } from "../types/salaryVariable";

const emptyVariable: SalaryVariable = {
  code: "",
  source: "",
  description: "",
  isActive: true,
};

type MessageState = {
  type: "success" | "error";
  text: string;
};

export const SalaryVariableManager = () => {
  const { variables, catalogs, loading, defineVariable, setVariableActive, deleteVariable } =
    useSalaryVariable();
  const [variableForm, setVariableForm] = useState<SalaryVariable>(emptyVariable);
  const [message, setMessage] = useState<MessageState | null>(null);
  const [variableTab, setVariableTab] = useState<"active" | "inactive">("active");

  const activeCatalogs = catalogs.filter((item) => item.isActive);
  const activeVariables = variables.filter((item) => item.isActive);
  const inactiveVariables = variables.filter((item) => !item.isActive);
  const visibleVariables = variableTab === "active" ? activeVariables : inactiveVariables;

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
      setVariableTab("active");
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

  const handleDeleteVariable = async (row: SalaryVariable) => {
    const confirmed = window.confirm(
      `Xóa hẳn biến lương "${row.code}"? Thao tác này không thể hoàn tác.`,
    );
    if (!confirmed) return;

    try {
      const res = await deleteVariable(row.code);
      setMessage({
        type: "success",
        text: res.message || "Đã xóa biến lương.",
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
      render: (row) => (
        <span className="break-all font-mono text-xs text-[var(--hicas-text-secondary)]">
          {row.source}
        </span>
      ),
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

  const variableTableColumns: Array<DataTableColumn<SalaryVariable>> = variableColumns.map((column) =>
    column.key !== "actions"
      ? column
      : {
          ...column,
          render: (row) => (
            <div className="flex flex-wrap justify-end gap-2">
              <Button
                size="sm"
                variant={row.isActive ? "ghost" : "secondary"}
                iconLeft={<Power size={15} />}
                onClick={() => handleToggleVariable(row)}
              >
                {row.isActive ? "Tắt" : "Bật"}
              </Button>
              {!row.isActive && (
                <Button
                  size="sm"
                  variant="danger"
                  iconLeft={<Trash2 size={15} />}
                  onClick={() => handleDeleteVariable(row)}
                >
                  Xóa hẳn
                </Button>
              )}
            </div>
          ),
        },
  );

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

      <section className="grid gap-4 md:grid-cols-3">
        <div className="rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-white p-5 shadow-sm">
          <p className="text-sm font-semibold text-[var(--hicas-text-secondary)]">Đang dùng</p>
          <p className="mt-2 text-3xl font-bold text-[var(--hicas-text-main)]">
            {activeVariables.length}
          </p>
          <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">biến lương</p>
        </div>
        <div className="rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-white p-5 shadow-sm">
          <p className="text-sm font-semibold text-[var(--hicas-text-secondary)]">Tạm tắt</p>
          <p className="mt-2 text-3xl font-bold text-[var(--hicas-text-main)]">
            {inactiveVariables.length}
          </p>
          <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">biến lương</p>
        </div>
        <div className="rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-white p-5 shadow-sm">
          <p className="text-sm font-semibold text-[var(--hicas-text-secondary)]">Nguồn khả dụng</p>
          <p className="mt-2 text-3xl font-bold text-[var(--hicas-orange-dark)]">
            {activeCatalogs.length}
          </p>
          <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">nguồn dữ liệu</p>
        </div>
      </section>

      <Card
        title="Thêm biến lương"
        description="Chọn nguồn có sẵn, đặt mã biến và mô tả ngắn để dùng trong công thức lương."
      >
        <form onSubmit={handleVariableSubmit} className="grid gap-4 lg:grid-cols-[220px_minmax(260px,1fr)_minmax(260px,1fr)_150px] lg:items-end">
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

          <div className="space-y-3">
            <label className="flex h-5 items-center gap-2 text-sm font-medium">
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
              Lưu
            </Button>
          </div>
        </form>
      </Card>

      <Card
        title="Danh sách biến lương"
        actions={
          <Badge variant="orange">
            {variables.length} biến / {activeCatalogs.length} nguồn đang dùng
          </Badge>
        }
      >
        <div className="mb-4">
          <Tabs
            value={variableTab}
            onChange={(value) => setVariableTab(value as "active" | "inactive")}
            items={[
              {
                value: "active",
                label: "Đang dùng",
                badge: <Badge variant="success">{activeVariables.length}</Badge>,
              },
              {
                value: "inactive",
                label: "Tạm tắt",
                badge: <Badge variant="neutral">{inactiveVariables.length}</Badge>,
              },
            ]}
          />
        </div>
        <div className="max-h-[520px] overflow-y-auto pr-1">
          <DataTable
            columns={variableTableColumns}
            data={visibleVariables}
            loading={loading}
            rowKey={(row, index) => `${row.code}-${index}`}
            className="border-0 shadow-none"
            emptyTitle="Chưa có biến lương"
          />
        </div>
      </Card>

    </div>
  );
};
