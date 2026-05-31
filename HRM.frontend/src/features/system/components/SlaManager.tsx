import type { ChangeEvent, FormEvent } from "react";
import { useMemo, useState } from "react";
import { Clock3, Save } from "lucide-react";
import { PageHeader } from "../../../components/layout";
import { Badge, Button, Card, DataTable } from "../../../components/ui";
import type { DataTableColumn } from "../../../components/ui";
import { useSalaryVariable } from "../hooks/useSalaryVariable";
import { useSla } from "../hooks/useSla";
import type { SlaConfig, SlaUpdateRequest } from "../types/sla";

type MessageState = {
  type: "success" | "error";
  text: string;
};

const defaultModules = [
  "LEAVE_APPROVAL",
  "CONTRACT_REVIEW",
  "PROFILE_CHANGE",
  "PAYROLL_CONFIRM",
  "RECRUITMENT_APPROVAL",
  "OVERTIME_APPROVAL",
];

export const SlaManager = () => {
  const { catalogs } = useSalaryVariable();
  const { slas, loading, updateSla } = useSla();
  const [formData, setFormData] = useState<SlaUpdateRequest>({
    moduleCode: "",
    value: "",
    unit: "HOURS",
  });
  const [message, setMessage] = useState<MessageState | null>(null);

  const availableModules = useMemo(() => {
    const catalogModules = catalogs
      .map((catalog) => catalog.module?.trim())
      .filter(Boolean)
      .map((module) => module.toUpperCase());
    return Array.from(new Set([...defaultModules, ...catalogModules]));
  }, [catalogs]);

  const handleInputChange = (event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = event.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    try {
      const res = await updateSla(formData);
      const responseMessage =
        typeof res === "object" && res !== null && "message" in res
          ? (res as { message?: string }).message
          : undefined;
      setMessage({
        type: "success",
        text: responseMessage || "Đã cập nhật thời hạn xử lý nghiệp vụ.",
      });
      setFormData({ moduleCode: "", value: "", unit: "HOURS" });
    } catch (error: unknown) {
      setMessage({ type: "error", text: String(error) });
    }
  };

  const columns: Array<DataTableColumn<SlaConfig>> = [
    {
      key: "code",
      header: "Mã quy trình",
      render: (row) => (
        <span className="font-mono text-sm font-semibold text-[var(--hicas-text-main)]">
          {row.code || (row as { moduleCode?: string }).moduleCode || "--"}
        </span>
      ),
    },
    {
      key: "value",
      header: "Thời hạn",
      render: (row) => (
        <span className="text-lg font-bold text-[var(--hicas-orange-dark)]">
          {row.value}
        </span>
      ),
    },
    {
      key: "unit",
      header: "Đơn vị",
      render: (row) => (
        <Badge variant={row.unit === "HOURS" ? "info" : "orange"}>
          {row.unit === "HOURS" ? "Giờ" : "Ngày"}
        </Badge>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        title="F0.2 Cấu hình thời hạn xử lý SLA"
        description="Thiết lập thời hạn xử lý cho các nghiệp vụ có phê duyệt như nghỉ phép, hợp đồng, hồ sơ, OT, tuyển dụng và chốt bảng lương."
        breadcrumb={[
          { label: "Module 0" },
          { label: "Cấu hình hệ thống" },
          { label: "SLA" },
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

      <section className="grid gap-6 xl:grid-cols-[420px_minmax(0,1fr)]">
        <Card
          title="Cập nhật SLA"
          description="Thời hạn được dùng cho cảnh báo quá hạn và dữ liệu đánh giá SLA."
        >
          <form onSubmit={handleSubmit} className="space-y-4">
            <label className="block">
              <span className="mb-2 block text-sm font-semibold">Quy trình *</span>
              <select
                required
                name="moduleCode"
                value={formData.moduleCode}
                onChange={handleInputChange}
                className="hicas-select w-full"
              >
                <option value="">Chọn quy trình</option>
                {availableModules.map((module) => (
                  <option key={module} value={module}>
                    {module}
                  </option>
                ))}
              </select>
            </label>

            <label className="block">
              <span className="mb-2 block text-sm font-semibold">Thời hạn *</span>
              <input
                required
                type="number"
                min="1"
                name="value"
                value={formData.value}
                onChange={handleInputChange}
                className="hicas-input w-full"
                placeholder="Ví dụ: 48"
              />
            </label>

            <label className="block">
              <span className="mb-2 block text-sm font-semibold">Đơn vị *</span>
              <select
                required
                name="unit"
                value={formData.unit}
                onChange={handleInputChange}
                className="hicas-select w-full"
              >
                <option value="HOURS">Giờ</option>
                <option value="DAYS">Ngày</option>
              </select>
            </label>

            <Button type="submit" fullWidth iconLeft={<Save size={17} />}>
              Cập nhật SLA
            </Button>
          </form>
        </Card>

        <Card
          title="Danh sách SLA hiện tại"
          description="Các cấu hình đang được hệ thống dùng để theo dõi trễ hạn xử lý."
          actions={<Clock3 size={20} className="text-[var(--hicas-orange)]" />}
        >
          <DataTable
            columns={columns}
            data={slas}
            loading={loading}
            rowKey={(row, index) => `${row.code}-${index}`}
            className="border-0 shadow-none"
            emptyTitle="Chưa có cấu hình SLA"
          />
        </Card>
      </section>
    </div>
  );
};
