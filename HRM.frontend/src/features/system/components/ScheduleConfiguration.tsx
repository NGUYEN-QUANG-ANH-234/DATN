import type { FormEvent } from "react";
import { useEffect, useState } from "react";
import { CalendarDays, History, Save } from "lucide-react";
import { PageHeader } from "../../../components/layout";
import { Badge, Button, Card, DataTable } from "../../../components/ui";
import type { DataTableColumn } from "../../../components/ui";
import { useScheduleConfig } from "../hooks/useScheduleConfig";
import { companyCalendarApi } from "../api/companyCalendarApi";
import type {
  ConfiguredScheduleItem,
  ScheduleChangeHistoryItem,
} from "../types/scheduleConfig";
import type { CompanyCalendar } from "../types/companyCalendar";
import type { DepartmentTree } from "../../organization/types/department";

const currentDate = new Date();

const parseHolidayDates = (value?: string | null): string[] => {
  if (!value) return [];
  try {
    const parsed = JSON.parse(value);
    if (!Array.isArray(parsed)) return [];
    return parsed
      .filter((item): item is string => typeof item === "string")
      .filter(Boolean)
      .sort();
  } catch {
    return [];
  }
};

const stringifyHolidayDates = (dates: string[]) =>
  JSON.stringify([...new Set(dates.filter(Boolean))].sort());

const flattenDepartments = (
  nodes: DepartmentTree[],
  level = 0,
): { id: number; name: string }[] =>
  nodes.reduce(
    (acc, curr) => [
      ...acc,
      { id: curr.id, name: `${"-- ".repeat(level)}${curr.deptName}` },
      ...flattenDepartments(curr.children, level + 1),
    ],
    [] as { id: number; name: string }[],
  );

const formatTime = (value?: string | null) => (value ? value.substring(0, 5) : "--:--");

const extractHistoryMessage = (value?: string | null) => {
  if (!value) return "";
  try {
    const parsed = JSON.parse(value) as { Message?: string; message?: string };
    return parsed.Message || parsed.message || value;
  } catch {
    return value;
  }
};

const toDateLabel = (date: string) => new Date(date).toLocaleDateString("vi-VN");

export const ScheduleConfiguration = () => {
  const {
    departments,
    leaveTypes,
    configuredSchedules,
    history,
    loading,
    submitting,
    handleSaveConfig,
  } = useScheduleConfig();

  const [formData, setFormData] = useState({
    shiftName: "Ca Hành chính",
    startTime: "08:00",
    endTime: "17:00",
    hasBreak: true,
    breakStartTime: "12:00",
    breakEndTime: "13:00",
    lateThresholdMins: 15,
    earlyLeaveThresholdMins: 0,
    deptId: "",
    leaveTypeId: "",
    year: currentDate.getFullYear(),
    month: currentDate.getMonth() + 1,
    standardWorkDays: 22,
    standardHoursPerDay: 8,
    includePaidLeaveInWorkDays: true,
    workingDaysOfWeek: "1,2,3,4,5",
    companyCalendarId: "",
    holidayDatesJson: "[]",
    holidayWorkingStartTime: "",
    holidayWorkingEndTime: "",
    lockWorkCalendar: false,
    calendarNote: "",
    totalDays: 12,
  });
  const [holidayDateInput, setHolidayDateInput] = useState("");
  const [companyCalendars, setCompanyCalendars] = useState<CompanyCalendar[]>([]);

  const flatDepts = flattenDepartments(departments);
  const selectedDeptId = formData.deptId || flatDepts[0]?.id.toString() || "";
  const selectedLeaveTypeId = formData.leaveTypeId || leaveTypes[0]?.id.toString() || "";
  const holidayDates = parseHolidayDates(formData.holidayDatesJson);

  useEffect(() => {
    let mounted = true;
    companyCalendarApi
      .getByYear(Number(formData.year))
      .then((res) => {
        if (mounted) setCompanyCalendars(res.data);
      })
      .catch(() => {
        if (mounted) setCompanyCalendars([]);
      });

    return () => {
      mounted = false;
    };
  }, [formData.year]);

  const updateHolidayDates = (dates: string[]) => {
    setFormData((prev) => ({
      ...prev,
      holidayDatesJson: stringifyHolidayDates(dates),
    }));
  };

  const addHolidayDate = () => {
    if (!holidayDateInput) return;
    updateHolidayDates([...holidayDates, holidayDateInput]);
    setHolidayDateInput("");
  };

  const removeHolidayDate = (date: string) => {
    updateHolidayDates(holidayDates.filter((item) => item !== date));
  };

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();

    await handleSaveConfig({
      ...formData,
      deptId: Number(selectedDeptId),
      leaveTypeId: Number(selectedLeaveTypeId),
      year: Number(formData.year),
      month: Number(formData.month),
      standardWorkDays: Number(formData.standardWorkDays),
      standardHoursPerDay: Number(formData.standardHoursPerDay),
      includePaidLeaveInWorkDays: formData.includePaidLeaveInWorkDays,
      companyCalendarId: formData.companyCalendarId
        ? Number(formData.companyCalendarId)
        : null,
      totalDays: Number(formData.totalDays),
      startTime: `${formData.startTime}:00`,
      endTime: `${formData.endTime}:00`,
      breakStartTime: formData.hasBreak ? `${formData.breakStartTime}:00` : null,
      breakEndTime: formData.hasBreak ? `${formData.breakEndTime}:00` : null,
      holidayDatesJson: stringifyHolidayDates(holidayDates),
      holidayWorkingStartTime: formData.holidayWorkingStartTime
        ? `${formData.holidayWorkingStartTime}:00`
        : null,
      holidayWorkingEndTime: formData.holidayWorkingEndTime
        ? `${formData.holidayWorkingEndTime}:00`
        : null,
      calendarNote: formData.calendarNote || null,
    });
  };

  const scheduleColumns: Array<DataTableColumn<ConfiguredScheduleItem>> = [
    {
      key: "dept",
      header: "Phòng ban",
      render: (row) => (
        <div>
          <p className="font-semibold text-[var(--hicas-text-main)]">{row.deptName}</p>
          <p className="mt-1 text-xs text-[var(--hicas-text-secondary)]">
            Kỳ {row.month ? `${row.month}/${row.year}` : row.year}
          </p>
        </div>
      ),
    },
    {
      key: "shift",
      header: "Ca làm việc",
      render: (row) => (
        <div>
          <p className="font-semibold">{row.shiftName}</p>
          <p className="mt-1 text-xs text-[var(--hicas-text-secondary)]">
            {formatTime(row.startTime)} - {formatTime(row.endTime)}
          </p>
          <p className="text-xs text-[var(--hicas-text-secondary)]">
            Nghỉ:{" "}
            {row.breakStartTime && row.breakEndTime
              ? `${formatTime(row.breakStartTime)} - ${formatTime(row.breakEndTime)}`
              : "Không cấu hình"}
          </p>
        </div>
      ),
    },
    {
      key: "workdays",
      header: "Quỹ công",
      render: (row) => (
        <div>
          <p className="font-bold text-[var(--hicas-orange-dark)]">
            {row.standardWorkDays ?? "--"} ngày
          </p>
          <p className="text-xs text-[var(--hicas-text-secondary)]">
            {row.standardHoursPerDay ?? 8}h/ngày
          </p>
        </div>
      ),
    },
    {
      key: "threshold",
      header: "Ngưỡng",
      render: (row) => (
        <div className="text-sm">
          <p>Muộn: {row.lateThresholdMins} phút</p>
          <p>Sớm: {row.earlyLeaveThresholdMins} phút</p>
        </div>
      ),
    },
    {
      key: "leave",
      header: "Quỹ phép",
      render: (row) => (
        <div>
          <p className="font-semibold">{row.leaveTypeName}</p>
          <p className="text-sm text-[var(--hicas-text-secondary)]">{row.totalDays} ngày/năm</p>
        </div>
      ),
    },
    {
      key: "status",
      header: "Trạng thái",
      render: (row) => (
        <Badge variant={row.isWorkCalendarLocked ? "warning" : "success"}>
          {row.isWorkCalendarLocked ? "Đã khóa" : "Đang mở"}
        </Badge>
      ),
    },
  ];

  const historyColumns: Array<DataTableColumn<ScheduleChangeHistoryItem>> = [
    {
      key: "message",
      header: "Nội dung thay đổi",
      render: (row) => (
        <div>
          <p className="font-semibold text-[var(--hicas-text-main)]">
            {extractHistoryMessage(row.message)}
          </p>
          <p className="mt-1 text-xs text-[var(--hicas-text-secondary)]">
            {row.actorName || "Hệ thống"} - {new Date(row.timestamp).toLocaleString("vi-VN")}
          </p>
        </div>
      ),
    },
    {
      key: "action",
      header: "Loại",
      render: (row) => <Badge variant="neutral">{row.actionType}</Badge>,
    },
  ];

  if (loading) {
    return (
      <Card>
        <div className="py-10 text-center text-sm text-[var(--hicas-text-secondary)]">
          Đang tải cấu hình lịch làm việc...
        </div>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Ca làm việc"
        description="Thiết lập ca làm việc, kỳ công và lịch áp dụng cho từng phòng ban."
        breadcrumb={[
          { label: "Cấu hình hệ thống" },
          { label: "Ca làm việc" },
        ]}
      />

      <Card
        title="Thiết lập ca làm việc"
        description="Cập nhật ca, ngày công chuẩn, lịch nghỉ công ty và quỹ phép cho phòng ban."
        actions={<CalendarDays size={20} className="text-[var(--hicas-orange)]" />}
      >
        <form onSubmit={handleSubmit} className="space-y-6">
          <section className="grid gap-4 md:grid-cols-3">
            <label className="block">
              <span className="mb-2 block text-sm font-semibold">Phòng ban</span>
              <select
                required
                className="hicas-select w-full"
                value={selectedDeptId}
                onChange={(event) => setFormData({ ...formData, deptId: event.target.value })}
              >
                {flatDepts.map((dept) => (
                  <option key={dept.id} value={dept.id}>
                    {dept.name}
                  </option>
                ))}
              </select>
            </label>
            <label className="block">
              <span className="mb-2 block text-sm font-semibold">Năm</span>
              <input
                required
                type="number"
                className="hicas-input w-full"
                value={formData.year}
                onChange={(event) =>
                  setFormData({ ...formData, year: Number(event.target.value) })
                }
              />
            </label>
            <label className="block">
              <span className="mb-2 block text-sm font-semibold">Tháng kỳ công</span>
              <input
                required
                type="number"
                min="1"
                max="12"
                className="hicas-input w-full"
                value={formData.month}
                onChange={(event) =>
                  setFormData({ ...formData, month: Number(event.target.value) })
                }
              />
            </label>
          </section>

          <section className="rounded-2xl border border-[var(--hicas-border)] p-4">
            <h3 className="mb-4 text-sm font-bold uppercase text-[var(--hicas-text-secondary)]">
              Ca làm việc trong ngày
            </h3>
            <div className="grid gap-4 md:grid-cols-3">
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Tên ca</span>
                <input
                  required
                  className="hicas-input w-full"
                  value={formData.shiftName}
                  onChange={(event) =>
                    setFormData({ ...formData, shiftName: event.target.value })
                  }
                />
              </label>
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Giờ bắt đầu</span>
                <input
                  required
                  type="time"
                  className="hicas-input w-full"
                  value={formData.startTime}
                  onChange={(event) =>
                    setFormData({ ...formData, startTime: event.target.value })
                  }
                />
              </label>
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Giờ kết thúc</span>
                <input
                  required
                  type="time"
                  className="hicas-input w-full"
                  value={formData.endTime}
                  onChange={(event) =>
                    setFormData({ ...formData, endTime: event.target.value })
                  }
                />
              </label>
            </div>

            <label className="mt-4 flex items-center gap-2 text-sm font-medium">
              <input
                type="checkbox"
                checked={formData.hasBreak}
                onChange={(event) =>
                  setFormData({ ...formData, hasBreak: event.target.checked })
                }
                className="accent-[var(--hicas-orange)]"
              />
              Có giờ nghỉ giữa ca
            </label>

            {formData.hasBreak && (
              <div className="mt-4 grid gap-4 md:grid-cols-2">
                <label className="block">
                  <span className="mb-2 block text-sm font-semibold">Bắt đầu nghỉ</span>
                  <input
                    type="time"
                    className="hicas-input w-full"
                    value={formData.breakStartTime}
                    onChange={(event) =>
                      setFormData({ ...formData, breakStartTime: event.target.value })
                    }
                  />
                </label>
                <label className="block">
                  <span className="mb-2 block text-sm font-semibold">Kết thúc nghỉ</span>
                  <input
                    type="time"
                    className="hicas-input w-full"
                    value={formData.breakEndTime}
                    onChange={(event) =>
                      setFormData({ ...formData, breakEndTime: event.target.value })
                    }
                  />
                </label>
              </div>
            )}
          </section>

          <section className="rounded-2xl border border-[var(--hicas-border)] p-4">
            <h3 className="mb-4 text-sm font-bold uppercase text-[var(--hicas-text-secondary)]">
              Quỹ thời gian và quy đổi công
            </h3>
            <div className="grid gap-4 md:grid-cols-4">
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Ngày công chuẩn</span>
                <input
                  required
                  type="number"
                  min="1"
                  max="31"
                  step="0.5"
                  className="hicas-input w-full"
                  value={formData.standardWorkDays}
                  onChange={(event) =>
                    setFormData({ ...formData, standardWorkDays: Number(event.target.value) })
                  }
                />
              </label>
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Giờ chuẩn/ngày</span>
                <input
                  type="number"
                  min="1"
                  max="24"
                  step="0.25"
                  className="hicas-input w-full"
                  value={formData.standardHoursPerDay}
                  onChange={(event) =>
                    setFormData({ ...formData, standardHoursPerDay: Number(event.target.value) })
                  }
                />
              </label>
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Ngưỡng đi muộn</span>
                <input
                  type="number"
                  min="0"
                  className="hicas-input w-full"
                  value={formData.lateThresholdMins}
                  onChange={(event) =>
                    setFormData({ ...formData, lateThresholdMins: Number(event.target.value) })
                  }
                />
              </label>
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Ngưỡng về sớm</span>
                <input
                  type="number"
                  min="0"
                  className="hicas-input w-full"
                  value={formData.earlyLeaveThresholdMins}
                  onChange={(event) =>
                    setFormData({
                      ...formData,
                      earlyLeaveThresholdMins: Number(event.target.value),
                    })
                  }
                />
              </label>
            </div>

            <div className="mt-4 grid gap-4 md:grid-cols-2">
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Ngày làm trong tuần</span>
                <input
                  className="hicas-input w-full"
                  value={formData.workingDaysOfWeek}
                  onChange={(event) =>
                    setFormData({ ...formData, workingDaysOfWeek: event.target.value })
                  }
                  placeholder="1,2,3,4,5"
                />
              </label>
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Lịch nghỉ công ty</span>
                <select
                  className="hicas-select w-full"
                  value={formData.companyCalendarId}
                  onChange={(event) =>
                    setFormData({ ...formData, companyCalendarId: event.target.value })
                  }
                >
                  <option value="">Dùng lịch active theo năm</option>
                  {companyCalendars.map((calendar) => (
                    <option key={calendar.id} value={calendar.id}>
                      {calendar.versionCode}
                    </option>
                  ))}
                </select>
              </label>
            <div className="rounded-2xl border border-[var(--hicas-border)] bg-[var(--hicas-orange-lighter)] p-4 text-sm text-[var(--hicas-text-secondary)]">
                Ngày công = phút làm thực tế / giờ chuẩn, tối đa 1 công/ngày. Phần vượt giờ
                chuẩn được xử lý riêng ở phần làm thêm.
              </div>
            </div>

            <div className="mt-4 grid gap-4 md:grid-cols-2">
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Giờ bắt đầu làm ngày lễ</span>
                <input
                  type="time"
                  className="hicas-input w-full"
                  value={formData.holidayWorkingStartTime}
                  onChange={(event) =>
                    setFormData({ ...formData, holidayWorkingStartTime: event.target.value })
                  }
                />
              </label>
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Giờ kết thúc làm ngày lễ</span>
                <input
                  type="time"
                  className="hicas-input w-full"
                  value={formData.holidayWorkingEndTime}
                  onChange={(event) =>
                    setFormData({ ...formData, holidayWorkingEndTime: event.target.value })
                  }
                />
              </label>
            </div>

            <div className="mt-4 grid gap-4 md:grid-cols-2">
              <div>
                <span className="mb-2 block text-sm font-semibold">Ngày nghỉ lễ trong kỳ</span>
                <div className="flex gap-2">
                  <input
                    type="date"
                    className="hicas-input w-full"
                    value={holidayDateInput}
                    onChange={(event) => setHolidayDateInput(event.target.value)}
                  />
                  <Button type="button" variant="secondary" onClick={addHolidayDate}>
                    Thêm
                  </Button>
                </div>
                <div className="mt-3 flex min-h-10 flex-wrap gap-2">
                  {holidayDates.length === 0 ? (
                    <span className="text-sm text-[var(--hicas-text-muted)]">
                      Chưa có ngày nghỉ lễ trong kỳ.
                    </span>
                  ) : (
                    holidayDates.map((date) => (
                      <button
                        key={date}
                        type="button"
                        onClick={() => removeHolidayDate(date)}
                        className="rounded-full bg-[var(--hicas-orange-soft)] px-3 py-1 text-xs font-semibold text-[var(--hicas-orange-dark)]"
                      >
                        {toDateLabel(date)} x
                      </button>
                    ))
                  )}
                </div>
              </div>
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Ghi chú kỳ công</span>
                <textarea
                  className="hicas-textarea min-h-[90px] w-full"
                  value={formData.calendarNote}
                  onChange={(event) =>
                    setFormData({ ...formData, calendarNote: event.target.value })
                  }
                />
              </label>
            </div>

            <div className="mt-4 grid gap-3 md:grid-cols-2">
              <label className="flex items-center gap-2 text-sm font-medium">
                <input
                  type="checkbox"
                  checked={formData.includePaidLeaveInWorkDays}
                  onChange={(event) =>
                    setFormData({
                      ...formData,
                      includePaidLeaveInWorkDays: event.target.checked,
                    })
                  }
                  className="accent-[var(--hicas-orange)]"
                />
                Nghỉ phép có lương được tính vào ngày công
              </label>
              <label className="flex items-center gap-2 text-sm font-medium">
                <input
                  type="checkbox"
                  checked={formData.lockWorkCalendar}
                  onChange={(event) =>
                    setFormData({ ...formData, lockWorkCalendar: event.target.checked })
                  }
                  className="accent-[var(--hicas-orange)]"
                />
                Khóa kỳ công sau khi chốt
              </label>
            </div>
          </section>

          <section className="rounded-2xl border border-[var(--hicas-border)] p-4">
            <h3 className="mb-4 text-sm font-bold uppercase text-[var(--hicas-text-secondary)]">
              Quỹ nghỉ phép định biên
            </h3>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Loại nghỉ phép</span>
                <select
                  required
                  className="hicas-select w-full"
                  value={selectedLeaveTypeId}
                  onChange={(event) =>
                    setFormData({ ...formData, leaveTypeId: event.target.value })
                  }
                >
                  {leaveTypes.map((type) => (
                    <option key={type.id} value={type.id}>
                      {type.typeName}
                    </option>
                  ))}
                </select>
              </label>
              <label className="block">
                <span className="mb-2 block text-sm font-semibold">Số ngày cấp trong năm</span>
                <input
                  required
                  type="number"
                  min="0"
                  step="0.5"
                  className="hicas-input w-full"
                  value={formData.totalDays}
                  onChange={(event) =>
                    setFormData({ ...formData, totalDays: Number(event.target.value) })
                  }
                />
              </label>
            </div>
          </section>

          <div className="flex justify-end">
            <Button type="submit" isLoading={submitting} iconLeft={<Save size={17} />}>
              Áp dụng cấu hình
            </Button>
          </div>
        </form>
      </Card>

      <DataTable
        columns={scheduleColumns}
        data={configuredSchedules}
        loading={loading}
        rowKey={(row) => row.deptId}
        emptyTitle="Chưa có cấu hình lịch trình"
        emptyDescription="Hãy thiết lập ca làm việc và kỳ công cho phòng ban đầu tiên."
      />

      <Card
        title="Lịch sử thay đổi"
        description="Các biến động cấu hình ca, kỳ công và quỹ phép gần đây."
        actions={<History size={20} className="text-[var(--hicas-orange)]" />}
      >
        <DataTable
          columns={historyColumns}
          data={history}
          rowKey={(row) => row.id}
          className="border-0 shadow-none"
          emptyTitle="Chưa có lịch sử thay đổi"
        />
      </Card>
    </div>
  );
};

