import { AlertTriangle, CheckCircle2, SlidersHorizontal } from "lucide-react";
import { Badge, Card } from "../../../components/ui";
import type { PayrollPreflight } from "../types/payroll";

type Props = {
  preflight: PayrollPreflight | null;
  loading?: boolean;
};

const dateLabel = (value?: string | null) => (value ? value.slice(0, 10) : "Khong gioi han");

export const PayrollPreflightPanel = ({ preflight, loading = false }: Props) => {
  if (loading) {
    return (
      <Card title="Kiem tra truoc khi tinh luong">
        <p className="text-sm text-[var(--hicas-text-secondary)]">Dang kiem tra cau hinh dang ap dung...</p>
      </Card>
    );
  }

  if (!preflight) {
    return (
      <Card title="Kiem tra truoc khi tinh luong">
        <p className="text-sm text-[var(--hicas-text-secondary)]">Chua co du lieu kiem tra cho ky luong nay.</p>
      </Card>
    );
  }

  return (
    <Card
      title="Kiem tra truoc khi tinh luong"
      description="Xem cac phien ban chinh sach va tac dong cau hinh truoc khi tong hop."
      actions={
        <Badge variant={preflight.canCalculate ? "success" : "danger"}>
          {preflight.canCalculate ? "San sang tinh" : "Can bo sung"}
        </Badge>
      }
    >
      <div className="grid gap-4 lg:grid-cols-[1.25fr_0.75fr]">
        <div className="space-y-3">
          {preflight.errors.length > 0 && (
            <div className="rounded-[var(--radius-md)] border border-red-200 bg-red-50 p-3 text-sm text-red-700">
              <div className="mb-2 flex items-center gap-2 font-semibold">
                <AlertTriangle size={16} />
                Can xu ly truoc khi tinh luong
              </div>
              <ul className="list-disc space-y-1 pl-5">
                {preflight.errors.map((item) => (
                  <li key={item}>{item}</li>
                ))}
              </ul>
            </div>
          )}

          {preflight.warnings.length > 0 && (
            <div className="rounded-[var(--radius-md)] border border-amber-200 bg-amber-50 p-3 text-sm text-amber-800">
              <div className="mb-2 flex items-center gap-2 font-semibold">
                <AlertTriangle size={16} />
                Can luu y
              </div>
              <ul className="list-disc space-y-1 pl-5">
                {preflight.warnings.map((item) => (
                  <li key={item}>{item}</li>
                ))}
              </ul>
            </div>
          )}

          {preflight.errors.length === 0 && preflight.warnings.length === 0 && (
            <div className="rounded-[var(--radius-md)] border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-700">
              <div className="flex items-center gap-2 font-semibold">
                <CheckCircle2 size={16} />
                Cau hinh bat buoc da san sang cho ky {preflight.period}.
              </div>
            </div>
          )}

          <div className="overflow-hidden rounded-[var(--radius-md)] border border-[var(--hicas-border-soft)]">
            <div className="grid grid-cols-[1fr_1fr_110px_170px] bg-slate-50 px-3 py-2 text-xs font-semibold uppercase tracking-[0.06em] text-[var(--hicas-text-secondary)]">
              <span>Nhom</span>
              <span>Phien ban</span>
              <span>Trang thai</span>
              <span>Hieu luc</span>
            </div>
            <div className="max-h-[300px] divide-y divide-[var(--hicas-border-soft)] overflow-auto">
              {preflight.policies.map((policy) => (
                <div key={`${policy.area}-${policy.code}-${policy.versionCode ?? policy.version}`} className="grid grid-cols-[1fr_1fr_110px_170px] gap-2 px-3 py-2 text-sm">
                  <div>
                    <p className="font-semibold text-[var(--hicas-text-main)]">{policy.area}</p>
                    <p className="text-xs text-[var(--hicas-text-secondary)]">{policy.name}</p>
                  </div>
                  <div>
                    <p className="font-mono text-xs font-semibold text-[var(--hicas-text-main)]">{policy.versionCode || `v${policy.version}`}</p>
                    <p className="truncate text-xs text-[var(--hicas-text-secondary)]">{policy.code}</p>
                  </div>
                  <div>
                    <Badge variant={policy.isApplied ? "success" : "neutral"}>
                      {policy.isApplied ? "Ap dung" : "Tam tat"}
                    </Badge>
                  </div>
                  <p className="text-xs text-[var(--hicas-text-secondary)]">
                    {dateLabel(policy.effectiveFrom)} - {dateLabel(policy.effectiveTo)}
                  </p>
                </div>
              ))}
            </div>
          </div>
        </div>

        <div className="space-y-3">
          <div className="flex items-center gap-2 text-sm font-semibold text-[var(--hicas-text-main)]">
            <SlidersHorizontal size={16} />
            Tac dong cau hinh
          </div>
          {preflight.dependencyImpacts.map((item) => (
            <div key={item.key} className="rounded-[var(--radius-md)] border border-[var(--hicas-border-soft)] p-3">
              <div className="mb-2 flex items-center justify-between gap-2">
                <span className="text-sm font-semibold text-[var(--hicas-text-main)]">{item.name}</span>
                <Badge variant={item.enabled ? "success" : "neutral"}>
                  {item.enabled ? "Dang bat" : "Tam tat"}
                </Badge>
              </div>
              <ul className="list-disc space-y-1 pl-5 text-xs leading-5 text-[var(--hicas-text-secondary)]">
                {item.impacts.map((impact) => (
                  <li key={impact}>{impact}</li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      </div>
    </Card>
  );
};
