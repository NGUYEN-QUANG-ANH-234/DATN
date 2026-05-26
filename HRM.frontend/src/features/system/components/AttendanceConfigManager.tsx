import React, { useMemo, useState } from "react";
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
      name: "Co so chinh",
      latitude: config.latitude,
      longitude: config.longitude,
      radiusInMeters: config.radiusInMeters,
      allowedIpRanges: config.allowedIpRanges || [],
      isActive: true,
    },
  ];
};

export const AttendanceConfigManager: React.FC = () => {
  const { config, loading, updateConfig } = useAttendanceConfig();
  const [message, setMessage] = useState<string>("");
  const [draftOffices, setDraftOffices] = useState<AttendanceOfficeLocation[]>([
    emptyOffice(),
  ]);
  const [isDirty, setIsDirty] = useState(false);

  const configuredOffices = useMemo(() => toOfficeLocations(config), [config]);
  const offices = isDirty ? draftOffices : configuredOffices;

  const updateDraftOffices = (
    updater: (
      current: AttendanceOfficeLocation[],
    ) => AttendanceOfficeLocation[],
  ) => {
    setIsDirty(true);
    setDraftOffices((current) =>
      updater(isDirty ? current : configuredOffices),
    );
  };

  const updateOffice = (
    index: number,
    patch: Partial<AttendanceOfficeLocation>,
  ) => {
    updateDraftOffices((current) =>
      current.map((office, i) =>
        i === index ? { ...office, ...patch } : office,
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
      current.length === 1 ? current : current.filter((_, i) => i !== index),
    );
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const normalizedOffices = offices.map((office) => ({
        ...office,
        name: office.name.trim(),
        latitude: Number(office.latitude),
        longitude: Number(office.longitude),
        radiusInMeters: Number(office.radiusInMeters),
        allowedIpRanges: office.allowedIpRanges
          .map((ip) => ip.trim())
          .filter(Boolean),
      }));

      const primaryOffice = normalizedOffices.find((office) => office.isActive) ??
        normalizedOffices[0];

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
      setMessage(res.message || "Luu cau hinh thanh cong.");
    } catch (error: unknown) {
      setMessage(`Loi: ${error instanceof Error ? error.message : String(error)}`);
    }
  };

  return (
    <div className="rounded-lg border border-gray-200 bg-white p-5 shadow-sm sm:p-6">
      <div className="mb-5 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-xl font-bold text-gray-900">
            Cau hinh tham so cham cong
          </h2>
          <p className="mt-1 text-sm text-gray-500">
            Moi co so co toa do GPS, ban kinh va danh sach IP/CIDR rieng.
          </p>
        </div>
        <button
          type="button"
          onClick={() =>
            updateDraftOffices((current) => [...current, emptyOffice()])
          }
          className="rounded bg-slate-900 px-4 py-2 text-sm font-semibold text-white hover:bg-slate-800"
        >
          Them co so
        </button>
      </div>

      {loading && !config ? (
        <p>Dang tai du lieu...</p>
      ) : (
        <form onSubmit={handleSubmit} className="space-y-4">
          {offices.map((office, index) => (
            <section
              key={index}
              className="rounded-lg border border-gray-200 p-4"
            >
              <div className="mb-4 flex items-center justify-between gap-3">
                <h3 className="font-semibold text-gray-900">
                  Co so #{index + 1}
                </h3>
                <button
                  type="button"
                  onClick={() => removeOffice(index)}
                  disabled={offices.length === 1}
                  className="rounded border border-red-200 px-3 py-1 text-sm font-medium text-red-600 disabled:cursor-not-allowed disabled:opacity-40"
                >
                  Xoa
                </button>
              </div>

              <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                <div>
                  <label className="mb-1 block text-sm font-medium">
                    Ten co so *
                  </label>
                  <input
                    required
                    value={office.name}
                    onChange={(e) => updateOffice(index, { name: e.target.value })}
                    className="w-full rounded border p-2"
                    placeholder="VD: Tru so Ha Noi"
                  />
                </div>
                <label className="flex items-end gap-2 text-sm font-medium">
                  <input
                    type="checkbox"
                    checked={office.isActive}
                    onChange={(e) =>
                      updateOffice(index, { isActive: e.target.checked })
                    }
                    className="mb-3"
                  />
                  Dang ap dung
                </label>
                <div>
                  <label className="mb-1 block text-sm font-medium">
                    Vi do (Latitude) *
                  </label>
                  <input
                    required
                    type="number"
                    step="any"
                    value={office.latitude}
                    onChange={(e) =>
                      updateOffice(index, { latitude: Number(e.target.value) })
                    }
                    className="w-full rounded border p-2"
                  />
                </div>
                <div>
                  <label className="mb-1 block text-sm font-medium">
                    Kinh do (Longitude) *
                  </label>
                  <input
                    required
                    type="number"
                    step="any"
                    value={office.longitude}
                    onChange={(e) =>
                      updateOffice(index, { longitude: Number(e.target.value) })
                    }
                    className="w-full rounded border p-2"
                  />
                </div>
                <div>
                  <label className="mb-1 block text-sm font-medium">
                    Ban kinh cho phep (m) *
                  </label>
                  <input
                    required
                    type="number"
                    min="1"
                    value={office.radiusInMeters}
                    onChange={(e) =>
                      updateOffice(index, {
                        radiusInMeters: Number(e.target.value),
                      })
                    }
                    className="w-full rounded border p-2"
                  />
                </div>
                <div>
                  <label className="mb-1 block text-sm font-medium">
                    IP Public/CIDR hop le *
                  </label>
                  <textarea
                    required
                    rows={4}
                    value={office.allowedIpRanges.join("\n")}
                    onChange={(e) => updateIpRanges(index, e.target.value)}
                    className="w-full rounded border p-2 font-mono text-sm"
                    placeholder={"123.16.84.230\n123.16.84.0/24"}
                  />
                </div>
              </div>
            </section>
          ))}

          <button
            type="submit"
            className="rounded bg-purple-600 px-4 py-2 font-semibold text-white hover:bg-purple-700"
          >
            Luu cau hinh cham cong
          </button>

          {message && (
            <p
              className={`text-sm font-medium ${
                message.startsWith("Loi") ? "text-red-600" : "text-green-600"
              }`}
            >
              {message}
            </p>
          )}
        </form>
      )}
    </div>
  );
};
