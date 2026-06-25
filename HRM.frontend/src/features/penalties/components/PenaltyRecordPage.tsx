import { useMemo, useState } from "react";
import type { FormEvent } from "react";
import { ClipboardList, History, TimerReset } from "lucide-react";
import { Badge, Button, Card, DataTable, Select, Tabs } from "../../../components/ui";
import type { DataTableColumn } from "../../../components/ui";
import { FeaturePage } from "../../../core/components/FeatureShell";
import { usePenaltyRecords } from "../hooks/usePenaltyRecords";
import type { CreateManualPenaltyRecordRequest, PenaltyRecord, PenaltyTab } from "../types/penalty";

const statusOptions = [
  { value: "", label: "Tất cả trạng thái" },
  { value: "PendingEmployeeExplanation", label: "Chờ nhân viên giải trình" },
  { value: "PendingHRReview", label: "Chờ HR xem xét biên bản" },
  { value: "PendingDirectorApproval", label: "Chờ Giám đốc phê duyệt xử lý" },
  { value: "Approved", label: "Biên bản có hiệu lực" },
  { value: "Applied", label: "Đã áp dụng xử lý công" },
  { value: "Rejected", label: "Biên bản không hiệu lực" },
];

const violationTypeOptions = [
  { value: "Manual", label: "Thủ công" },
  { value: "LeftWorkplace", label: "Rời vị trí làm việc" },
  { value: "UnauthorizedAbsence", label: "Vắng mặt không phép" },
  { value: "LateArrival", label: "Đi muộn" },
  { value: "EarlyLeave", label: "Về sớm" },
  { value: "ProcessViolation", label: "Vi phạm quy trình" },
  { value: "SlaViolation", label: "Vi phạm SLA" },
];

const severityOptions = [
  { value: "Low", label: "Nhẹ" },
  { value: "Medium", label: "Trung bình" },
  { value: "High", label: "Nghiêm trọng" },
  { value: "Critical", label: "Rất nghiêm trọng" },
];

const statusLabel = (status: string) =>
  statusOptions.find((option) => option.value === status)?.label || status || "Không rõ";

const severityLabel = (severity: string) =>
  severityOptions.find((option) => option.value === severity)?.label || severity || "Không rõ";

const violationLabel = (violationType: string) =>
  violationTypeOptions.find((option) => option.value === violationType)?.label || violationType || "Không rõ";

const formatDate = (value?: string | null) =>
  value ? new Intl.DateTimeFormat("vi-VN", { dateStyle: "short", timeStyle: "short" }).format(new Date(value)) : "-";

const formatWorkImpact = (record: PenaltyRecord) => {
  const parts = [];
  if (record.deductedMinutes) parts.push(`${record.deductedMinutes} phút`);
  if (record.deductedWorkday) parts.push(`${record.deductedWorkday} công`);
  return parts.length > 0 ? parts.join(" / ") : "Không";
};

const createInitialForm = (): CreateManualPenaltyRecordRequest => ({
  employeeId: 0,
  occurredAt: new Date().toISOString().slice(0, 16),
  period: "",
  violationType: "Manual",
  severity: "Low",
  description: "",
  penaltyPoint: 0,
  requiresEmployeeExplanation: true,
  affectsAttendance: false,
  affectsPerformance: true,
  affectsPersonnelDecision: false,
  deductedMinutes: null,
  deductedWorkday: null,
  evidenceFilePath: "",
  managerNote: "",
  ruleCode: "",
});

const buildColumns = (): Array<DataTableColumn<PenaltyRecord>> => [
  {
    key: "employee",
    header: "Nhân sự",
    render: (record) => (
      <div>
        <p className="font-semibold text-[var(--hicas-text-main)]">{record.employeeName || `Employee #${record.employeeId}`}</p>
        <p className="text-xs text-[var(--hicas-text-secondary)]">{record.employeeCode || record.departmentName || ""}</p>
      </div>
    ),
  },
  { key: "occurredAt", header: "Thời điểm", render: (record) => formatDate(record.occurredAt) },
  { key: "type", header: "Loại vi phạm", render: (record) => violationLabel(record.violationType) },
  { key: "severity", header: "Mức độ", render: (record) => severityLabel(record.severity) },
  { key: "workImpact", header: "Điều chỉnh công", render: (record) => formatWorkImpact(record) },
  { key: "point", header: "Điểm trừ", render: (record) => record.penaltyPoint },
  {
    key: "impact",
    header: "Ảnh hưởng",
    render: (record) => (
      <div className="flex flex-wrap gap-1">
        {record.affectsAttendance && <Badge variant="warning">Công</Badge>}
        {record.affectsPerformance && <Badge variant="info">KPI</Badge>}
        {record.affectsPersonnelDecision && <Badge variant="danger">Hồ sơ</Badge>}
        {!record.affectsAttendance && !record.affectsPerformance && !record.affectsPersonnelDecision && (
          <Badge variant="neutral">Theo dõi</Badge>
        )}
      </div>
    ),
  },
  { key: "status", header: "Trạng thái", render: (record) => statusLabel(record.status) },
  {
    key: "reason",
    header: "Nội dung",
    render: (record) => <span className="line-clamp-2 text-[var(--hicas-text-secondary)]">{record.reason || "-"}</span>,
  },
];

export const PenaltyRecordPage = () => {
  const {
    records,
    historyRecords,
    loading,
    saving,
    status,
    setStatus,
    loadRecords,
    createManualRecord,
    loadEmployeeHistory,
  } = usePenaltyRecords();
  const [tab, setTab] = useState<PenaltyTab>("attendance");
  const [form, setForm] = useState<CreateManualPenaltyRecordRequest>(() => createInitialForm());
  const [historyEmployeeId, setHistoryEmployeeId] = useState("");

  const columns = useMemo(() => buildColumns(), []);
  const attendanceRecords = useMemo(
    () => records.filter((record) => record.affectsAttendance || record.sourceType === "Attendance"),
    [records],
  );

  const submitManualRecord = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const saved = await createManualRecord({
      ...form,
      occurredAt: new Date(form.occurredAt).toISOString(),
      period: form.period || null,
      deductedMinutes: form.affectsAttendance ? form.deductedMinutes || null : null,
      deductedWorkday: form.affectsAttendance ? form.deductedWorkday || null : null,
      evidenceFilePath: form.evidenceFilePath || null,
      managerNote: form.managerNote || null,
      ruleCode: form.ruleCode || null,
    });

    if (saved) {
      setForm(createInitialForm());
    }
  };

  const renderManualForm = () => (
    <Card title="Lập biên bản vi phạm" description="Manager/HR chỉ ghi nhận ảnh hưởng công, điểm KPI và hồ sơ. Không trừ tiền trực tiếp tại đây.">
      <form className="grid gap-4 md:grid-cols-2" onSubmit={submitManualRecord}>
        <label className="block">
          <span className="mb-2 block text-sm font-medium text-[var(--hicas-text-main)]">EmployeeId *</span>
          <input
            type="number"
            min={1}
            required
            className="hicas-input w-full"
            value={form.employeeId || ""}
            onChange={(event) => setForm((current) => ({ ...current, employeeId: Number(event.target.value) }))}
          />
        </label>
        <label className="block">
          <span className="mb-2 block text-sm font-medium text-[var(--hicas-text-main)]">Thời điểm vi phạm *</span>
          <input
            type="datetime-local"
            required
            className="hicas-input w-full"
            value={form.occurredAt}
            onChange={(event) => setForm((current) => ({ ...current, occurredAt: event.target.value }))}
          />
        </label>
        <Select
          label="Loại vi phạm"
          value={form.violationType}
          options={violationTypeOptions}
          onChange={(event) => setForm((current) => ({ ...current, violationType: event.target.value }))}
        />
        <Select
          label="Mức độ"
          value={form.severity}
          options={severityOptions}
          onChange={(event) => setForm((current) => ({ ...current, severity: event.target.value }))}
        />
        <label className="block">
          <span className="mb-2 block text-sm font-medium text-[var(--hicas-text-main)]">Kỳ đánh giá</span>
          <input
            className="hicas-input w-full"
            placeholder="MM-yyyy"
            value={form.period ?? ""}
            onChange={(event) => setForm((current) => ({ ...current, period: event.target.value }))}
          />
        </label>
        <label className="block">
          <span className="mb-2 block text-sm font-medium text-[var(--hicas-text-main)]">Điểm trừ</span>
          <input
            type="number"
            min={0}
            step={0.25}
            className="hicas-input w-full"
            value={form.penaltyPoint}
            onChange={(event) => setForm((current) => ({ ...current, penaltyPoint: Number(event.target.value) }))}
          />
        </label>
        <div className="grid gap-3 rounded-[var(--radius-lg)] border border-[var(--hicas-border)] p-4 text-sm md:col-span-2 md:grid-cols-3">
          <label className="flex items-center gap-2">
            <input
              type="checkbox"
              className="h-4 w-4 accent-[var(--hicas-orange)]"
              checked={form.affectsAttendance}
              onChange={(event) => setForm((current) => ({ ...current, affectsAttendance: event.target.checked }))}
            />
            Ảnh hưởng bảng công
          </label>
          <label className="flex items-center gap-2">
            <input
              type="checkbox"
              className="h-4 w-4 accent-[var(--hicas-orange)]"
              checked={form.affectsPerformance}
              onChange={(event) => setForm((current) => ({ ...current, affectsPerformance: event.target.checked }))}
            />
            Ảnh hưởng KPI
          </label>
          <label className="flex items-center gap-2">
            <input
              type="checkbox"
              className="h-4 w-4 accent-[var(--hicas-orange)]"
              checked={form.affectsPersonnelDecision}
              onChange={(event) => setForm((current) => ({ ...current, affectsPersonnelDecision: event.target.checked }))}
            />
            Ghi nhận vào hồ sơ nhân sự
          </label>
        </div>
        <label className="block">
          <span className="mb-2 block text-sm font-medium text-[var(--hicas-text-main)]">Phút điều chỉnh công</span>
          <input
            type="number"
            min={0}
            className="hicas-input w-full"
            disabled={!form.affectsAttendance}
            value={form.deductedMinutes ?? ""}
            onChange={(event) => setForm((current) => ({ ...current, deductedMinutes: Number(event.target.value) }))}
          />
        </label>
        <label className="block">
          <span className="mb-2 block text-sm font-medium text-[var(--hicas-text-main)]">Công điều chỉnh</span>
          <input
            type="number"
            min={0}
            step={0.25}
            className="hicas-input w-full"
            disabled={!form.affectsAttendance}
            value={form.deductedWorkday ?? ""}
            onChange={(event) => setForm((current) => ({ ...current, deductedWorkday: Number(event.target.value) }))}
          />
        </label>
        <label className="block md:col-span-2">
          <span className="mb-2 block text-sm font-medium text-[var(--hicas-text-main)]">Mô tả *</span>
          <textarea
            required
            className="hicas-input min-h-[112px] w-full py-3"
            value={form.description}
            onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
            placeholder="VD: Nhân viên rời vị trí làm việc trong ca, có xác nhận từ quản lý trực tiếp."
          />
        </label>
        <label className="block md:col-span-2">
          <span className="mb-2 block text-sm font-medium text-[var(--hicas-text-main)]">Ghi chú quản lý</span>
          <textarea
            className="hicas-input min-h-[88px] w-full py-3"
            value={form.managerNote ?? ""}
            onChange={(event) => setForm((current) => ({ ...current, managerNote: event.target.value }))}
          />
        </label>
        <div className="md:col-span-2">
          <Button type="submit" isLoading={saving}>
            Ghi nhận biên bản
          </Button>
        </div>
      </form>
    </Card>
  );

  return (
    <FeaturePage
      title="Quản lý vi phạm, điểm trừ và điều chỉnh công"
      description="Theo dõi vi phạm, điểm trừ và điều chỉnh công để phục vụ đánh giá, bảng công và quyết định nhân sự."
      actions={
        <Button variant="secondary" onClick={() => loadRecords()}>
          Tải lại
        </Button>
      }
    >
      <Card>
        <Tabs
          value={tab}
          onChange={(value) => setTab(value as PenaltyTab)}
          items={[
            { value: "attendance", label: <span className="inline-flex items-center gap-2"><TimerReset size={16} />Vi phạm & điều chỉnh công</span> },
            { value: "manual", label: <span className="inline-flex items-center gap-2"><ClipboardList size={16} />Biên bản vi phạm</span> },
            { value: "history", label: <span className="inline-flex items-center gap-2"><History size={16} />Lịch sử điểm trừ</span> },
          ]}
        />
      </Card>

      {tab !== "history" && (
        <Card title="Bộ lọc">
          <Select
            label="Trạng thái"
            value={status}
            options={statusOptions}
            onChange={(event) => setStatus(event.target.value)}
          />
        </Card>
      )}

      {tab === "attendance" && (
        <DataTable
          columns={columns}
          data={attendanceRecords}
          loading={loading}
          rowKey={(row) => row.id}
          emptyTitle="Chưa có vi phạm ảnh hưởng bảng công"
          emptyDescription="Các lỗi đi muộn, về sớm, vắng mặt hoặc rời vị trí sẽ xuất hiện tại đây sau khi phát sinh."
        />
      )}

      {tab === "manual" && (
        <>
          {renderManualForm()}
          <DataTable
            columns={columns}
            data={records}
            loading={loading}
            rowKey={(row) => row.id}
            emptyTitle="Chưa có biên bản vi phạm"
          />
        </>
      )}

      {tab === "history" && (
        <>
          <Card title="Tra cứu lịch sử điểm trừ">
            <div className="flex flex-col gap-3 sm:flex-row">
              <input
                type="number"
                min={1}
                className="hicas-input min-w-[240px]"
                placeholder="Nhập EmployeeId"
                value={historyEmployeeId}
                onChange={(event) => setHistoryEmployeeId(event.target.value)}
              />
              <Button onClick={() => loadEmployeeHistory(Number(historyEmployeeId))}>Tra cứu</Button>
            </div>
          </Card>
          <DataTable
            columns={columns}
            data={historyRecords}
            loading={loading}
            rowKey={(row) => row.id}
            emptyTitle="Chưa có dữ liệu lịch sử điểm trừ"
          />
        </>
      )}
    </FeaturePage>
  );
};
