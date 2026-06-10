import type { ChangeEvent, FormEvent } from "react";
import { useMemo, useState } from "react";
import { Clock3, Power, PowerOff, Save } from "lucide-react";
import { PageHeader } from "../../../components/layout";
import { Badge, Button, Card, DataTable } from "../../../components/ui";
import type { DataTableColumn } from "../../../components/ui";
import { useSla } from "../hooks/useSla";
import type { SlaConfig, SlaUpdateRequest } from "../types/sla";

type MessageState = {
  type: "success" | "error";
  text: string;
};

export const SlaManager = () => {
  const { slas, loading, updateSla, setSlaActive } = useSla();
  const [formData, setFormData] = useState<SlaUpdateRequest>({
    moduleCode: "",
    value: "",
    unit: "HOURS",
  });
  const [message, setMessage] = useState<MessageState | null>(null);
  const [togglingCode, setTogglingCode] = useState<string | null>(null);

  const availableProcesses = useMemo(
    () =>
      [...slas].sort((a, b) =>
        `${a.moduleName}-${a.displayName}`.localeCompare(
          `${b.moduleName}-${b.displayName}`,
          "vi",
        ),
      ),
    [slas],
  );

  const handleInputChange = (event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = event.target;

    if (name === "moduleCode") {
      const selected = slas.find((item) => item.moduleCode === value || item.code === value);
      setFormData({
        moduleCode: value,
        value: selected?.value ?? "",
        unit: selected?.unit ?? "HOURS",
      });
      return;
    }

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
        text: responseMessage || "Đã cập nhật thời hạn xử lý SLA.",
      });
      setFormData({ moduleCode: "", value: "", unit: "HOURS" });
    } catch (error: unknown) {
      setMessage({ type: "error", text: String(error) });
    }
  };

  const handleToggle = async (row: SlaConfig) => {
    const code = row.moduleCode || row.code;
    setTogglingCode(code);
    try {
      const res = await setSlaActive(code, !row.isActive);
      const responseMessage =
        typeof res === "object" && res !== null && "message" in res
          ? (res as { message?: string }).message
          : undefined;
      setMessage({
        type: "success",
        text: responseMessage || "Đã cập nhật trạng thái SLA.",
      });
    } catch (error: unknown) {
      setMessage({ type: "error", text: String(error) });
    } finally {
      setTogglingCode(null);
    }
  };

  const columns: Array<DataTableColumn<SlaConfig>> = [
    {
      key: "process",
      header: "Quy trình",
      render: (row) => (
        <div className="min-w-[260px] space-y-1">
          <div className="font-semibold text-[var(--hicas-text-main)]">{row.displayName}</div>
          <div className="text-xs leading-5 text-[var(--hicas-text-muted)]">
            {row.description}
          </div>
        </div>
      ),
    },
    {
      key: "moduleName",
      header: "Phân hệ",
      render: (row) => <Badge variant="neutral">{row.moduleName}</Badge>,
    },
    {
      key: "deadline",
      header: "Thời hạn",
      render: (row) => (
        <div className="flex items-center gap-2">
          <span className="text-lg font-bold text-[var(--hicas-orange-dark)]">{row.value}</span>
          <Badge variant={row.unit === "HOURS" ? "info" : "orange"}>
            {row.unit === "HOURS" ? "Giờ" : "Ngày"}
          </Badge>
        </div>
      ),
    },
    {
      key: "status",
      header: "Trạng thái",
      render: (row) => (
        <Badge variant={row.isActive ? "success" : "neutral"}>
          {row.isActive ? "Đang áp dụng" : "Tạm tắt"}
        </Badge>
      ),
    },
    {
      key: "actions",
      header: "Thao tác",
      render: (row) => {
        const code = row.moduleCode || row.code;
        return (
          <Button
            size="sm"
            variant={row.isActive ? "ghost" : "secondary"}
            iconLeft={row.isActive ? <PowerOff size={15} /> : <Power size={15} />}
            isLoading={togglingCode === code}
            onClick={() => handleToggle(row)}
          >
            {row.isActive ? "Tắt" : "Bật"}
          </Button>
        );
      },
    },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        title="Thời hạn xử lý"
        description="Điều chỉnh thời hạn xử lý cho các quy trình nhân sự đang áp dụng."
        breadcrumb={[
          { label: "Cấu hình hệ thống" },
          { label: "Thời hạn xử lý" },
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
          description="Chọn quy trình, đặt thời hạn và bật trạng thái theo dõi."
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
                {availableProcesses.map((process) => (
                  <option key={process.code} value={process.moduleCode || process.code}>
                    {process.moduleName} - {process.displayName}
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
          description="Theo dõi các thời hạn đang áp dụng cho từng quy trình."
          actions={<Clock3 size={20} className="text-[var(--hicas-orange)]" />}
        >
          <DataTable
            columns={columns}
            data={slas}
            loading={loading}
            rowKey={(row, index) => `${row.code || row.moduleCode}-${index}`}
            className="border-0 shadow-none"
            emptyTitle="Chưa có cấu hình SLA"
          />
        </Card>
      </section>
    </div>
  );
};
