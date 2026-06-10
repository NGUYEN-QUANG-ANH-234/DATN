import type { FormEvent } from "react";
import { useMemo, useState } from "react";
import { MapPin, Plus, Save, Trash2 } from "lucide-react";
import { PageHeader } from "../../../components/layout";
import { Badge, Button, Card } from "../../../components/ui";
import { useAttendanceConfig } from "../hooks/useAttendanceConfig";
import type {
  AttendanceConfig,
  AttendanceOfficeLocation,
} from "../types/attendanceConfig";

const emptyOffice = (): AttendanceOfficeLocation => ({
  name: "",
  latitude: 0,
  longitude: 0,
  radiusInMeters: 50,
  allowedIpRanges: [],
  isActive: true,
});

const toOfficeLocations = (
  config?: AttendanceConfig | null,
): AttendanceOfficeLocation[] => {
  if (!config) return [emptyOffice()];

  if (config.officeLocations && config.officeLocations.length > 0) {
    return config.officeLocations;
  }

  return [
    {
      name: "Cơ sở chính",
      latitude: config.latitude,
      longitude: config.longitude,
      radiusInMeters: config.radiusInMeters,
      allowedIpRanges: config.allowedIpRanges || [],
      isActive: true,
    },
  ];
};

type MessageState = {
  type: "success" | "error";
  text: string;
};

export const AttendanceConfigManager = () => {
  const { config, loading, updateConfig } = useAttendanceConfig();
  const [message, setMessage] = useState<MessageState | null>(null);
  const [draftOffices, setDraftOffices] = useState<AttendanceOfficeLocation[]>([
    emptyOffice(),
  ]);
  const [isDirty, setIsDirty] = useState(false);

  const configuredOffices = useMemo(() => toOfficeLocations(config), [config]);
  const offices = isDirty ? draftOffices : configuredOffices;

  const updateDraftOffices = (
    updater: (current: AttendanceOfficeLocation[]) => AttendanceOfficeLocation[],
  ) => {
    setIsDirty(true);
    setDraftOffices((current) => updater(isDirty ? current : configuredOffices));
  };

  const updateOffice = (index: number, patch: Partial<AttendanceOfficeLocation>) => {
    updateDraftOffices((current) =>
      current.map((office, officeIndex) =>
        officeIndex === index ? { ...office, ...patch } : office,
      ),
    );
  };

  const updateIpRanges = (index: number, value: string) => {
    updateOffice(index, {
      allowedIpRanges: value
        .split("\n")
        .map((ip) => ip.trim())
        .filter(Boolean),
    });
  };

  const removeOffice = (index: number) => {
    updateDraftOffices((current) =>
      current.length === 1 ? current : current.filter((_, officeIndex) => officeIndex !== index),
    );
  };

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    try {
      const normalizedOffices = offices.map((office) => ({
        ...office,
        name: office.name.trim(),
        latitude: Number(office.latitude),
        longitude: Number(office.longitude),
        radiusInMeters: Number(office.radiusInMeters),
        allowedIpRanges: office.allowedIpRanges.map((ip) => ip.trim()).filter(Boolean),
      }));

      const primaryOffice =
        normalizedOffices.find((office) => office.isActive) ?? normalizedOffices[0];

      const payload: AttendanceConfig = {
        latitude: primaryOffice.latitude,
        longitude: primaryOffice.longitude,
        radiusInMeters: primaryOffice.radiusInMeters,
        allowedIpRanges: primaryOffice.allowedIpRanges,
        officeLocations: normalizedOffices,
      };

      const res = (await updateConfig(payload)) as { message?: string };
      setDraftOffices(normalizedOffices);
      setIsDirty(true);
      setMessage({
        type: "success",
        text: res.message || "Đã lưu cấu hình chấm công.",
      });
    } catch (error: unknown) {
      setMessage({
        type: "error",
        text: error instanceof Error ? error.message : String(error),
      });
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="Tham số chấm công"
        description="Thiết lập vị trí, bán kính và mạng cho phép tại từng cơ sở làm việc."
        breadcrumb={[
          { label: "Cấu hình hệ thống" },
          { label: "Tham số chấm công" },
        ]}
        actions={
          <Button
            variant="secondary"
            iconLeft={<Plus size={17} />}
            onClick={() => updateDraftOffices((current) => [...current, emptyOffice()])}
          >
            Thêm cơ sở
          </Button>
        }
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

      {loading && !config ? (
        <Card>
          <div className="py-10 text-center text-sm text-[var(--hicas-text-secondary)]">
            Đang tải dữ liệu...
          </div>
        </Card>
      ) : (
        <form onSubmit={handleSubmit} className="space-y-5">
          <div className="grid gap-5 xl:grid-cols-2">
            {offices.map((office, index) => (
              <Card
                key={index}
                title={`Cơ sở #${index + 1}`}
                description="Tọa độ, bán kính và mạng được phép chấm công."
                actions={
                  <div className="flex items-center gap-2">
                    <Badge variant={office.isActive ? "success" : "neutral"}>
                      {office.isActive ? "Đang áp dụng" : "Tạm tắt"}
                    </Badge>
                    <button
                      type="button"
                      onClick={() => removeOffice(index)}
                      disabled={offices.length === 1}
                      className="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-[var(--hicas-border)] text-[var(--hicas-danger)] transition hover:bg-[var(--hicas-danger-soft)] disabled:cursor-not-allowed disabled:opacity-40"
                      aria-label="Xóa cơ sở"
                    >
                      <Trash2 size={16} />
                    </button>
                  </div>
                }
              >
                <div className="grid gap-4 md:grid-cols-2">
                  <label className="block md:col-span-2">
                    <span className="mb-2 block text-sm font-semibold">Tên cơ sở *</span>
                    <input
                      required
                      value={office.name}
                      onChange={(event) => updateOffice(index, { name: event.target.value })}
                      className="hicas-input w-full"
                      placeholder="Ví dụ: Trụ sở Hà Nội"
                    />
                  </label>

                  <label className="flex items-center gap-2 rounded-2xl border border-[var(--hicas-border)] px-4 py-3 text-sm font-medium md:col-span-2">
                    <input
                      type="checkbox"
                      checked={office.isActive}
                      onChange={(event) =>
                        updateOffice(index, { isActive: event.target.checked })
                      }
                      className="accent-[var(--hicas-orange)]"
                    />
                    Cho phép chấm công tại cơ sở này
                  </label>

                  <label className="block">
                    <span className="mb-2 block text-sm font-semibold">Vĩ độ *</span>
                    <input
                      required
                      type="number"
                      step="any"
                      value={office.latitude}
                      onChange={(event) =>
                        updateOffice(index, { latitude: Number(event.target.value) })
                      }
                      className="hicas-input w-full"
                    />
                  </label>

                  <label className="block">
                    <span className="mb-2 block text-sm font-semibold">Kinh độ *</span>
                    <input
                      required
                      type="number"
                      step="any"
                      value={office.longitude}
                      onChange={(event) =>
                        updateOffice(index, { longitude: Number(event.target.value) })
                      }
                      className="hicas-input w-full"
                    />
                  </label>

                  <label className="block">
                    <span className="mb-2 block text-sm font-semibold">
                      Bán kính cho phép (m) *
                    </span>
                    <input
                      required
                      type="number"
                      min="1"
                      value={office.radiusInMeters}
                      onChange={(event) =>
                        updateOffice(index, { radiusInMeters: Number(event.target.value) })
                      }
                      className="hicas-input w-full"
                    />
                  </label>

                  <div className="rounded-2xl border border-[var(--hicas-border)] bg-[var(--hicas-orange-lighter)] p-4 text-sm text-[var(--hicas-text-secondary)]">
                    <div className="mb-2 flex items-center gap-2 font-semibold text-[var(--hicas-text-main)]">
                      <MapPin size={17} className="text-[var(--hicas-orange)]" />
                      Điều kiện xác thực
                    </div>
                    Check-in hợp lệ khi thiết bị nằm trong bán kính GPS và IP public thuộc
                    danh sách cho phép.
                  </div>

                  <label className="block md:col-span-2">
                    <span className="mb-2 block text-sm font-semibold">
                      IP Public/CIDR hợp lệ *
                    </span>
                    <textarea
                      required
                      rows={4}
                      value={office.allowedIpRanges.join("\n")}
                      onChange={(event) => updateIpRanges(index, event.target.value)}
                      className="hicas-textarea w-full font-mono text-sm"
                      placeholder={"123.16.84.230\n123.16.84.0/24"}
                    />
                  </label>
                </div>
              </Card>
            ))}
          </div>

          <div className="flex justify-end">
            <Button type="submit" iconLeft={<Save size={17} />}>
              Lưu cấu hình chấm công
            </Button>
          </div>
        </form>
      )}
    </div>
  );
};
