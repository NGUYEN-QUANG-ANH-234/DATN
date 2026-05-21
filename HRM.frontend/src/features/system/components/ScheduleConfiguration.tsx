import React, { useState, useEffect } from "react";
import { useScheduleConfig } from "../hooks/useScheduleConfig";
import type { DepartmentTree } from "../../organization/types/department";

export const ScheduleConfiguration: React.FC = () => {
  const {
    departments,
    leaveTypes,
    configuredSchedules,
    loading,
    submitting,
    handleSaveConfig,
  } = useScheduleConfig();

  const [formData, setFormData] = useState({
    shiftName: "Ca Hành Chính",
    startTime: "08:00",
    endTime: "17:00",
    hasBreak: true,
    breakStartTime: "12:00",
    breakEndTime: "13:00",
    lateThresholdMins: 15,
    earlyLeaveThresholdMins: 0,
    deptId: "",
    leaveTypeId: "",
    year: new Date().getFullYear(),
    totalDays: 12,
  });

  const flattenDepartments = (
    nodes: DepartmentTree[],
    level = 0,
  ): { id: number; name: string }[] => {
    return nodes.reduce(
      (acc, curr) => {
        const indent = "— ".repeat(level);
        return [
          ...acc,
          { id: curr.id, name: `${indent}${curr.deptName}` },
          ...flattenDepartments(curr.children, level + 1),
        ];
      },
      [] as { id: number; name: string }[],
    );
  };

  const flatDepts = flattenDepartments(departments);

  useEffect(() => {
    if (flatDepts.length > 0 && !formData.deptId) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setFormData((p) => ({ ...p, deptId: flatDepts[0].id.toString() }));
    }
    if (leaveTypes.length > 0 && !formData.leaveTypeId) {
      setFormData((p) => ({ ...p, leaveTypeId: leaveTypes[0].id.toString() }));
    }
  }, [flatDepts, formData.deptId, formData.leaveTypeId, leaveTypes]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const payload = {
      ...formData,
      deptId: Number(formData.deptId),
      leaveTypeId: Number(formData.leaveTypeId),
      year: Number(formData.year),
      totalDays: Number(formData.totalDays),
      startTime: `${formData.startTime}:00`,
      endTime: `${formData.endTime}:00`,
      breakStartTime: formData.hasBreak
        ? `${formData.breakStartTime}:00`
        : null,
      breakEndTime: formData.hasBreak ? `${formData.breakEndTime}:00` : null,
    };

    await handleSaveConfig(payload);
  };

  if (loading)
    return (
      <div className="p-8 text-center text-gray-500 animate-pulse">
        Đang tải biểu mẫu cấu hình...
      </div>
    );

  return (
    <div className="flex min-h-full flex-col items-center gap-6 rounded-lg bg-gray-50 px-4 py-6 sm:px-6">
      <div className="w-full max-w-6xl rounded-lg border border-gray-200 bg-white p-5 shadow-sm sm:p-6">
        <h2 className="text-2xl font-bold text-gray-800 mb-2">
          Cấu hình Lịch trình & Quỹ phép bộ phận
        </h2>
        <p className="text-gray-500 text-sm mb-6">
          Thiết lập khung giờ làm việc và áp số ngày nghỉ định biên hàng loạt
          cho nhân sự thuộc bộ phận.
        </p>

        <form onSubmit={handleSubmit} className="space-y-6">
          <div className="bg-blue-50/50 p-4 rounded-lg border border-blue-100 grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label className="block text-xs font-semibold text-gray-600 uppercase mb-1">
                Phòng ban áp dụng (*)
              </label>
              <select
                required
                className="w-full border p-2 rounded bg-white text-sm"
                value={formData.deptId}
                onChange={(e) =>
                  setFormData({ ...formData, deptId: e.target.value })
                }
              >
                {flatDepts.map((d) => (
                  <option key={d.id} value={d.id}>
                    {d.name}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs font-semibold text-gray-600 uppercase mb-1">
                Loại ngày nghỉ (*)
              </label>
              <select
                required
                className="w-full border p-2 rounded bg-white text-sm"
                value={formData.leaveTypeId}
                onChange={(e) =>
                  setFormData({ ...formData, leaveTypeId: e.target.value })
                }
              >
                {leaveTypes.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.typeName}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs font-semibold text-gray-600 uppercase mb-1">
                Năm cấu hình / Số ngày định biên
              </label>
              <div className="flex gap-2">
                <input
                  required
                  type="number"
                  className="w-20 border p-2 rounded text-sm text-center"
                  value={formData.year}
                  onChange={(e) =>
                    setFormData({ ...formData, year: Number(e.target.value) })
                  }
                />
                <input
                  required
                  type="number"
                  step="0.5"
                  className="w-full border p-2 rounded text-sm text-center font-bold text-blue-600"
                  value={formData.totalDays}
                  onChange={(e) =>
                    setFormData({
                      ...formData,
                      totalDays: Number(e.target.value),
                    })
                  }
                />
              </div>
            </div>
          </div>

          <div className="border border-gray-200 rounded-lg p-5 space-y-4">
            <h3 className="font-bold text-gray-700 text-sm border-b pb-2">
              ⏱️ Khung giờ Ca làm việc
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Tên Ca (*)
                </label>
                <input
                  required
                  type="text"
                  className="w-full border p-2 rounded text-sm"
                  value={formData.shiftName}
                  onChange={(e) =>
                    setFormData({ ...formData, shiftName: e.target.value })
                  }
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Giờ bắt đầu (*)
                </label>
                <input
                  required
                  type="time"
                  className="w-full border p-2 rounded text-sm"
                  value={formData.startTime}
                  onChange={(e) =>
                    setFormData({ ...formData, startTime: e.target.value })
                  }
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Giờ kết thúc (*)
                </label>
                <input
                  required
                  type="time"
                  className="w-full border p-2 rounded text-sm"
                  value={formData.endTime}
                  onChange={(e) =>
                    setFormData({ ...formData, endTime: e.target.value })
                  }
                />
              </div>
            </div>

            <div className="flex items-center gap-2 pt-2">
              <input
                type="checkbox"
                id="hasBreak"
                checked={formData.hasBreak}
                onChange={(e) =>
                  setFormData({ ...formData, hasBreak: e.target.checked })
                }
              />
              <label
                htmlFor="hasBreak"
                className="text-sm font-medium text-gray-700 select-none"
              >
                Có cấu hình giờ nghỉ trưa
              </label>
            </div>

            {formData.hasBreak && (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 bg-gray-50 p-3 rounded border border-dashed animate-fadeIn">
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">
                    Bắt đầu nghỉ trưa
                  </label>
                  <input
                    type="time"
                    className="w-full border p-2 rounded text-sm bg-white"
                    value={formData.breakStartTime}
                    onChange={(e) =>
                      setFormData({
                        ...formData,
                        breakStartTime: e.target.value,
                      })
                    }
                  />
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-600 mb-1">
                    Kết thúc nghỉ trưa
                  </label>
                  <input
                    type="time"
                    className="w-full border p-2 rounded text-sm bg-white"
                    value={formData.breakEndTime}
                    onChange={(e) =>
                      setFormData({ ...formData, breakEndTime: e.target.value })
                    }
                  />
                </div>
              </div>
            )}
          </div>

          <div className="border border-gray-200 rounded-lg p-5 space-y-4">
            <h3 className="font-bold text-gray-700 text-sm border-b pb-2">
              🚨 Quy tắc đi muộn / Về sớm
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Ngưỡng cho phép đi muộn (phút)
                </label>
                <input
                  type="number"
                  min="0"
                  className="w-full border p-2 rounded text-sm"
                  value={formData.lateThresholdMins}
                  onChange={(e) =>
                    setFormData({
                      ...formData,
                      lateThresholdMins: Number(e.target.value),
                    })
                  }
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Ngưỡng cho phép về sớm (phút)
                </label>
                <input
                  type="number"
                  min="0"
                  className="w-full border p-2 rounded text-sm"
                  value={formData.earlyLeaveThresholdMins}
                  onChange={(e) =>
                    setFormData({
                      ...formData,
                      earlyLeaveThresholdMins: Number(e.target.value),
                    })
                  }
                />
              </div>
            </div>
          </div>

          <div className="flex justify-end gap-3 pt-4 border-t">
            <button
              type="button"
              className="px-5 py-2 border rounded text-sm hover:bg-gray-100"
            >
              Hủy bỏ
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="px-6 py-2 bg-blue-600 text-white rounded font-medium text-sm hover:bg-blue-700 disabled:bg-blue-400 shadow transition-colors"
            >
              {submitting ? "Đang cập nhật..." : "Áp dụng cấu hình"}
            </button>
          </div>
        </form>
      </div>

      <div className="w-full max-w-6xl rounded-lg border border-gray-200 bg-white p-5 shadow-sm sm:p-6">
        <h3 className="text-lg font-bold text-gray-800 mb-4">
          Danh sách cấu hình hiện tại
        </h3>
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm border-collapse">
            <thead>
              <tr className="bg-gray-100 text-gray-700 text-xs uppercase font-semibold">
                <th className="p-3 rounded-l">Phòng ban</th>
                <th className="p-3">Tên Ca làm việc</th>
                <th className="p-3">Khung giờ ca</th>
                <th className="p-3">Nghỉ trưa</th>
                <th className="p-3">Đi muộn/Về sớm</th>
                <th className="p-3 rounded-r">Quỹ Phép Định Biên</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {configuredSchedules.length === 0 ? (
                <tr>
                  <td colSpan={6} className="p-6 text-center text-gray-400">
                    Chưa có phòng ban nào được thiết lập lịch trình.
                  </td>
                </tr>
              ) : (
                configuredSchedules.map((item, index) => (
                  <tr
                    key={index}
                    className="hover:bg-gray-50/70 transition-colors"
                  >
                    <td className="p-3 font-semibold text-gray-800">
                      {item.deptName}
                    </td>
                    <td className="p-3 text-gray-600">
                      <span className="bg-blue-50 text-blue-700 text-xs px-2 py-1 rounded font-medium">
                        {item.shiftName}
                      </span>
                    </td>
                    <td className="p-3 text-gray-700 font-medium">
                      {item.startTime.substring(0, 5)} -{" "}
                      {item.endTime.substring(0, 5)}
                    </td>
                    <td className="p-3 text-gray-500 text-xs">
                      {item.breakStartTime && item.breakEndTime
                        ? `${item.breakStartTime.substring(0, 5)} - ${item.breakEndTime.substring(0, 5)}`
                        : "Không nghỉ"}
                    </td>
                    <td className="p-3 text-xs text-gray-500">
                      <div>Muộn: {item.lateThresholdMins}m</div>
                      <div>Sớm: {item.earlyLeaveThresholdMins}m</div>
                    </td>
                    <td className="p-3">
                      <div className="text-xs font-bold text-blue-600">
                        {item.leaveTypeName} ({item.year})
                      </div>
                      <div className="text-sm font-extrabold text-gray-800">
                        {item.totalDays} ngày phép
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
