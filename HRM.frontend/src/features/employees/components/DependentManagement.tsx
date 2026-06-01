import React, { useMemo, useState } from "react";
import { dependentApi } from "../api/dependentApi";
import type {
  DependentDto,
  DependentFormState,
  DependentRelation,
} from "../types/dependent";
import { BACKEND_URL } from "../../../core/api/config";
import { useNotification } from "../../../core/context/NotificationContext";

const emptyForm: DependentFormState = {
  fullName: "",
  relationship: 0,
  idNumber: "",
  taxDependentCode: "",
  birthDate: "",
  validFrom: new Date().toISOString().slice(0, 10),
  validTo: "",
  note: "",
  evidenceFile: null,
};

const relationOptions: Array<{ value: DependentRelation; label: string }> = [
  { value: 0, label: "Con" },
  { value: 1, label: "Cha/Mẹ" },
  { value: 2, label: "Vợ/Chồng" },
  { value: 3, label: "Khác" },
];

const relationLabel = (value: DependentRelation) =>
  relationOptions.find((item) => item.value === value)?.label ?? "Khác";

const buildFormData = (form: DependentFormState) => {
  const data = new FormData();
  data.append("FullName", form.fullName.trim());
  data.append("Relationship", String(form.relationship));
  data.append("ValidFrom", form.validFrom);
  if (form.idNumber.trim()) data.append("IdNumber", form.idNumber.trim());
  if (form.taxDependentCode.trim())
    data.append("TaxDependentCode", form.taxDependentCode.trim());
  if (form.birthDate) data.append("BirthDate", form.birthDate);
  if (form.validTo) data.append("ValidTo", form.validTo);
  if (form.note.trim()) data.append("Note", form.note.trim());
  if (form.evidenceFile) data.append("EvidenceFile", form.evidenceFile);
  return data;
};

interface Props {
  dependents: DependentDto[];
  loading: boolean;
  onRefresh: () => void;
}

export const DependentManagement: React.FC<Props> = ({
  dependents,
  loading,
  onRefresh,
}) => {
  const { triggerAlert } = useNotification();
  const [form, setForm] = useState<DependentFormState>(emptyForm);
  const [editing, setEditing] = useState<DependentDto | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const sortedDependents = useMemo(
    () => [...dependents].sort((a, b) => Number(b.isActive) - Number(a.isActive)),
    [dependents],
  );

  const updateField = <K extends keyof DependentFormState>(
    key: K,
    value: DependentFormState[K],
  ) => {
    setForm((prev) => ({ ...prev, [key]: value }));
  };

  const startEdit = (item: DependentDto) => {
    setEditing(item);
    setForm({
      fullName: item.fullName,
      relationship: item.relationship,
      idNumber: item.idNumber ?? "",
      taxDependentCode: item.taxDependentCode ?? "",
      birthDate: item.birthDate ? item.birthDate.slice(0, 10) : "",
      validFrom: item.validFrom ? item.validFrom.slice(0, 10) : "",
      validTo: item.validTo ? item.validTo.slice(0, 10) : "",
      note: item.note ?? "",
      evidenceFile: null,
    });
  };

  const resetForm = () => {
    setEditing(null);
    setForm(emptyForm);
    document
      .querySelectorAll<HTMLInputElement>("input[data-dependent-file='true']")
      .forEach((el) => {
        el.value = "";
      });
  };

  const submitRequest = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!form.fullName.trim()) {
      triggerAlert("warning", "Thiếu thông tin", "Vui lòng nhập họ tên người phụ thuộc.");
      return;
    }
    if (!form.validFrom) {
      triggerAlert("warning", "Thiếu ngày hiệu lực", "Vui lòng chọn ngày bắt đầu hiệu lực.");
      return;
    }

    setSubmitting(true);
    try {
      const payload = buildFormData(form);
      const res = editing
        ? await dependentApi.requestUpdate(editing.id, payload)
        : await dependentApi.requestCreate(payload);
      triggerAlert(
        "success",
        "Đã gửi yêu cầu",
        res.message || "Yêu cầu người phụ thuộc đã được gửi đến HR.",
      );
      resetForm();
      onRefresh();
    } catch (error: unknown) {
      const msg =
        error instanceof Error
          ? error.message
          : "Không thể gửi yêu cầu người phụ thuộc.";
      triggerAlert("error", "Lỗi", msg);
    } finally {
      setSubmitting(false);
    }
  };

  const requestDeactivate = (item: DependentDto) => {
    triggerAlert(
      "confirm",
      "Ngừng hiệu lực người phụ thuộc",
      `Gửi yêu cầu ngừng hiệu lực cho ${item.fullName}?`,
      async () => {
        setSubmitting(true);
        try {
          const res = await dependentApi.requestDeactivate(item.id);
          triggerAlert(
            "success",
            "Đã gửi yêu cầu",
            res.message || "Yêu cầu đã được gửi đến HR.",
          );
          onRefresh();
        } catch (error: unknown) {
          const msg =
            error instanceof Error
              ? error.message
              : "Không thể gửi yêu cầu ngừng hiệu lực.";
          triggerAlert("error", "Lỗi", msg);
        } finally {
          setSubmitting(false);
        }
      },
    );
  };

  return (
    <div className="pt-6 border-t">
      <div className="mb-4 flex flex-col gap-1 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h3 className="font-bold text-gray-800 text-base">
            Người phụ thuộc
          </h3>
          <p className="text-sm text-gray-500">
            Nhân viên gửi yêu cầu, HR phê duyệt trước khi dữ liệu chính thức được cập nhật.
          </p>
        </div>
        {editing && (
          <button
            type="button"
            onClick={resetForm}
            className="rounded border border-gray-300 px-3 py-1.5 text-sm text-gray-600 hover:bg-gray-50"
          >
            Hủy sửa
          </button>
        )}
      </div>

      <div className="grid grid-cols-1 gap-5 lg:grid-cols-[1fr_360px]">
        <div className="overflow-hidden rounded border border-gray-200">
          <table className="min-w-full divide-y divide-gray-200 text-sm">
            <thead className="bg-gray-50 text-left text-xs font-semibold uppercase text-gray-500">
              <tr>
                <th className="px-4 py-3">Họ tên</th>
                <th className="px-4 py-3">Quan hệ</th>
                <th className="px-4 py-3">MST phụ thuộc</th>
                <th className="px-4 py-3">Hiệu lực</th>
                <th className="px-4 py-3">Trạng thái</th>
                <th className="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 bg-white">
              {loading ? (
                <tr>
                  <td colSpan={6} className="px-4 py-6 text-center text-gray-500">
                    Đang tải người phụ thuộc...
                  </td>
                </tr>
              ) : sortedDependents.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-4 py-6 text-center text-gray-500">
                    Chưa có người phụ thuộc.
                  </td>
                </tr>
              ) : (
                sortedDependents.map((item) => (
                  <tr key={item.id} className="align-top">
                    <td className="px-4 py-3">
                      <div className="font-medium text-gray-800">
                        {item.fullName}
                      </div>
                      <div className="text-xs text-gray-500">
                        {item.idNumber || "Chưa có CCCD"}
                      </div>
                      {item.evidenceUrl && (
                        <a
                          href={`${BACKEND_URL}${item.evidenceUrl}`}
                          target="_blank"
                          rel="noreferrer"
                          className="mt-1 inline-block text-xs text-blue-600 hover:underline"
                        >
                          Xem minh chứng
                        </a>
                      )}
                    </td>
                    <td className="px-4 py-3">{relationLabel(item.relationship)}</td>
                    <td className="px-4 py-3">{item.taxDependentCode || "-"}</td>
                    <td className="px-4 py-3">
                      <div>
                        {item.validFrom
                          ? new Date(item.validFrom).toLocaleDateString("vi-VN")
                          : "-"}
                      </div>
                      <div className="text-xs text-gray-500">
                        {item.validTo
                          ? `Đến ${new Date(item.validTo).toLocaleDateString("vi-VN")}`
                          : "Không thời hạn"}
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <span
                        className={`rounded-full px-2 py-1 text-xs font-medium ${
                          item.isActive
                            ? "bg-green-50 text-green-700"
                            : "bg-gray-100 text-gray-500"
                        }`}
                      >
                        {item.isActive ? "Đang hiệu lực" : "Ngừng hiệu lực"}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex flex-col gap-2">
                        <button
                          type="button"
                          onClick={() => startEdit(item)}
                          className="rounded border border-blue-200 px-3 py-1.5 text-xs font-medium text-blue-700 hover:bg-blue-50"
                        >
                          Sửa
                        </button>
                        {item.isActive && (
                          <button
                            type="button"
                            onClick={() => requestDeactivate(item)}
                            disabled={submitting}
                            className="rounded border border-red-200 px-3 py-1.5 text-xs font-medium text-red-600 hover:bg-red-50 disabled:opacity-50"
                          >
                            Ngừng hiệu lực
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <form
          onSubmit={submitRequest}
          className="rounded border border-gray-200 bg-gray-50 p-4"
        >
          <h4 className="mb-3 text-sm font-semibold text-gray-800">
            {editing ? "Gửi yêu cầu sửa" : "Gửi yêu cầu thêm"}
          </h4>
          <div className="space-y-3">
            <input
              className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
              placeholder="Họ tên người phụ thuộc"
              value={form.fullName}
              onChange={(e) => updateField("fullName", e.target.value)}
            />
            <select
              className="w-full rounded border border-gray-300 bg-white px-3 py-2 text-sm"
              value={form.relationship}
              onChange={(e) =>
                updateField("relationship", Number(e.target.value) as DependentRelation)
              }
            >
              {relationOptions.map((item) => (
                <option key={item.value} value={item.value}>
                  {item.label}
                </option>
              ))}
            </select>
            <div className="grid grid-cols-2 gap-3">
              <input
                className="rounded border border-gray-300 px-3 py-2 text-sm"
                placeholder="CCCD"
                value={form.idNumber}
                onChange={(e) => updateField("idNumber", e.target.value)}
              />
              <input
                className="rounded border border-gray-300 px-3 py-2 text-sm"
                placeholder="MST phụ thuộc"
                value={form.taxDependentCode}
                onChange={(e) => updateField("taxDependentCode", e.target.value)}
              />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <label className="text-xs text-gray-500">
                Ngày sinh
                <input
                  type="date"
                  className="mt-1 w-full rounded border border-gray-300 px-3 py-2 text-sm"
                  value={form.birthDate}
                  onChange={(e) => updateField("birthDate", e.target.value)}
                />
              </label>
              <label className="text-xs text-gray-500">
                Hiệu lực từ
                <input
                  type="date"
                  className="mt-1 w-full rounded border border-gray-300 px-3 py-2 text-sm"
                  value={form.validFrom}
                  onChange={(e) => updateField("validFrom", e.target.value)}
                />
              </label>
            </div>
            <label className="block text-xs text-gray-500">
              Hiệu lực đến
              <input
                type="date"
                className="mt-1 w-full rounded border border-gray-300 px-3 py-2 text-sm"
                value={form.validTo}
                onChange={(e) => updateField("validTo", e.target.value)}
              />
            </label>
            <textarea
              className="min-h-[76px] w-full rounded border border-gray-300 px-3 py-2 text-sm"
              placeholder="Ghi chú"
              value={form.note}
              onChange={(e) => updateField("note", e.target.value)}
            />
            <input
              data-dependent-file="true"
              type="file"
              accept=".jpg,.jpeg,.png,.pdf"
              className="w-full rounded border border-gray-300 bg-white px-3 py-2 text-sm"
              onChange={(e) =>
                updateField("evidenceFile", e.target.files?.[0] ?? null)
              }
            />
            <button
              type="submit"
              disabled={submitting}
              className="w-full rounded bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-700 disabled:bg-blue-300"
            >
              {submitting ? "Đang gửi..." : "Gửi yêu cầu HR duyệt"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
