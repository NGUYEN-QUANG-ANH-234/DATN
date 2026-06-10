import React, { useMemo, useState } from "react";
import { Badge, Button, Card, EmptyState } from "../../../components/ui";
import { BACKEND_URL } from "../../../core/api/config";
import { useNotification } from "../../../core/context/NotificationContext";
import { dependentApi } from "../api/dependentApi";
import type {
  DependentDto,
  DependentFormState,
  DependentRelation,
} from "../types/dependent";

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

const formatDate = (value?: string | null) =>
  value ? new Date(value).toLocaleDateString("vi-VN") : "-";

const buildFormData = (form: DependentFormState) => {
  const data = new FormData();
  data.append("FullName", form.fullName.trim());
  data.append("Relationship", String(form.relationship));
  data.append("ValidFrom", form.validFrom);
  if (form.idNumber.trim()) data.append("IdNumber", form.idNumber.trim());
  if (form.taxDependentCode.trim()) {
    data.append("TaxDependentCode", form.taxDependentCode.trim());
  }
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
        error instanceof Error ? error.message : "Không thể gửi yêu cầu người phụ thuộc.";
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
            error instanceof Error ? error.message : "Không thể gửi yêu cầu ngừng hiệu lực.";
          triggerAlert("error", "Lỗi", msg);
        } finally {
          setSubmitting(false);
        }
      },
    );
  };

  return (
    <Card
      title="Người phụ thuộc"
      description="Nhân viên gửi yêu cầu, HR phê duyệt trước khi dữ liệu được cập nhật chính thức."
      actions={editing ? <Badge variant="warning">Đang sửa</Badge> : <Badge variant="info">Tự phục vụ</Badge>}
    >
      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_380px]">
        <div className="overflow-auto rounded-[var(--radius-lg)] border border-[var(--hicas-border)]">
          {loading ? (
            <div className="py-10 text-center text-sm text-[var(--hicas-text-secondary)]">
              Đang tải người phụ thuộc...
            </div>
          ) : sortedDependents.length === 0 ? (
            <div className="p-6">
              <EmptyState
                title="Chưa có người phụ thuộc"
                description="Bạn có thể gửi yêu cầu thêm người phụ thuộc ở form bên cạnh."
              />
            </div>
          ) : (
            <table className="min-w-full divide-y divide-[var(--hicas-border-soft)] text-sm">
              <thead className="bg-[var(--hicas-bg)] text-left text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">
                <tr>
                  <th className="px-4 py-3">Họ tên</th>
                  <th className="px-4 py-3">Quan hệ</th>
                  <th className="px-4 py-3">MST phụ thuộc</th>
                  <th className="px-4 py-3">Hiệu lực</th>
                  <th className="px-4 py-3">Trạng thái</th>
                  <th className="px-4 py-3">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--hicas-border-soft)] bg-white">
                {sortedDependents.map((item) => (
                  <tr key={item.id} className="align-top">
                    <td className="px-4 py-3">
                      <div className="font-semibold text-[var(--hicas-text-main)]">
                        {item.fullName}
                      </div>
                      <div className="text-xs text-[var(--hicas-text-secondary)]">
                        {item.idNumber || "Chưa có CCCD"}
                      </div>
                      {item.evidenceUrl && (
                        <a
                          href={`${BACKEND_URL}${item.evidenceUrl}`}
                          target="_blank"
                          rel="noreferrer"
                          className="mt-1 inline-block text-xs font-semibold text-[var(--hicas-orange-dark)] hover:underline"
                        >
                          Xem minh chứng
                        </a>
                      )}
                    </td>
                    <td className="px-4 py-3">{relationLabel(item.relationship)}</td>
                    <td className="px-4 py-3">{item.taxDependentCode || "-"}</td>
                    <td className="px-4 py-3">
                      <div>{formatDate(item.validFrom)}</div>
                      <div className="text-xs text-[var(--hicas-text-secondary)]">
                        {item.validTo ? `Đến ${formatDate(item.validTo)}` : "Không thời hạn"}
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <Badge variant={item.isActive ? "success" : "neutral"}>
                        {item.isActive ? "Đang hiệu lực" : "Ngừng hiệu lực"}
                      </Badge>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex flex-col gap-2">
                        <Button
                          type="button"
                          size="sm"
                          variant="secondary"
                          onClick={() => startEdit(item)}
                        >
                          Sửa
                        </Button>
                        {item.isActive && (
                          <Button
                            type="button"
                            size="sm"
                            variant="danger"
                            onClick={() => requestDeactivate(item)}
                            disabled={submitting}
                          >
                            Ngừng
                          </Button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        <form
          onSubmit={submitRequest}
          className="rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-[var(--hicas-bg)] p-4"
        >
          <div className="mb-4 flex items-start justify-between gap-3">
            <div>
              <h3 className="text-sm font-semibold text-[var(--hicas-text-main)]">
                {editing ? "Gửi yêu cầu sửa" : "Gửi yêu cầu thêm"}
              </h3>
              <p className="mt-1 text-xs leading-5 text-[var(--hicas-text-secondary)]">
                Thông tin sẽ chuyển đến HR để phê duyệt.
              </p>
            </div>
            {editing && (
              <Button type="button" size="sm" variant="ghost" onClick={resetForm}>
                Hủy
              </Button>
            )}
          </div>

          <div className="space-y-3">
            <input
              className="hicas-input w-full"
              placeholder="Họ tên người phụ thuộc"
              value={form.fullName}
              onChange={(event) => updateField("fullName", event.target.value)}
            />
            <select
              className="hicas-select w-full"
              value={form.relationship}
              onChange={(event) =>
                updateField("relationship", Number(event.target.value) as DependentRelation)
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
                className="hicas-input w-full"
                placeholder="CCCD"
                value={form.idNumber}
                onChange={(event) => updateField("idNumber", event.target.value)}
              />
              <input
                className="hicas-input w-full"
                placeholder="MST phụ thuộc"
                value={form.taxDependentCode}
                onChange={(event) => updateField("taxDependentCode", event.target.value)}
              />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <label className="text-xs font-medium text-[var(--hicas-text-secondary)]">
                Ngày sinh
                <input
                  type="date"
                  className="hicas-input mt-1 w-full"
                  value={form.birthDate}
                  onChange={(event) => updateField("birthDate", event.target.value)}
                />
              </label>
              <label className="text-xs font-medium text-[var(--hicas-text-secondary)]">
                Hiệu lực từ
                <input
                  type="date"
                  className="hicas-input mt-1 w-full"
                  value={form.validFrom}
                  onChange={(event) => updateField("validFrom", event.target.value)}
                />
              </label>
            </div>
            <label className="block text-xs font-medium text-[var(--hicas-text-secondary)]">
              Hiệu lực đến
              <input
                type="date"
                className="hicas-input mt-1 w-full"
                value={form.validTo}
                onChange={(event) => updateField("validTo", event.target.value)}
              />
            </label>
            <textarea
              className="hicas-textarea min-h-[84px] w-full"
              placeholder="Ghi chú"
              value={form.note}
              onChange={(event) => updateField("note", event.target.value)}
            />
            <input
              data-dependent-file="true"
              type="file"
              accept=".jpg,.jpeg,.png,.pdf"
              className="hicas-input w-full py-2 text-sm"
              onChange={(event) =>
                updateField("evidenceFile", event.target.files?.[0] ?? null)
              }
            />
            <Button type="submit" fullWidth isLoading={submitting}>
              {editing ? "Gửi yêu cầu sửa" : "Gửi yêu cầu thêm"}
            </Button>
          </div>
        </form>
      </div>
    </Card>
  );
};
