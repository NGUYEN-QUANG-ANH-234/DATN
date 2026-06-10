import { useEffect, useState } from "react";
import { Save, ShieldCheck, Utensils, Clock3, UsersRound } from "lucide-react";
import { Button, Card } from "../../../components/ui";
import { payrollFeatureToggleApi } from "../api/payrollFeatureToggleApi";
import type { PayrollFeatureToggle } from "../types/payrollFeatureToggle";

const defaultToggles: PayrollFeatureToggle = {
  enableInsurance: true,
  enableOvertime: true,
  enableMealAllowance: true,
  enableExternalTimesheetPay: true,
};

const toggleItems = [
  {
    key: "enableInsurance",
    title: "Bảo hiểm",
    description: "Áp dụng lương đóng bảo hiểm, khoản trích người lao động và chi phí công ty.",
    icon: ShieldCheck,
  },
  {
    key: "enableOvertime",
    title: "Làm thêm giờ",
    description: "Đưa dữ liệu OT đã duyệt vào đối chiếu và công thức lương.",
    icon: Clock3,
  },
  {
    key: "enableMealAllowance",
    title: "Phụ cấp ăn",
    description: "Tính phụ cấp ăn theo ngày công thực tế và chính sách thuế hiện hành.",
    icon: Utensils,
  },
  {
    key: "enableExternalTimesheetPay",
    title: "Giờ công cộng tác viên",
    description: "Cho phép đưa giờ công cộng tác viên đã duyệt vào kỳ lương.",
    icon: UsersRound,
  },
] as const;

type MessageState = {
  type: "success" | "error";
  text: string;
};

export const PayrollFeatureTogglePanel = () => {
  const [toggles, setToggles] = useState<PayrollFeatureToggle>(defaultToggles);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<MessageState | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      const res = await payrollFeatureToggleApi.get();
      setToggles({ ...defaultToggles, ...res.data });
    } catch (error) {
      setMessage({
        type: "error",
        text: error instanceof Error ? error.message : "Không thể tải cấu hình tính lương.",
      });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const handleSave = async () => {
    setSaving(true);
    setMessage(null);
    try {
      const res = await payrollFeatureToggleApi.update(toggles);
      setToggles({ ...defaultToggles, ...res.data });
      setMessage({ type: "success", text: res.message || "Đã lưu cấu hình tính lương." });
    } catch (error) {
      setMessage({
        type: "error",
        text: error instanceof Error ? error.message : "Không thể lưu cấu hình tính lương.",
      });
    } finally {
      setSaving(false);
    }
  };

  return (
    <Card
      title="Nhánh tính lương"
      description="Bật hoặc tắt các nguồn dữ liệu đang áp dụng trong kỳ lương."
      actions={
        <Button
          size="sm"
          iconLeft={<Save size={16} />}
          isLoading={saving}
          onClick={handleSave}
          disabled={loading}
        >
          Lưu cấu hình
        </Button>
      }
    >
      <div className="grid gap-3 md:grid-cols-2">
        {toggleItems.map((item) => {
          const Icon = item.icon;
          const key = item.key;
          const checked = toggles[key];

          return (
            <label
              key={key}
              className="flex min-h-[104px] cursor-pointer gap-3 rounded-[var(--radius-md)] border border-[var(--hicas-border-soft)] bg-white p-4 transition hover:border-[var(--hicas-orange)]"
            >
              <span className="mt-0.5 flex h-10 w-10 shrink-0 items-center justify-center rounded-[var(--radius-md)] bg-[var(--hicas-orange-soft)] text-[var(--hicas-orange-dark)]">
                <Icon size={18} />
              </span>
              <span className="min-w-0 flex-1">
                <span className="flex items-center justify-between gap-3">
                  <span className="text-sm font-semibold text-[var(--hicas-text-main)]">
                    {item.title}
                  </span>
                  <input
                    type="checkbox"
                    checked={checked}
                    onChange={(event) =>
                      setToggles((prev) => ({ ...prev, [key]: event.target.checked }))
                    }
                    className="h-5 w-5 accent-[var(--hicas-orange)]"
                  />
                </span>
                <span className="mt-1 block text-sm leading-6 text-[var(--hicas-text-secondary)]">
                  {item.description}
                </span>
                {!checked && (
                  <span className="mt-2 inline-flex rounded-full bg-slate-100 px-2.5 py-1 text-xs font-semibold text-slate-600">
                    Không áp dụng
                  </span>
                )}
              </span>
            </label>
          );
        })}
      </div>

      {message && (
        <p
          className={`mt-4 rounded-[var(--radius-md)] px-3 py-2 text-sm font-medium ${
            message.type === "success"
              ? "bg-emerald-50 text-emerald-700"
              : "bg-red-50 text-red-700"
          }`}
        >
          {message.text}
        </p>
      )}
    </Card>
  );
};
