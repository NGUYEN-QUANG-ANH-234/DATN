import type { ChangeEvent, FormEvent } from "react";
import { useEffect, useMemo, useState } from "react";
import { CalendarDays, Download, Plus, RefreshCw, Save, Trash2, Upload } from "lucide-react";
import { PageHeader } from "../../../components/layout";
import { Badge, Button, Card } from "../../../components/ui";
import { companyCalendarApi } from "../api/companyCalendarApi";
import type {
  CompanyCalendar,
  CompanyCalendarDay,
  CompanyCalendarDayType,
  PolicyVersionStatus,
  SaveCompanyCalendarPayload,
} from "../types/companyCalendar";

const today = new Date();
const currentYear = today.getFullYear();

const dayTypeOptions: { value: number; key: string; label: string }[] = [
  { value: 0, key: "PublicHoliday", label: "Ngày lễ" },
  { value: 1, key: "CompanyHoliday", label: "Nghỉ công ty" },
  { value: 2, key: "CompensatoryWorkingDay", label: "Làm bù" },
  { value: 3, key: "CompensatoryDayOff", label: "Nghỉ bù" },
  { value: 4, key: "SpecialPaidLeave", label: "Nghỉ hưởng lương" },
  { value: 5, key: "UnpaidCompanyClosure", label: "Nghỉ không lương" },
];

type CalendarDayForm = Omit<CompanyCalendarDay, "id">;

type MessageState = {
  type: "success" | "error";
  text: string;
};

const blankDay = (year = currentYear): CalendarDayForm => ({
  date: `${year}-01-01`,
  dayType: 0,
  name: "",
  isPaid: true,
  isOvertimeHoliday: true,
  isWorkingDayOverride: false,
  description: "",
});

const toDateInput = (value?: string | null) => (value ? value.slice(0, 10) : "");

const normalizeStatus = (status: PolicyVersionStatus): "Draft" | "Active" | "Archived" => {
  if (status === 0 || status === "Draft") return "Draft";
  if (status === 2 || status === "Archived") return "Archived";
  return "Active";
};

const statusValue = (status: PolicyVersionStatus): 0 | 1 | 2 => {
  const normalized = normalizeStatus(status);
  if (normalized === "Draft") return 0;
  if (normalized === "Archived") return 2;
  return 1;
};

const statusLabel = (status: PolicyVersionStatus) => {
  const normalized = normalizeStatus(status);
  if (normalized === "Draft") return "Bản nháp";
  if (normalized === "Archived") return "Lưu trữ";
  return "Đang áp dụng";
};

const dayTypeValue = (type: CompanyCalendarDayType): CompanyCalendarDayType => {
  if (typeof type === "number") return type as CompanyCalendarDayType;
  return (dayTypeOptions.find((item) => item.key === type)?.value ?? 0) as CompanyCalendarDayType;
};

const dayTypeLabel = (type: CompanyCalendarDayType) =>
  dayTypeOptions.find((item) => item.value === dayTypeValue(type))?.label ?? "Khác";

const mapCalendarToPayload = (calendar: CompanyCalendar): SaveCompanyCalendarPayload => ({
  id: calendar.id,
  versionCode: calendar.versionCode,
  effectiveFrom: toDateInput(calendar.effectiveFrom),
  effectiveTo: toDateInput(calendar.effectiveTo),
  status: statusValue(calendar.status),
  sourceRef: calendar.sourceRef,
  note: calendar.note,
  days: calendar.days.map((day) => ({
    date: toDateInput(day.date),
    dayType: dayTypeValue(day.dayType),
    name: day.name,
    isPaid: day.isPaid,
    isOvertimeHoliday: day.isOvertimeHoliday,
    isWorkingDayOverride: day.isWorkingDayOverride,
    description: day.description,
  })),
});

const buildDefaultVersionCode = (year: number) => {
  const stamp = new Date().toISOString().replace(/\D/g, "").slice(0, 14);
  return `VN_COMPANY_CALENDAR_${year}_${stamp}`;
};

const buildVietnamHolidayPreset = (year: number): CalendarDayForm[] => [
  {
    ...blankDay(year),
    date: `${year}-01-01`,
    name: "Tết Dương lịch",
    dayType: 0,
  },
  {
    ...blankDay(year),
    date: `${year}-04-30`,
    name: "Ngày Giải phóng miền Nam",
    dayType: 0,
  },
  {
    ...blankDay(year),
    date: `${year}-05-01`,
    name: "Ngày Quốc tế Lao động",
    dayType: 0,
  },
  {
    ...blankDay(year),
    date: `${year}-09-02`,
    name: "Ngày Quốc khánh",
    dayType: 0,
  },
];

export const CompanyCalendarManager = () => {
  const [year, setYear] = useState(currentYear);
  const [calendars, setCalendars] = useState<CompanyCalendar[]>([]);
  const [selectedId, setSelectedId] = useState<number | "new">("new");
  const [payload, setPayload] = useState<SaveCompanyCalendarPayload>({
    id: null,
    versionCode: buildDefaultVersionCode(currentYear),
    effectiveFrom: `${currentYear}-01-01`,
    effectiveTo: null,
    status: 1,
    sourceRef: "Admin",
    note: "",
    days: [],
  });
  const [dayForm, setDayForm] = useState<CalendarDayForm>(blankDay(currentYear));
  const [rangeEnd, setRangeEnd] = useState("");
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<MessageState | null>(null);

  const sortedDays = useMemo(
    () => [...payload.days].sort((a, b) => a.date.localeCompare(b.date)),
    [payload.days],
  );

  const activeCalendar = calendars.find((calendar) => normalizeStatus(calendar.status) === "Active");

  const load = async (targetYear = year) => {
    setLoading(true);
    setMessage(null);
    try {
      const res = await companyCalendarApi.getByYear(targetYear);
      setCalendars(res.data);
      const preferred = res.data.find((calendar) => normalizeStatus(calendar.status) === "Active") ?? res.data[0];
      if (preferred) {
        setSelectedId(preferred.id);
        setPayload(mapCalendarToPayload(preferred));
      } else {
        setSelectedId("new");
        setPayload({
          id: null,
          versionCode: buildDefaultVersionCode(targetYear),
          effectiveFrom: `${targetYear}-01-01`,
          effectiveTo: null,
          status: 1,
          sourceRef: "Admin",
          note: "",
          days: [],
        });
      }
    } catch (error) {
      setMessage({
        type: "error",
        text: error instanceof Error ? error.message : "Không thể tải lịch nghỉ công ty.",
      });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load(year);
  }, [year]);

  const handleSelectVersion = (value: string) => {
    if (value === "new") {
      setSelectedId("new");
      setPayload({
        id: null,
        versionCode: buildDefaultVersionCode(year),
        effectiveFrom: `${year}-01-01`,
        effectiveTo: null,
        status: 1,
        sourceRef: "Admin",
        note: "",
        days: [],
      });
      return;
    }

    const id = Number(value);
    const calendar = calendars.find((item) => item.id === id);
    if (!calendar) return;

    setSelectedId(id);
    setPayload(mapCalendarToPayload(calendar));
  };

  const handleAddDays = (event: FormEvent) => {
    event.preventDefault();
    if (!dayForm.name.trim()) {
      setMessage({ type: "error", text: "Vui lòng nhập tên ngày nghỉ." });
      return;
    }

    const start = new Date(`${dayForm.date}T00:00:00`);
    const end = rangeEnd ? new Date(`${rangeEnd}T00:00:00`) : start;
    if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime()) || end < start) {
      setMessage({ type: "error", text: "Khoảng ngày không hợp lệ." });
      return;
    }

    const newDays: CalendarDayForm[] = [];
    for (let date = new Date(start); date <= end; date.setDate(date.getDate() + 1)) {
      const dateText = date.toISOString().slice(0, 10);
      if (date.getFullYear() !== year) continue;
      newDays.push({
        ...dayForm,
        date: dateText,
        name: newDays.length === 0 ? dayForm.name.trim() : `${dayForm.name.trim()} (${newDays.length + 1})`,
      });
    }

    setPayload((prev) => {
      const byDate = new Map(prev.days.map((day) => [day.date, day]));
      for (const day of newDays) byDate.set(day.date, day);
      return { ...prev, days: [...byDate.values()] };
    });
    setDayForm(blankDay(year));
    setRangeEnd("");
    setMessage(null);
  };

  const handleRemoveDay = (date: string) => {
    setPayload((prev) => ({ ...prev, days: prev.days.filter((day) => day.date !== date) }));
  };

  const handlePreset = () => {
    setPayload((prev) => {
      const byDate = new Map(prev.days.map((day) => [day.date, day]));
      for (const day of buildVietnamHolidayPreset(year)) byDate.set(day.date, day);
      return { ...prev, days: [...byDate.values()] };
    });
  };

  const handleSave = async () => {
    setSaving(true);
    setMessage(null);
    try {
      const res = await companyCalendarApi.save(year, payload);
      setMessage({ type: "success", text: res.message || "Đã lưu lịch nghỉ công ty." });
      await load(year);
      setSelectedId(res.data.id);
    } catch (error) {
      setMessage({
        type: "error",
        text: error instanceof Error ? error.message : "Không thể lưu lịch nghỉ công ty.",
      });
    } finally {
      setSaving(false);
    }
  };

  const handleExport = () => {
    const file = new Blob([JSON.stringify({ year, ...payload }, null, 2)], {
      type: "application/json",
    });
    const url = URL.createObjectURL(file);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = `lich-nghi-cong-ty-${year}.json`;
    anchor.click();
    URL.revokeObjectURL(url);
  };

  const handleImport = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    try {
      const text = await file.text();
      const imported = JSON.parse(text) as Partial<SaveCompanyCalendarPayload> & { year?: number };
      if (imported.year && imported.year !== year) {
        setMessage({ type: "error", text: "File nhập không khớp với năm đang chọn." });
        return;
      }

      setPayload((prev) => ({
        ...prev,
        versionCode: imported.versionCode ?? prev.versionCode,
        effectiveFrom: imported.effectiveFrom ?? prev.effectiveFrom,
        effectiveTo: imported.effectiveTo ?? prev.effectiveTo,
        status: typeof imported.status === "undefined" ? prev.status : statusValue(imported.status),
        sourceRef: imported.sourceRef ?? prev.sourceRef,
        note: imported.note ?? prev.note,
        days: Array.isArray(imported.days)
          ? imported.days.map((day) => ({ ...day, dayType: dayTypeValue(day.dayType) }))
          : prev.days,
      }));
      setMessage({ type: "success", text: "Đã nhập dữ liệu lịch từ file." });
    } catch {
      setMessage({ type: "error", text: "File lịch không hợp lệ." });
    } finally {
      event.target.value = "";
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="Lịch nghỉ công ty"
        description="Thiết lập ngày nghỉ và ngày làm bù dùng chung cho chấm công, nghỉ phép và tính lương."
        breadcrumb={[
          { label: "Cấu hình hệ thống" },
          { label: "Lịch nghỉ công ty" },
        ]}
        actions={
          <Button
            variant="secondary"
            iconLeft={<RefreshCw size={16} />}
            onClick={() => void load(year)}
            isLoading={loading}
          >
            Làm mới
          </Button>
        }
      />

      <div className="grid gap-6 xl:grid-cols-[minmax(0,420px)_1fr]">
        <Card
          title="Cấu hình năm"
          description="Chọn năm, version và trạng thái áp dụng."
          actions={<CalendarDays size={20} className="text-[var(--hicas-orange)]" />}
        >
          <div className="space-y-4">
            <label className="block text-sm font-semibold text-[var(--hicas-text-main)]">
              Năm
              <input
                type="number"
                min={2000}
                max={2100}
                value={year}
                onChange={(event) => setYear(Number(event.target.value))}
                className="mt-2 h-11 w-full rounded-[var(--radius-md)] border border-[var(--hicas-border)] px-3 text-sm"
              />
            </label>

            <label className="block text-sm font-semibold text-[var(--hicas-text-main)]">
              Phiên bản
              <select
                value={selectedId}
                onChange={(event) => handleSelectVersion(event.target.value)}
                className="mt-2 h-11 w-full rounded-[var(--radius-md)] border border-[var(--hicas-border)] px-3 text-sm"
              >
                <option value="new">Tạo phiên bản mới</option>
                {calendars.map((calendar) => (
                  <option key={calendar.id} value={calendar.id}>
                    {calendar.versionCode} - {statusLabel(calendar.status)}
                  </option>
                ))}
              </select>
            </label>

            <label className="block text-sm font-semibold text-[var(--hicas-text-main)]">
              Mã phiên bản
              <input
                value={payload.versionCode ?? ""}
                onChange={(event) =>
                  setPayload((prev) => ({ ...prev, versionCode: event.target.value }))
                }
                className="mt-2 h-11 w-full rounded-[var(--radius-md)] border border-[var(--hicas-border)] px-3 text-sm"
              />
            </label>

            <div className="grid gap-3 sm:grid-cols-2">
              <label className="block text-sm font-semibold text-[var(--hicas-text-main)]">
                Hiệu lực từ
                <input
                  type="date"
                  value={toDateInput(payload.effectiveFrom)}
                  onChange={(event) =>
                    setPayload((prev) => ({ ...prev, effectiveFrom: event.target.value }))
                  }
                  className="mt-2 h-11 w-full rounded-[var(--radius-md)] border border-[var(--hicas-border)] px-3 text-sm"
                />
              </label>

              <label className="block text-sm font-semibold text-[var(--hicas-text-main)]">
                Hiệu lực đến
                <input
                  type="date"
                  value={toDateInput(payload.effectiveTo)}
                  onChange={(event) =>
                    setPayload((prev) => ({ ...prev, effectiveTo: event.target.value || null }))
                  }
                  className="mt-2 h-11 w-full rounded-[var(--radius-md)] border border-[var(--hicas-border)] px-3 text-sm"
                />
              </label>
            </div>

            <label className="block text-sm font-semibold text-[var(--hicas-text-main)]">
              Trạng thái
              <select
                value={normalizeStatus(payload.status)}
                onChange={(event) =>
                  setPayload((prev) => ({
                    ...prev,
                    status: statusValue(event.target.value as PolicyVersionStatus),
                  }))
                }
                className="mt-2 h-11 w-full rounded-[var(--radius-md)] border border-[var(--hicas-border)] px-3 text-sm"
              >
                <option value="Active">Đang áp dụng</option>
                <option value="Draft">Bản nháp</option>
                <option value="Archived">Lưu trữ</option>
              </select>
            </label>

            <label className="block text-sm font-semibold text-[var(--hicas-text-main)]">
              Ghi chú
              <textarea
                rows={3}
                value={payload.note ?? ""}
                onChange={(event) =>
                  setPayload((prev) => ({ ...prev, note: event.target.value }))
                }
                className="mt-2 w-full rounded-[var(--radius-md)] border border-[var(--hicas-border)] px-3 py-2 text-sm"
              />
            </label>

            <div className="flex flex-wrap gap-2">
              <Button iconLeft={<Save size={16} />} isLoading={saving} onClick={handleSave}>
                Lưu lịch
              </Button>
              <Button variant="secondary" iconLeft={<Download size={16} />} onClick={handleExport}>
                Tải JSON
              </Button>
              <label className="hicas-btn-secondary inline-flex min-h-[42px] cursor-pointer items-center justify-center gap-2 rounded-[var(--radius-md)] px-[18px] text-sm font-semibold">
                <Upload size={16} />
                Nhập JSON
                <input type="file" accept="application/json" className="hidden" onChange={handleImport} />
              </label>
            </div>
          </div>
        </Card>

        <div className="space-y-6">
          <Card
            title="Thêm ngày nghỉ"
            description="Thêm một ngày hoặc cả khoảng ngày vào lịch đang cấu hình."
            actions={<Button size="sm" variant="secondary" onClick={handlePreset}>Thêm lễ cơ bản</Button>}
          >
            <form className="grid gap-3 lg:grid-cols-6" onSubmit={handleAddDays}>
              <input
                type="date"
                value={dayForm.date}
                onChange={(event) => setDayForm((prev) => ({ ...prev, date: event.target.value }))}
                className="h-11 rounded-[var(--radius-md)] border border-[var(--hicas-border)] px-3 text-sm"
              />
              <input
                type="date"
                value={rangeEnd}
                onChange={(event) => setRangeEnd(event.target.value)}
                className="h-11 rounded-[var(--radius-md)] border border-[var(--hicas-border)] px-3 text-sm"
                title="Ngày kết thúc nếu thêm theo khoảng"
              />
              <select
                value={dayTypeValue(dayForm.dayType)}
                onChange={(event) =>
                  setDayForm((prev) => ({
                    ...prev,
                    dayType: Number(event.target.value) as CompanyCalendarDayType,
                  }))
                }
                className="h-11 rounded-[var(--radius-md)] border border-[var(--hicas-border)] px-3 text-sm"
              >
                {dayTypeOptions.map((item) => (
                  <option key={item.value} value={item.value}>
                    {item.label}
                  </option>
                ))}
              </select>
              <input
                value={dayForm.name}
                onChange={(event) => setDayForm((prev) => ({ ...prev, name: event.target.value }))}
                placeholder="Tên ngày nghỉ"
                className="h-11 rounded-[var(--radius-md)] border border-[var(--hicas-border)] px-3 text-sm lg:col-span-2"
              />
              <Button type="submit" iconLeft={<Plus size={16} />}>
                Thêm
              </Button>
            </form>

            <div className="mt-3 flex flex-wrap gap-4 text-sm text-[var(--hicas-text-secondary)]">
              <label className="inline-flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={dayForm.isPaid}
                  onChange={(event) =>
                    setDayForm((prev) => ({ ...prev, isPaid: event.target.checked }))
                  }
                  className="h-4 w-4 accent-[var(--hicas-orange)]"
                />
                Hưởng lương
              </label>
              <label className="inline-flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={dayForm.isOvertimeHoliday}
                  onChange={(event) =>
                    setDayForm((prev) => ({ ...prev, isOvertimeHoliday: event.target.checked }))
                  }
                  className="h-4 w-4 accent-[var(--hicas-orange)]"
                />
                Tính OT ngày lễ
              </label>
              <label className="inline-flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={dayForm.isWorkingDayOverride}
                  onChange={(event) =>
                    setDayForm((prev) => ({
                      ...prev,
                      isWorkingDayOverride: event.target.checked,
                    }))
                  }
                  className="h-4 w-4 accent-[var(--hicas-orange)]"
                />
                Là ngày làm việc
              </label>
            </div>
          </Card>

          <Card
            title="Danh sách ngày"
            description="Lịch đang cấu hình sẽ được dùng chung cho các kỳ công trong năm."
            actions={
              activeCalendar ? (
                <Badge variant="success">Active: {activeCalendar.versionCode}</Badge>
              ) : (
                <Badge variant="warning">Chưa có lịch active</Badge>
              )
            }
          >
            {message && (
              <p
                className={`mb-4 rounded-[var(--radius-md)] px-3 py-2 text-sm font-medium ${
                  message.type === "success"
                    ? "bg-emerald-50 text-emerald-700"
                    : "bg-red-50 text-red-700"
                }`}
              >
                {message.text}
              </p>
            )}

            <div className="max-h-[540px] overflow-auto pr-1">
              <table className="min-w-full text-left text-sm">
                <thead className="bg-slate-50 text-xs uppercase text-[var(--hicas-text-secondary)]">
                  <tr>
                    <th className="px-3 py-3">Ngày</th>
                    <th className="px-3 py-3">Tên</th>
                    <th className="px-3 py-3">Loại</th>
                    <th className="px-3 py-3">Áp dụng</th>
                    <th className="px-3 py-3 text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[var(--hicas-border-soft)]">
                  {sortedDays.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="px-3 py-8 text-center text-[var(--hicas-text-secondary)]">
                        Chưa có ngày nào trong lịch.
                      </td>
                    </tr>
                  ) : (
                    sortedDays.map((day) => (
                      <tr key={day.date}>
                        <td className="px-3 py-3 font-medium text-[var(--hicas-text-main)]">
                          {new Date(`${day.date}T00:00:00`).toLocaleDateString("vi-VN")}
                        </td>
                        <td className="px-3 py-3">{day.name}</td>
                        <td className="px-3 py-3">{dayTypeLabel(day.dayType)}</td>
                        <td className="px-3 py-3">
                          <div className="flex flex-wrap gap-2">
                            {day.isPaid && <Badge variant="success">Hưởng lương</Badge>}
                            {day.isOvertimeHoliday && <Badge variant="info">OT ngày lễ</Badge>}
                            {day.isWorkingDayOverride && <Badge variant="warning">Làm việc</Badge>}
                          </div>
                        </td>
                        <td className="px-3 py-3 text-right">
                          <Button
                            size="sm"
                            variant="ghost"
                            iconLeft={<Trash2 size={15} />}
                            onClick={() => handleRemoveDay(day.date)}
                          >
                            Xóa
                          </Button>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
};
