import {
  Banknote,
  BriefcaseBusiness,
  CalendarDays,
  ChevronLeft,
  ChevronRight,
  Clock3,
  Download,
  FileText,
  RefreshCw,
  ShieldCheck,
  TrendingUp,
  WalletCards,
} from "lucide-react";
import type { ReactNode } from "react";
import { Button, Card, StatusBadge } from "../../../components/ui";
import { useCurrentUser } from "../../../core/auth/hooks/useCurrentUser";
import { normalizeRole } from "../../../core/auth/roleAccess";
import { FeaturePage } from "../../../core/components/FeatureShell";
import { usePayrollPeriod } from "../hooks/usePayrollPeriod";
import { useMySalarySlips } from "../hooks/useMySalarySlips";
import type { ExternalTimesheetSource, ProjectBonusSource, SalarySlip, SalarySlipDetail } from "../types/payroll";
import { formatMoney, formatNumber, getPayrollStatusLabel, normalizePayrollStatus } from "../utils";

const statusLabel: Record<string, string> = {
  Draft: "Bản nháp",
  Calculated: "Đã tổng hợp",
  HRReviewed: "HR đã kiểm tra",
  PendingApproval: "Chờ duyệt",
  Approved: "Đã duyệt",
  Locked: "Đã khóa",
  Finalized: "Đã chốt",
  Paid: "Đã chi trả",
  Cancelled: "Đã hủy",
  RevisionRequired: "Cần bổ sung",
  Rejected: "Từ chối",
};

const importantIncomeCodes = new Set([
  "BASE_SALARY_ACTUAL",
  "BASE_SALARY",
  "KPI_BONUS",
  "PROJECT_BONUS",
  "INTERN_ALLOWANCE",
  "EXTERNAL_TIMESHEET_PAY",
  "OVERTIME_PAY",
  "SENIORITY_ALLOWANCE",
]);

export const MySalaryPage = () => {
  const { user } = useCurrentUser();
  const role = normalizeRole(user?.role);
  const accessMode = role === "Admin" ? "scope" : "self";
  const { month, year, period, setMonth, setYear } = usePayrollPeriod();
  const payroll = useMySalarySlips(period, accessMode);
  const slip = payroll.activeSlip;

  const incomeDetails = getIncomeDetails(slip);
  const deductionDetails = getDeductionDetails(slip);
  const projectBonusSources = getProjectBonusSources(slip);
  const externalTimesheetSources = getExternalTimesheetSources(slip);
  const kpiBonus = sumByCodes(slip, ["KPI_BONUS"]);
  const internAllowance = sumByCodes(slip, ["INTERN_ALLOWANCE"]);
  const projectBonus = sumByCodes(slip, ["PROJECT_BONUS"]);
  const externalTimesheet = sumByCodes(slip, ["EXTERNAL_TIMESHEET_PAY"]);
  const overtimePay = sumByCodes(slip, ["OVERTIME_PAY", "OVERTIME_HOURS"]);
  const totalDeductions = payroll.summary?.totalDeductions ?? 0;

  const moveMonth = (direction: -1 | 1) => {
    const next = new Date(year, month - 1 + direction, 1);
    setMonth(next.getMonth() + 1);
    setYear(next.getFullYear());
  };

  const downloadCsv = () => {
    if (!slip) return;
    const csv = buildPersonalSlipCsv(slip);
    const blob = new Blob(["\uFEFF" + csv], { type: "text/csv;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `phieu-luong-${slip.period.replace("/", "-")}.csv`;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  };

  return (
    <FeaturePage
      title="Lương của tôi"
      description="Theo dõi phiếu lương, khoản thu nhập, khấu trừ và dữ liệu nguồn trong từng kỳ."
      width="wide"
      actions={
        <div className="flex flex-wrap items-center gap-2">
          <Button variant="secondary" iconLeft={<RefreshCw size={16} />} onClick={payroll.loadSlips} disabled={payroll.loading}>
            Làm mới
          </Button>
          <Button variant="secondary" iconLeft={<Download size={16} />} onClick={downloadCsv} disabled={!slip}>
            Tải CSV
          </Button>
        </div>
      }
    >
      <Card padded={false} className="overflow-hidden">
        <div className="grid gap-0 lg:grid-cols-[1.35fr_0.65fr]">
          <section className="relative overflow-hidden bg-[linear-gradient(135deg,#111111_0%,#181a1b_54%,#2a1708_100%)] p-6 text-white sm:p-8">
            <div className="absolute right-[-80px] top-[-80px] h-64 w-64 rounded-full bg-[rgba(255,122,0,0.20)] blur-3xl" />
            <div className="relative z-10 flex flex-col gap-8">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="text-sm font-bold uppercase tracking-[0.18em] text-[var(--hicas-orange)]">
                    Phiếu lương cá nhân
                  </p>
                  <h2 className="mt-3 text-3xl font-extrabold tracking-normal sm:text-4xl">
                    {slip ? formatMoney(slip.netSalary) : "Chưa có phiếu"}
                  </h2>
                  <p className="mt-2 text-sm font-medium text-white/70">
                    Thực nhận kỳ {period}
                  </p>
                </div>
                <div className="rounded-[var(--radius-lg)] border border-white/15 bg-white/10 px-4 py-3 backdrop-blur">
                  <p className="text-xs font-semibold uppercase tracking-[0.12em] text-white/60">Trạng thái</p>
                  <p className="mt-1 text-lg font-bold">{slip ? getStatusLabel(slip.status) : "Chưa phát sinh"}</p>
                </div>
              </div>

              {slip ? (
                <div className="grid gap-3 sm:grid-cols-3">
                  <HeroMetric label="Tổng thu nhập" value={formatMoney(slip.grossIncome)} />
                  <HeroMetric label="Khấu trừ" value={formatMoney(totalDeductions)} />
                  <HeroMetric label="Chi phí công ty" value={formatMoney(slip.totalCompanyCost)} />
                </div>
              ) : (
                <div className="rounded-[var(--radius-lg)] border border-white/15 bg-white/10 p-4 text-sm font-medium text-white/75">
                  Chưa có phiếu lương trong kỳ này. Phiếu sẽ hiển thị sau khi bảng lương được tổng hợp và phát hành.
                </div>
              )}
            </div>
          </section>

          <section className="flex flex-col justify-between gap-5 bg-white p-6 sm:p-8">
            <div>
              <p className="text-sm font-bold uppercase tracking-[0.14em] text-[var(--hicas-text-secondary)]">Kỳ lương</p>
              <div className="mt-3 flex items-center gap-2">
                <Button variant="secondary" iconLeft={<ChevronLeft size={16} />} onClick={() => moveMonth(-1)}>
                  Trước
                </Button>
                <div className="flex min-h-11 min-w-[112px] items-center justify-center rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-[var(--hicas-bg-soft)] px-4 text-base font-bold text-[var(--hicas-text-main)]">
                  {period}
                </div>
                <Button variant="secondary" iconRight={<ChevronRight size={16} />} onClick={() => moveMonth(1)}>
                  Sau
                </Button>
              </div>
            </div>

            <div className="grid gap-3 text-sm">
              <InfoLine label="Nhân sự" value={slip?.employeeName || "Chưa có dữ liệu"} />
              <InfoLine label="Mã nhân viên" value={slip?.employeeCode || "-"} />
              <InfoLine label="Phòng ban" value={slip?.departmentName || "-"} />
              <InfoLine label="Chức danh" value={slip?.positionName || "-"} />
            </div>
          </section>
        </div>
      </Card>

      {slip ? (
        <>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-6">
            <MetricTile icon={<Banknote size={20} />} label="Lương hợp đồng" value={formatMoney(slip.baseSalary)} />
            <MetricTile icon={<WalletCards size={20} />} label="Trợ cấp thực tập" value={formatMoney(internAllowance)} />
            <MetricTile icon={<TrendingUp size={20} />} label="Thưởng KPI" value={formatMoney(kpiBonus)} />
            <MetricTile icon={<BriefcaseBusiness size={20} />} label="Thưởng dự án" value={formatMoney(projectBonus)} />
            <MetricTile icon={<Clock3 size={20} />} label="Làm thêm giờ" value={formatMoney(overtimePay)} />
            <MetricTile icon={<Clock3 size={20} />} label="Giờ công CTV" value={formatMoney(externalTimesheet)} />
          </div>

          <div className="grid gap-6 xl:grid-cols-[1.25fr_0.75fr]">
            <Card
              title="Cơ cấu lương"
              description="Các khoản thu nhập và khấu trừ đã được ghi nhận trong kỳ."
            >
              <div className="grid gap-6 lg:grid-cols-2">
                <BreakdownSection title="Thu nhập" items={incomeDetails} fallbackAmount={slip.grossIncome} />
                <BreakdownSection
                  title="Khấu trừ"
                  items={deductionDetails}
                  fallbackItems={[
                    { label: "Bảo hiểm người lao động", amount: slip.employeeInsuranceAmount },
                    { label: "Thuế TNCN", amount: slip.pitAmount },
                    { label: "Khấu trừ khác", amount: slip.otherDeductions },
                  ]}
                />
              </div>
            </Card>

            <Card title="Tỉ trọng thực nhận" description="So sánh phần thực nhận với tổng thu nhập trước khấu trừ.">
              <div className="flex flex-col items-center gap-5">
                <DonutChart
                  netRatio={payroll.summary?.netRatio ?? 0}
                  deductionRatio={payroll.summary?.deductionRatio ?? 0}
                />
                <div className="grid w-full gap-3">
                  <LegendRow color="bg-[var(--hicas-orange)]" label="Thực nhận" value={formatMoney(slip.netSalary)} />
                  <LegendRow color="bg-[var(--hicas-charcoal)]" label="Khấu trừ" value={formatMoney(totalDeductions)} />
                  <LegendRow color="bg-[var(--hicas-bg-soft)]" label="Tổng thu nhập" value={formatMoney(slip.grossIncome)} />
                </div>
              </div>
            </Card>
          </div>

          <div className="grid gap-6 xl:grid-cols-[0.9fr_1.1fr]">
            <Card title="Công và bảo hiểm" description="Dữ liệu được lấy từ bảng công, hợp đồng và chính sách đang áp dụng.">
              <div className="grid gap-3 sm:grid-cols-2">
                <CompactMetric icon={<CalendarDays size={18} />} label="Công chuẩn" value={`${formatNumber(slip.standardWorkDays)} công`} />
                <CompactMetric icon={<CalendarDays size={18} />} label="Công thực tế" value={`${formatNumber(slip.actualWorkDays)} công`} />
                <CompactMetric icon={<Clock3 size={18} />} label="Giờ tính lương" value={`${formatNumber(slip.payableWorkHours)} giờ`} />
                <CompactMetric icon={<Clock3 size={18} />} label="Làm thêm" value={`${formatNumber((slip.actualOtMinutes || 0) / 60)} giờ`} />
                <CompactMetric icon={<Clock3 size={18} />} label="Đi muộn" value={`${slip.lateMinutes || 0} phút`} />
                <CompactMetric icon={<Clock3 size={18} />} label="Về sớm" value={`${slip.earlyLeaveMinutes || 0} phút`} />
                <CompactMetric icon={<ShieldCheck size={18} />} label="Lương bảo hiểm" value={formatMoney(slip.insuranceSalary)} />
                <CompactMetric icon={<WalletCards size={18} />} label="Công ty đóng" value={formatMoney(slip.employerContributionAmount)} />
              </div>
            </Card>

            <Card title="Chi tiết khoản lương" description="Danh sách dòng lương đã snapshot vào phiếu của kỳ này.">
              <div className="overflow-x-auto rounded-[var(--radius-md)] border border-[var(--hicas-border)]">
                <table className="min-w-full text-left text-sm">
                  <thead className="bg-[var(--hicas-bg-soft)] text-xs font-bold uppercase text-[var(--hicas-text-secondary)]">
                    <tr>
                      <th className="px-4 py-3">Khoản lương</th>
                      <th className="px-4 py-3 text-right">Số tiền</th>
                      <th className="px-4 py-3 text-right">Chịu thuế</th>
                      <th className="px-4 py-3 text-right">Tính bảo hiểm</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-[var(--hicas-border-soft)]">
                    {slip.details.map((detail) => (
                      <tr key={detail.id}>
                        <td className="px-4 py-3">
                          <p className="font-semibold text-[var(--hicas-text-main)]">{detail.componentName}</p>
                          <p className="text-xs text-[var(--hicas-text-secondary)]">{detail.componentCode}</p>
                        </td>
                        <td className="px-4 py-3 text-right font-bold">{formatMoney(detail.amount)}</td>
                        <td className="px-4 py-3 text-right">{formatMoney(detail.taxableAmount)}</td>
                        <td className="px-4 py-3 text-right">{formatMoney(detail.insuranceBaseAmount)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </Card>
          </div>

          <SourcePanels projectBonusSources={projectBonusSources} externalTimesheetSources={externalTimesheetSources} />

          {payroll.slips.length > 1 ? (
            <Card title="Phiếu trong cùng kỳ" description="Một kỳ có thể có nhiều bản khi bảng lương được tính lại trước lúc chốt.">
              <div className="grid gap-3">
                {payroll.slips.map((item) => (
                  <button
                    key={item.id}
                    type="button"
                    onClick={() => payroll.openSlip(item.id)}
                    className={`flex items-center justify-between gap-4 rounded-[var(--radius-md)] border px-4 py-3 text-left transition ${
                      item.id === slip.id
                        ? "border-[var(--hicas-orange)] bg-[var(--hicas-orange-lighter)]"
                        : "border-[var(--hicas-border)] bg-white hover:border-[var(--hicas-orange)]"
                    }`}
                  >
                    <span>
                      <span className="block font-semibold text-[var(--hicas-text-main)]">
                        Phiếu #{item.id} · {item.period}
                      </span>
                      <span className="mt-1 block text-sm text-[var(--hicas-text-secondary)]">
                        Tính lúc {formatDateTime(item.calculatedAt)}
                      </span>
                    </span>
                    <span className="flex items-center gap-3">
                      <StatusBadge
                        status={normalizePayrollStatus(item.status)}
                        label={getStatusLabel(item.status, item.statusText)}
                      />
                      <span className="font-bold text-[var(--hicas-text-main)]">{formatMoney(item.netSalary)}</span>
                    </span>
                  </button>
                ))}
              </div>
            </Card>
          ) : null}
        </>
      ) : (
        <Card>
          <div className="flex flex-col items-center justify-center px-6 py-16 text-center">
            <div className="flex h-14 w-14 items-center justify-center rounded-full bg-[var(--hicas-orange-soft)] text-[var(--hicas-orange)]">
              <FileText size={26} />
            </div>
            <h2 className="mt-4 text-xl font-bold text-[var(--hicas-text-main)]">Chưa có phiếu lương kỳ {period}</h2>
            <p className="mt-2 max-w-xl text-sm leading-6 text-[var(--hicas-text-secondary)]">
              Phiếu lương sẽ xuất hiện sau khi kỳ lương được tổng hợp, duyệt và phát hành theo quy trình.
            </p>
          </div>
        </Card>
      )}
    </FeaturePage>
  );
};

const HeroMetric = ({ label, value }: { label: string; value: string }) => (
  <div className="rounded-[var(--radius-lg)] border border-white/15 bg-white/10 p-4 backdrop-blur">
    <p className="text-xs font-semibold uppercase tracking-[0.12em] text-white/55">{label}</p>
    <p className="mt-2 text-lg font-extrabold text-white">{value}</p>
  </div>
);

const InfoLine = ({ label, value }: { label: string; value: string }) => (
  <div className="flex items-center justify-between gap-4 rounded-[var(--radius-md)] bg-[var(--hicas-bg-soft)] px-4 py-3">
    <span className="text-[var(--hicas-text-secondary)]">{label}</span>
    <span className="text-right font-bold text-[var(--hicas-text-main)]">{value}</span>
  </div>
);

const MetricTile = ({ icon, label, value }: { icon: ReactNode; label: string; value: string }) => (
  <div className="rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-white p-5 shadow-sm">
    <div className="flex items-start justify-between gap-4">
      <div>
        <p className="text-sm font-semibold text-[var(--hicas-text-secondary)]">{label}</p>
        <p className="mt-2 text-xl font-extrabold text-[var(--hicas-text-main)]">{value}</p>
      </div>
      <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-[var(--radius-md)] bg-[var(--hicas-orange-soft)] text-[var(--hicas-orange)]">
        {icon}
      </span>
    </div>
  </div>
);

const CompactMetric = ({ icon, label, value }: { icon: ReactNode; label: string; value: string }) => (
  <div className="flex items-start gap-3 rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4">
    <span className="mt-0.5 text-[var(--hicas-orange)]">{icon}</span>
    <span>
      <span className="block text-xs font-bold uppercase text-[var(--hicas-text-secondary)]">{label}</span>
      <span className="mt-1 block text-base font-bold text-[var(--hicas-text-main)]">{value}</span>
    </span>
  </div>
);

const BreakdownSection = ({
  title,
  items,
  fallbackAmount,
  fallbackItems,
}: {
  title: string;
  items: SalarySlipDetail[];
  fallbackAmount?: number;
  fallbackItems?: Array<{ label: string; amount: number }>;
}) => {
  const displayItems: Array<{ label: string; code?: string; amount: number }> = items.length
    ? items.map((item) => ({ label: item.componentName, code: item.componentCode, amount: item.amount }))
    : fallbackItems ?? (fallbackAmount ? [{ label: title, code: "", amount: fallbackAmount }] : []);
  const total = displayItems.reduce((sum, item) => sum + Math.abs(Number(item.amount) || 0), 0);

  return (
    <section>
      <div className="mb-4 flex items-center justify-between gap-4">
        <h3 className="text-base font-bold text-[var(--hicas-text-main)]">{title}</h3>
        <span className="font-bold text-[var(--hicas-orange)]">{formatMoney(total)}</span>
      </div>
      <div className="space-y-3">
        {displayItems.map((item, index) => {
          const percent = total > 0 ? Math.min(100, (Math.abs(Number(item.amount) || 0) / total) * 100) : 0;
          return (
            <div key={`${item.label}-${index}`}>
              <div className="mb-1 flex items-center justify-between gap-3 text-sm">
                <span className="font-semibold text-[var(--hicas-text-main)]">{item.label}</span>
                <span className="font-bold">{formatMoney(Math.abs(Number(item.amount) || 0))}</span>
              </div>
              {item.code ? <p className="mb-2 text-xs text-[var(--hicas-text-secondary)]">{item.code}</p> : null}
              <div className="h-2 overflow-hidden rounded-full bg-[var(--hicas-bg-soft)]">
                <div className="h-full rounded-full bg-[var(--hicas-orange)]" style={{ width: `${percent}%` }} />
              </div>
            </div>
          );
        })}
      </div>
    </section>
  );
};

const DonutChart = ({ netRatio, deductionRatio }: { netRatio: number; deductionRatio: number }) => (
  <div
    className="relative flex h-48 w-48 items-center justify-center rounded-full"
    style={{
      background: `conic-gradient(var(--hicas-orange) 0 ${netRatio}%, var(--hicas-charcoal) ${netRatio}% ${
        netRatio + deductionRatio
      }%, var(--hicas-bg-soft) ${netRatio + deductionRatio}% 100%)`,
    }}
  >
    <div className="flex h-28 w-28 flex-col items-center justify-center rounded-full bg-white shadow-inner">
      <span className="text-3xl font-extrabold text-[var(--hicas-text-main)]">{formatNumber(netRatio)}%</span>
      <span className="mt-1 text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">thực nhận</span>
    </div>
  </div>
);

const LegendRow = ({ color, label, value }: { color: string; label: string; value: string }) => (
  <div className="flex items-center justify-between gap-4 text-sm">
    <span className="flex items-center gap-2 text-[var(--hicas-text-secondary)]">
      <span className={`h-3 w-3 rounded-full ${color}`} />
      {label}
    </span>
    <span className="font-bold text-[var(--hicas-text-main)]">{value}</span>
  </div>
);

const SourcePanels = ({
  projectBonusSources,
  externalTimesheetSources,
}: {
  projectBonusSources: ProjectBonusSource[];
  externalTimesheetSources: ExternalTimesheetSource[];
}) => {
  if (!projectBonusSources.length && !externalTimesheetSources.length) return null;

  return (
    <div className="grid gap-6 xl:grid-cols-2">
      {projectBonusSources.length ? (
        <Card title="Nguồn thưởng dự án" description="Các khoản thưởng dự án đã được duyệt và đưa vào kỳ lương.">
          <SourceTable
            headers={["Dự án", "Số tiền", "Thuế", "Bảo hiểm"]}
            rows={projectBonusSources.map((source) => [
              <span key="project">
                <span className="block font-semibold text-[var(--hicas-text-main)]">{source.projectName}</span>
                <span className="text-xs text-[var(--hicas-text-secondary)]">{source.projectCode}</span>
              </span>,
              formatMoney(source.bonusAmount),
              source.taxable ? "Có" : "Không",
              source.insuranceContributable ? "Có" : "Không",
            ])}
          />
        </Card>
      ) : null}

      {externalTimesheetSources.length ? (
        <Card title="Nguồn giờ công cộng tác viên" description="Giờ công đã duyệt được đưa vào phần thù lao trong kỳ.">
          <SourceTable
            headers={["Ngày", "Dự án", "Giờ", "Thành tiền"]}
            rows={externalTimesheetSources.map((source) => [
              formatDate(source.workDate),
              <span key="project">
                <span className="block font-semibold text-[var(--hicas-text-main)]">{source.projectCode}</span>
                <span className="text-xs text-[var(--hicas-text-secondary)]">{source.taskCode}</span>
              </span>,
              formatNumber(source.approvedHours),
              formatMoney(source.amount),
            ])}
          />
        </Card>
      ) : null}
    </div>
  );
};

const SourceTable = ({ headers, rows }: { headers: string[]; rows: ReactNode[][] }) => (
  <div className="overflow-x-auto rounded-[var(--radius-md)] border border-[var(--hicas-border)]">
    <table className="min-w-full text-left text-sm">
      <thead className="bg-[var(--hicas-bg-soft)] text-xs font-bold uppercase text-[var(--hicas-text-secondary)]">
        <tr>
          {headers.map((header) => (
            <th key={header} className="px-4 py-3">
              {header}
            </th>
          ))}
        </tr>
      </thead>
      <tbody className="divide-y divide-[var(--hicas-border-soft)]">
        {rows.map((row, rowIndex) => (
          <tr key={rowIndex}>
            {row.map((cell, cellIndex) => (
              <td key={cellIndex} className="px-4 py-3">
                {cell}
              </td>
            ))}
          </tr>
        ))}
      </tbody>
    </table>
  </div>
);

const getIncomeDetails = (slip?: SalarySlip | null) => {
  if (!slip) return [];
  return slip.details
    .filter((detail) => detail.isIncome && !detail.isDeduction && Number(detail.amount) > 0)
    .sort((a, b) => {
      const aImportant = importantIncomeCodes.has(a.componentCode) ? 0 : 1;
      const bImportant = importantIncomeCodes.has(b.componentCode) ? 0 : 1;
      return aImportant - bImportant || Math.abs(b.amount) - Math.abs(a.amount);
    });
};

const getDeductionDetails = (slip?: SalarySlip | null) => {
  if (!slip) return [];
  return slip.details
    .filter((detail) => detail.isDeduction || Number(detail.amount) < 0)
    .sort((a, b) => Math.abs(b.amount) - Math.abs(a.amount));
};

const getProjectBonusSources = (slip?: SalarySlip | null) =>
  slip?.details.flatMap((detail) => detail.projectBonusSources ?? []) ?? [];

const getExternalTimesheetSources = (slip?: SalarySlip | null) =>
  slip?.details.flatMap((detail) => detail.externalTimesheetSources ?? []) ?? [];

const sumByCodes = (slip: SalarySlip | null | undefined, codes: string[]) => {
  if (!slip) return 0;
  const normalized = new Set(codes.map((code) => code.toUpperCase()));
  return slip.details
    .filter((detail) => normalized.has(detail.componentCode.toUpperCase()))
    .reduce((sum, detail) => sum + Number(detail.amount || 0), 0);
};

const getStatusLabel = (status: SalarySlip["status"], fallbackLabel?: string | null) => {
  const normalized = normalizePayrollStatus(status);
  return fallbackLabel || statusLabel[normalized] || getPayrollStatusLabel(status);
};

const formatDate = (value?: string | null) => {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toLocaleDateString("vi-VN");
};

const formatDateTime = (value?: string | null) => {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toLocaleString("vi-VN");
};

const csvCell = (value: unknown) => `"${String(value ?? "").replace(/"/g, '""')}"`;

const buildPersonalSlipCsv = (slip: SalarySlip) => {
  const rows = [
    ["Kỳ lương", slip.period],
    ["Nhân sự", slip.employeeName],
    ["Mã nhân viên", slip.employeeCode],
    ["Phòng ban", slip.departmentName ?? ""],
    ["Chức danh", slip.positionName ?? ""],
    ["Tổng thu nhập", slip.grossIncome],
    ["Bảo hiểm người lao động", slip.employeeInsuranceAmount],
    ["Thuế TNCN", slip.pitAmount],
    ["Khấu trừ khác", slip.otherDeductions],
    ["Thực nhận", slip.netSalary],
    [],
    ["Mã khoản", "Tên khoản", "Số tiền", "Chịu thuế", "Tính bảo hiểm"],
    ...slip.details.map((detail) => [
      detail.componentCode,
      detail.componentName,
      detail.amount,
      detail.taxableAmount,
      detail.insuranceBaseAmount,
    ]),
  ];

  return rows.map((row) => row.map(csvCell).join(",")).join("\n");
};
