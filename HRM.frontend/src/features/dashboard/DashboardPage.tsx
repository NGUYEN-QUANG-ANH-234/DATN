import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import {
  Activity,
  AlertTriangle,
  ArrowLeft,
  ArrowRight,
  BriefcaseBusiness,
  CalendarCheck,
  CheckCircle2,
  ChevronRight,
  ClipboardCheck,
  Clock3,
  FileText,
  GraduationCap,
  LineChart as LineChartIcon,
  Loader2,
  RefreshCw,
  ShieldCheck,
  Target,
  UserRoundCheck,
  UsersRound,
  WalletCards,
} from "lucide-react";
import {
  EmptyState,
  FeatureCard,
  FeaturePage,
  primaryButtonClass,
  secondaryButtonClass,
} from "../../core/components/FeatureShell";
import { DrawerForm } from "../../components/ui";
import { dashboardApi } from "./api/dashboardApi";
import type {
  ApiResponse,
  DashboardDrilldown,
  DashboardResponse,
  DashboardSection,
  DashboardSeverity,
  DashboardTable,
  DashboardWidget,
} from "./types/dashboard";

const HICAS_CHART = {
  orange: "#f97316",
  orangeDark: "#ea580c",
  orangeSoft: "#fed7aa",
  orangePale: "#fff7ed",
  charcoal: "#111827",
  slate: "#475569",
  slateSoft: "#e2e8f0",
  teal: "#0f766e",
  sky: "#0284c7",
  amber: "#d97706",
  green: "#16a34a",
  red: "#dc2626",
};

const CHART_COLORS = [
  HICAS_CHART.orange,
  HICAS_CHART.charcoal,
  HICAS_CHART.teal,
  HICAS_CHART.sky,
  HICAS_CHART.amber,
  HICAS_CHART.slate,
  HICAS_CHART.green,
  HICAS_CHART.red,
  "#7c3aed",
];

const SEVERITY_COLORS: Record<DashboardSeverity, string> = {
  neutral: HICAS_CHART.slate,
  success: HICAS_CHART.green,
  info: HICAS_CHART.sky,
  warning: HICAS_CHART.orange,
  danger: HICAS_CHART.red,
};

const unwrap = <T,>(response: ApiResponse<T>): T => {
  const data = response.data ?? response.Data;
  if (!data) {
    throw new Error(response.message || response.Message || "Chưa có dữ liệu tổng quan.");
  }
  return data;
};

const currentPeriod = () => {
  const now = new Date();
  return {
    month: now.getMonth() + 1,
    year: now.getFullYear(),
  };
};

const DashboardPage = () => {
  const navigate = useNavigate();
  const [period, setPeriod] = useState(currentPeriod);
  const [dashboard, setDashboard] = useState<DashboardResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [selectedWidget, setSelectedWidget] = useState<DashboardWidget | null>(null);
  const [drilldown, setDrilldown] = useState<DashboardDrilldown | null>(null);
  const [drilldownLoading, setDrilldownLoading] = useState(false);

  const periodLabel = useMemo(
    () => `${String(period.month).padStart(2, "0")}/${period.year}`,
    [period],
  );

  const fetchDashboard = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const response = await dashboardApi.getDashboard(period.month, period.year);
      setDashboard(unwrap(response));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể tải dữ liệu tổng quan.");
    } finally {
      setLoading(false);
    }
  }, [period.month, period.year]);

  useEffect(() => {
    void fetchDashboard();
  }, [fetchDashboard]);

  const changeMonth = (step: number) => {
    setPeriod((current) => {
      const date = new Date(current.year, current.month - 1 + step, 1);
      return { month: date.getMonth() + 1, year: date.getFullYear() };
    });
  };

  const openDrilldown = async (widget: DashboardWidget) => {
    if (!widget.drilldown?.type) return;

    setSelectedWidget(widget);
    setDrilldown(null);
    setDrilldownLoading(true);

    try {
      const response = await dashboardApi.getDrilldown(widget.drilldown.type, {
        month: period.month,
        year: period.year,
        scope: widget.drilldown.scope,
      });
      setDrilldown(unwrap(response));
    } catch (err) {
      setDrilldown({
        type: widget.drilldown.type,
        scope: widget.scope,
        title: widget.title,
        metrics: [],
        table: {
          columns: ["Nội dung"],
          rows: [
            {
              "Nội dung":
                err instanceof Error
                  ? err.message
                  : "Không thể tải dữ liệu chi tiết.",
            },
          ],
        },
      });
    } finally {
      setDrilldownLoading(false);
    }
  };

  const openSectionDrilldown = (section: DashboardSection) => {
    if (!section.table) return;

    setSelectedWidget({
      id: section.id,
      title: section.title,
      value: "",
      subtitle: section.subtitle,
      severity: "neutral",
      scope: dashboard?.scope || "",
      order: section.order,
      drilldown: null,
      metrics: [],
      actions: [],
    });
    setDrilldown({
      type: section.id,
      scope: dashboard?.scope || "",
      title: section.title,
      metrics: [],
      table: section.table,
    });
  };

  const closeDrilldown = () => {
    setSelectedWidget(null);
    setDrilldown(null);
    setDrilldownLoading(false);
  };

  return (
    <FeaturePage
      title="Tổng quan"
      description="Theo dõi nhanh các chỉ số cần chú ý trong kỳ."
      actions={
        <div className="flex flex-wrap items-center gap-2">
          <button type="button" className={secondaryButtonClass} onClick={() => changeMonth(-1)}>
            <ArrowLeft size={16} />
            Tháng trước
          </button>
          <div className="inline-flex min-h-[42px] items-center rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white px-4 text-sm font-semibold text-[var(--hicas-text-main)]">
            {periodLabel}
          </div>
          <button type="button" className={secondaryButtonClass} onClick={() => changeMonth(1)}>
            Tháng sau
            <ArrowRight size={16} />
          </button>
          <button type="button" className={primaryButtonClass} onClick={fetchDashboard} disabled={loading}>
            {loading ? <Loader2 size={16} className="animate-spin" /> : <RefreshCw size={16} />}
            Làm mới
          </button>
        </div>
      }
    >
      {loading && (
        <div className="flex min-h-64 items-center justify-center rounded-[var(--radius-md)] border border-dashed border-[var(--hicas-border)] bg-white">
          <div className="flex items-center gap-2 text-sm font-semibold text-[var(--hicas-text-secondary)]">
            <Loader2 size={18} className="animate-spin text-[var(--hicas-orange)]" />
            Đang tải dữ liệu...
          </div>
        </div>
      )}

      {!loading && error && (
        <FeatureCard>
          <EmptyState title="Không thể tải dữ liệu" description={error} />
        </FeatureCard>
      )}

      {!loading && !error && dashboard && (
        <>
          <DashboardChartBoard
            dashboard={dashboard}
            onOpenWidget={openDrilldown}
            onOpenSection={openSectionDrilldown}
          />

          {dashboard.quickActions.length > 0 && (
            <section className="flex flex-wrap gap-2">
              {dashboard.quickActions.map((action) => (
                <button
                  key={`${action.route}-${action.label}`}
                  type="button"
                  onClick={() => navigate(action.route)}
                  className="inline-flex min-h-[42px] items-center gap-2 rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white px-4 text-sm font-semibold text-[var(--hicas-text-main)] transition hover:border-[var(--hicas-orange)] hover:bg-[var(--hicas-orange-lighter)] hover:text-[var(--hicas-orange-dark)]"
                >
                  {actionIcon(action.icon)}
                  {action.label}
                </button>
              ))}
            </section>
          )}

          <section className="grid gap-5 2xl:grid-cols-2">
            {dashboard.sections.map((section) => (
              <SectionChartCard
                key={section.id}
                section={section}
                onOpen={() => openSectionDrilldown(section)}
              />
            ))}
          </section>
        </>
      )}

      <DashboardDrilldownDrawer
        widget={selectedWidget}
        drilldown={drilldown}
        loading={drilldownLoading}
        open={Boolean(selectedWidget)}
        onClose={closeDrilldown}
      />
    </FeaturePage>
  );
};

const DashboardChartBoard = ({
  dashboard,
  onOpenWidget,
  onOpenSection,
}: {
  dashboard: DashboardResponse;
  onOpenWidget: (widget: DashboardWidget) => void;
  onOpenSection: (section: DashboardSection) => void;
}) => {
  const widgetData = dashboard.widgets.map((widget, index) => {
    const parsed = parseDisplayValue(widget.value);
    return {
      name: compactLabel(widget.title),
      fullName: widget.title,
      value: parsed.chartValue,
      display: widget.value,
      unit: parsed.unit,
      severity: normalizeSeverity(widget.severity),
      fill: severityColor(widget.severity, index),
      widget,
    };
  });

  const severityData = Object.entries(
    dashboard.widgets.reduce<Record<string, number>>((acc, widget) => {
      const key = normalizeSeverity(widget.severity);
      acc[key] = (acc[key] || 0) + 1;
      return acc;
    }, {}),
  ).map(([severity, value], index) => {
    const normalizedSeverity = normalizeSeverity(severity);
    return {
      name: severityLabel(normalizedSeverity),
      severity: normalizedSeverity,
      value,
      fill: severityColor(normalizedSeverity, index),
    };
  });

  const sectionData = dashboard.sections.map((section, index) => ({
    name: compactLabel(section.title),
    fullName: section.title,
    value: section.table?.rows.length || section.widgets.length || 0,
    fill: CHART_COLORS[index % CHART_COLORS.length],
    section,
  }));

  return (
    <section className="grid items-start gap-5 xl:grid-cols-[minmax(0,1.35fr)_minmax(340px,0.65fr)]">
      <FeatureCard
        title="Chỉ số trọng tâm"
        description="Theo dõi các chỉ số chính trong kỳ và mở nhanh dữ liệu phía sau."
        className="overflow-hidden border-[var(--hicas-orange-soft)] bg-[linear-gradient(180deg,#ffffff_0%,#fff7ed_100%)]"
      >
        <MetricSignalGrid
          data={widgetData}
          onSelect={(item) => {
            if (item.widget) onOpenWidget(item.widget);
          }}
        />
      </FeatureCard>

      <div className="grid gap-5">
        <FeatureCard
          title="Mức ưu tiên"
          description="Phân nhóm chỉ số theo mức cần chú ý."
          className="overflow-hidden bg-[linear-gradient(180deg,#ffffff_0%,#f8fafc_100%)]"
        >
          <div className="grid items-center gap-4 sm:grid-cols-[150px_minmax(0,1fr)] xl:grid-cols-1 2xl:grid-cols-[150px_minmax(0,1fr)]">
            <div className="h-40 min-w-0">
              <DonutChartSvg data={severityData} />
            </div>
            <ChartLegend data={severityData.map((item) => ({ label: item.name, color: item.fill, value: `${item.value}` }))} />
          </div>
        </FeatureCard>

        <FeatureCard
          title="Bản đồ dữ liệu"
          description="Mở nhanh các cụm có dữ liệu cần đối chiếu."
          className="overflow-hidden bg-[linear-gradient(180deg,#ffffff_0%,#fff7ed_100%)]"
        >
          <TileMapChart
            data={sectionData}
            onSelect={(item) => {
              if (item.section?.table) onOpenSection(item.section);
            }}
          />
        </FeatureCard>
      </div>
    </section>
  );
};

const SectionChartCard = ({
  section,
  onOpen,
}: {
  section: DashboardSection;
  onOpen: () => void;
}) => {
  const chart = buildSectionChart(section);

  return (
    <FeatureCard
      title={section.title}
      description={section.subtitle || "Bấm biểu đồ để xem dữ liệu chi tiết."}
      className="overflow-hidden"
      actions={
        section.table ? (
          <button type="button" className={secondaryButtonClass} onClick={onOpen}>
            Mở chi tiết
            <ChevronRight size={16} />
          </button>
        ) : null
      }
    >
      {!chart ? (
        <EmptyState title="Chưa có dữ liệu" />
      ) : chart.kind === "bar" ? (
        <div className="h-80 min-w-0">
          <HorizontalBarChartSvg
            data={chart.data.map((item, index) => ({
              ...item,
              fill: CHART_COLORS[index % CHART_COLORS.length],
            }))}
            valueFormatter={(item) => chartTooltipValue(item.value, chart.unit)}
            metricLabel={chart.metricLabel}
            onSelect={onOpen}
          />
        </div>
      ) : chart.kind === "funnel" ? (
        <FunnelChart
          data={chart.data.map((item, index) => ({
            ...item,
            fill: CHART_COLORS[index % CHART_COLORS.length],
          }))}
          metricLabel={chart.metricLabel}
          unit={chart.unit}
          onSelect={onOpen}
        />
      ) : (
        <div className="grid gap-4 md:grid-cols-[220px_minmax(0,1fr)]">
          <div className="h-60">
            <DonutChartSvg
              data={chart.data.map((item, index) => ({
                ...item,
                fill: CHART_COLORS[index % CHART_COLORS.length],
              }))}
              onSelect={onOpen}
            />
          </div>
          <ChartLegend
            data={chart.data.map((item, index) => ({
              label: item.name,
              value: `${item.value}`,
              color: CHART_COLORS[index % CHART_COLORS.length],
            }))}
          />
        </div>
      )}
    </FeatureCard>
  );
};

const DashboardDrilldownDrawer = ({
  widget,
  drilldown,
  loading,
  open,
  onClose,
}: {
  widget: DashboardWidget | null;
  drilldown: DashboardDrilldown | null;
  loading: boolean;
  open: boolean;
  onClose: () => void;
}) => {
  const context = getDrilldownContext(drilldown?.type || widget?.drilldown?.type || widget?.id);

  return (
    <DrawerForm
      open={open}
      title={drilldown?.title || widget?.title || "Chi tiết"}
      description={drilldown?.scope || widget?.scope || "Dữ liệu theo phạm vi bạn được xem."}
      width="xl"
      onClose={onClose}
      footer={
        <button type="button" className={secondaryButtonClass} onClick={onClose}>
          Đóng
        </button>
      }
    >
      {loading && (
        <div className="flex min-h-60 items-center justify-center text-sm font-semibold text-[var(--hicas-text-secondary)]">
          <Loader2 size={18} className="mr-2 animate-spin text-[var(--hicas-orange)]" />
          Đang tải chi tiết...
        </div>
      )}

      {!loading && drilldown && (
        <div className="space-y-5">
          <DrilldownContextPanel context={context} />

          {drilldown.metrics.length > 0 && (
            <div className="grid gap-3 sm:grid-cols-3">
              {drilldown.metrics.map((metric) => (
                <div
                  key={metric.label}
                  className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4 shadow-sm"
                >
                  <p className="text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">
                    {metric.label}
                  </p>
                  <p className="mt-2 break-words text-lg font-bold text-[var(--hicas-text-main)]">
                    {metric.value}
                  </p>
                </div>
              ))}
            </div>
          )}
          <DashboardTableView table={drilldown.table} />
        </div>
      )}
    </DrawerForm>
  );
};

const DrilldownContextPanel = ({
  context,
}: {
  context: DrilldownContext;
}) => (
  <div className="grid gap-3 lg:grid-cols-3">
    <div className="rounded-[var(--radius-md)] border border-[var(--hicas-orange-soft)] bg-[var(--hicas-orange-lighter)] p-4">
      <p className="text-xs font-semibold uppercase text-[var(--hicas-orange-dark)]">Cần xem</p>
      <p className="mt-2 text-sm leading-6 text-[var(--hicas-text-main)]">{context.summary}</p>
    </div>
    <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4">
      <p className="text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">Nguồn dữ liệu</p>
      <p className="mt-2 text-sm leading-6 text-[var(--hicas-text-main)]">{context.source}</p>
    </div>
    <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4">
      <p className="text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">Việc tiếp theo</p>
      <p className="mt-2 text-sm leading-6 text-[var(--hicas-text-main)]">{context.action}</p>
    </div>
  </div>
);

const DashboardTableView = ({ table }: { table: DashboardTable }) => {
  if (!table.rows.length) {
    return <EmptyState title="Chưa có dữ liệu" />;
  }

  return (
    <div className="overflow-x-auto rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white">
      <table className="min-w-[920px] border-separate border-spacing-0 text-sm">
        <thead className="bg-[var(--hicas-bg-soft)]">
          <tr>
            {table.columns.map((column, index) => (
              <th
                key={column}
                className={`border-b border-[var(--hicas-border-soft)] px-4 py-3 text-left text-xs font-semibold text-[var(--hicas-text-secondary)] ${
                  index === 0 ? "sticky left-0 z-10 min-w-[190px] bg-[var(--hicas-bg-soft)]" : "min-w-[160px]"
                }`}
              >
                {column}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-[var(--hicas-border-soft)] bg-white">
          {table.rows.map((row, index) => (
            <tr key={`${index}-${table.columns[0]}`} className="align-top transition hover:bg-[var(--hicas-orange-lighter)]/60">
              {table.columns.map((column, columnIndex) => (
                <td
                  key={column}
                  className={`border-b border-[var(--hicas-border-soft)] px-4 py-3 leading-6 text-[var(--hicas-text-main)] ${
                    columnIndex === 0
                      ? "sticky left-0 z-10 min-w-[190px] bg-white font-semibold"
                      : "min-w-[160px] max-w-[360px]"
                  }`}
                >
                  <span className="block whitespace-normal break-words">{row[column] || "-"}</span>
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

const ChartLegend = ({
  data,
}: {
  data: Array<{ label: string; color: string; value?: string }>;
}) => (
  <div className="grid gap-2 text-sm">
    {data.map((item) => (
      <div key={item.label} className="flex items-start justify-between gap-3">
        <span className="inline-flex min-w-0 items-start gap-2 text-[var(--hicas-text-secondary)]">
          <span className="h-2.5 w-2.5 shrink-0 rounded-full" style={{ backgroundColor: item.color }} />
          <span className="break-words leading-5">{item.label}</span>
        </span>
        {item.value && <span className="shrink-0 font-semibold text-[var(--hicas-text-main)]">{item.value}</span>}
      </div>
    ))}
  </div>
);

const MetricSignalGrid = ({
  data,
  onSelect,
}: {
  data: SvgChartItem[];
  onSelect?: (item: SvgChartItem) => void;
}) => {
  if (!data.length) return <EmptyState title="Chưa có dữ liệu" />;

  const max = Math.max(...data.map((item) => item.value), 1);

  return (
    <div className="grid gap-3 lg:grid-cols-2">
      {data.map((item, index) => {
        const clickable = Boolean(item.widget?.drilldown?.type);
        const ratio = item.value / max;
        const barWidth = item.value > 0 ? Math.max(8, ratio * 100) : 0;
        const severity = item.widget?.severity || item.severity;

        return (
          <button
            key={`${item.fullName || item.name}-${index}`}
            type="button"
            onClick={clickable ? () => onSelect?.(item) : undefined}
            disabled={!clickable}
            className={`min-h-[132px] rounded-[var(--radius-md)] border bg-white p-4 text-left shadow-sm transition ${
              clickable
                ? "border-[var(--hicas-border)] hover:-translate-y-0.5 hover:border-[var(--hicas-orange)] hover:shadow-[var(--shadow-hover)]"
                : "cursor-default border-[var(--hicas-border-soft)]"
            }`}
          >
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="break-words text-base font-bold leading-6 text-[var(--hicas-text-main)]">
                  {item.fullName || item.name}
                </p>
                <p className="mt-2 break-words text-3xl font-extrabold tracking-tight text-[var(--hicas-text-main)]">
                  {item.widget?.value || chartTooltipValue(item.value, item.unit)}
                </p>
              </div>
              <span className={`inline-flex h-11 w-11 shrink-0 items-center justify-center rounded-xl ${severityIconClass(severity)}`}>
                {item.widget ? widgetIcon(item.widget.id, item.widget.drilldown?.type) : <LineChartIcon size={22} />}
              </span>
            </div>

            {item.widget?.subtitle && (
              <p className="mt-2 min-h-6 break-words text-sm leading-5 text-[var(--hicas-text-secondary)]">
                {item.widget.subtitle}
              </p>
            )}

            <div className="mt-3 h-2.5 rounded-full bg-[var(--hicas-orange-lighter)]">
              <div
                className="h-full rounded-full"
                style={{
                  width: `${barWidth}%`,
                  backgroundColor: item.fill,
                }}
              />
            </div>

            <div className="mt-3 flex items-center justify-between gap-3 text-xs font-bold">
              <span className={`rounded-md px-2 py-1 ${severityBadgeClass(severity)}`}>
                {severityLabel(severity)}
              </span>
              {clickable && (
                <span className="inline-flex items-center gap-1 text-[var(--hicas-orange-dark)]">
                  Xem sâu <ChevronRight size={14} />
                </span>
              )}
            </div>
          </button>
        );
      })}
    </div>
  );
};

const TileMapChart = ({
  data,
  onSelect,
}: {
  data: SvgChartItem[];
  onSelect?: (item: SvgChartItem) => void;
}) => {
  if (!data.length) return <EmptyState title="Chưa có dữ liệu" />;

  const max = Math.max(...data.map((item) => item.value), 1);

  return (
    <div className="grid gap-2">
      {data.slice(0, 6).map((item, index) => {
        const ratio = item.value / max;
        const clickable = Boolean(onSelect && item.section?.table);

        return (
          <button
            key={`${item.fullName || item.name}-${index}`}
            type="button"
            onClick={clickable ? () => onSelect?.(item) : undefined}
            disabled={!clickable}
            className={`grid min-h-[72px] grid-cols-[48px_minmax(0,1fr)_auto] items-center gap-3 rounded-[var(--radius-md)] border p-3 text-left transition ${
              clickable
                ? "border-[var(--hicas-border)] bg-white hover:border-[var(--hicas-orange)] hover:bg-[var(--hicas-orange-lighter)]"
                : "cursor-default border-[var(--hicas-border-soft)] bg-white"
            }`}
          >
            <span className="flex h-12 w-12 items-center justify-center rounded-xl bg-[var(--hicas-orange-lighter)]">
              <span
                className="block h-7 rounded-full"
                style={{
                  width: `${Math.max(8, ratio * 30)}px`,
                  backgroundColor: item.fill,
                }}
              />
            </span>
            <span className="min-w-0">
              <span className="block break-words text-sm font-bold leading-5 text-[var(--hicas-text-main)]">
                {item.fullName || item.name}
              </span>
              <span className="mt-1 block text-xs font-semibold text-[var(--hicas-text-secondary)]">
                {item.value.toLocaleString("vi-VN")} dòng dữ liệu
              </span>
            </span>
            {clickable && <ChevronRight size={16} className="text-[var(--hicas-orange-dark)]" />}
          </button>
        );
      })}
    </div>
  );
};

const FunnelChart = ({
  data,
  metricLabel,
  unit,
  onSelect,
}: {
  data: SvgChartItem[];
  metricLabel: string;
  unit: string;
  onSelect?: () => void;
}) => {
  if (!data.length) return <EmptyState title="Chưa có dữ liệu" />;

  const max = Math.max(...data.map((item) => item.value), 1);

  return (
    <div className="grid gap-3">
      <div className="flex items-center justify-between gap-3 text-xs font-semibold text-[var(--hicas-text-secondary)]">
        <span>{metricLabel}</span>
        <span>{unit || "Số lượng"}</span>
      </div>
      {data.slice(0, 7).map((item, index) => {
        const percent = Math.max(16, (item.value / max) * 100);

        return (
          <button
            key={`${item.name}-${index}`}
            type="button"
            onClick={onSelect}
            className="group grid gap-2 rounded-[var(--radius-md)] border border-[var(--hicas-border-soft)] bg-white p-3 text-left transition hover:border-[var(--hicas-orange)] hover:bg-[var(--hicas-orange-lighter)]"
          >
            <div className="flex items-center justify-between gap-3">
              <span className="min-w-0 break-words text-sm font-semibold text-[var(--hicas-text-main)]">
                {item.fullName || item.name}
              </span>
              <span className="shrink-0 text-sm font-bold text-[var(--hicas-text-main)]">
                {chartTooltipValue(item.value, unit)}
              </span>
            </div>
            <div className="h-8 rounded-full bg-[var(--hicas-bg-soft)] p-1">
              <div
                className="flex h-full items-center justify-end rounded-full px-3 text-xs font-bold text-white shadow-sm"
                style={{
                  width: `${percent}%`,
                  backgroundColor: item.fill,
                }}
              >
                {Math.round(percent)}%
              </div>
            </div>
          </button>
        );
      })}
    </div>
  );
};

type DrilldownContext = {
  summary: string;
  source: string;
  action: string;
};

type SvgChartItem = {
  name: string;
  fullName?: string;
  value: number;
  fill: string;
  unit?: string;
  severity?: DashboardSeverity;
  widget?: DashboardWidget;
  section?: DashboardSection;
};

const HorizontalBarChartSvg = ({
  data,
  valueFormatter,
  metricLabel,
  onSelect,
}: {
  data: SvgChartItem[];
  valueFormatter: (item: SvgChartItem) => string;
  metricLabel: string;
  onSelect?: () => void;
}) => {
  if (!data.length) return <EmptyState title="Chưa có dữ liệu" />;

  const width = 760;
  const rowHeight = 34;
  const margin = { top: 22, right: 104, bottom: 20, left: 150 };
  const height = margin.top + margin.bottom + data.length * rowHeight;
  const chartWidth = width - margin.left - margin.right;
  const max = Math.max(...data.map((item) => item.value), 1);

  return (
    <svg viewBox={`0 0 ${width} ${height}`} className="h-full w-full" role="img">
      <text x={margin.left} y={14} fontSize="12" fontWeight="700" fill={HICAS_CHART.slate}>
        {metricLabel}
      </text>
      {data.map((item, index) => {
        const y = margin.top + index * rowHeight;
        const barWidth = Math.max(3, (item.value / max) * chartWidth);

        return (
          <g key={`${item.name}-${index}`} onClick={onSelect} className={onSelect ? "cursor-pointer" : undefined}>
            <title>{`${item.name}: ${valueFormatter(item)}`}</title>
            <text x={margin.left - 12} y={y + 20} textAnchor="end" fontSize="12" fill={HICAS_CHART.slate}>
              {item.name}
            </text>
            <rect x={margin.left} y={y + 6} width={chartWidth} height="18" rx="9" fill="#fff7ed" />
            <rect x={margin.left} y={y + 6} width={barWidth} height="18" rx="9" fill={item.fill} />
            <text x={margin.left + chartWidth + 12} y={y + 20} fontSize="12" fontWeight="700" fill={HICAS_CHART.charcoal}>
              {valueFormatter(item)}
            </text>
          </g>
        );
      })}
    </svg>
  );
};

const DonutChartSvg = ({
  data,
  onSelect,
}: {
  data: SvgChartItem[];
  onSelect?: () => void;
}) => {
  const total = data.reduce((sum, item) => sum + item.value, 0);
  const radius = 62;
  const circumference = 2 * Math.PI * radius;
  let offset = 0;

  if (!data.length || total <= 0) {
    return <EmptyState title="Chưa có dữ liệu" />;
  }

  return (
    <svg viewBox="0 0 220 180" className="h-full w-full" role="img">
      <circle cx="110" cy="88" r={radius} fill="none" stroke="#fff7ed" strokeWidth="28" />
      {data.map((item, index) => {
        const value = (item.value / total) * circumference;
        const strokeDasharray = `${value} ${circumference - value}`;
        const strokeDashoffset = -offset;
        offset += value;

        return (
          <circle
            key={`${item.name}-${index}`}
            cx="110"
            cy="88"
            r={radius}
            fill="none"
            stroke={item.fill}
            strokeWidth="28"
            strokeDasharray={strokeDasharray}
            strokeDashoffset={strokeDashoffset}
            strokeLinecap="round"
            transform="rotate(-90 110 88)"
            onClick={onSelect}
            className={onSelect ? "cursor-pointer" : undefined}
          >
            <title>{`${item.name}: ${item.value}`}</title>
          </circle>
        );
      })}
      <text x="110" y="82" textAnchor="middle" fontSize="24" fontWeight="800" fill={HICAS_CHART.charcoal}>
        {total}
      </text>
      <text x="110" y="104" textAnchor="middle" fontSize="12" fill={HICAS_CHART.slate}>
        tổng mục
      </text>
    </svg>
  );
};

const buildSectionChart = (section: DashboardSection):
  | {
      kind: "bar";
      data: Array<{ name: string; value: number }>;
      metricLabel: string;
      unit: string;
    }
  | {
      kind: "funnel";
      data: Array<{ name: string; fullName?: string; value: number }>;
      metricLabel: string;
      unit: string;
    }
  | {
      kind: "pie";
      data: Array<{ name: string; value: number }>;
    }
  | null => {
  const table = section.table;
  if (!table || table.rows.length === 0) return null;

  const metricColumn = findBestMetricColumn(table);
  const nameColumn = table.columns[0];
  const sectionChartKind = inferSectionChartKind(section);

  if (metricColumn) {
    const unit = inferColumnUnit(metricColumn, table.rows[0]?.[metricColumn]);
    const data = table.rows
      .map((row) => ({
        name: compactLabel(String(row[nameColumn] || "-"), 18),
        fullName: String(row[nameColumn] || "-"),
        value: parseDisplayValue(String(row[metricColumn] || "0")).chartValue,
      }))
      .filter((item) => item.value > 0)
      .slice(0, 8);

    if (data.length > 0) {
      return sectionChartKind === "funnel"
        ? {
            kind: "funnel",
            data,
            metricLabel: metricColumn,
            unit,
          }
        : {
            kind: "bar",
            data,
            metricLabel: metricColumn,
            unit,
          };
    }
  }

  const statusColumn = findStatusColumn(table);
  const grouped = table.rows.reduce<Record<string, number>>((acc, row) => {
    const key = String(row[statusColumn] || "Chưa xác định");
    acc[key] = (acc[key] || 0) + 1;
    return acc;
  }, {});

  const groupedData = Object.entries(grouped).map(([name, value]) => ({ name, value }));

  return sectionChartKind === "funnel"
    ? {
        kind: "funnel",
        data: groupedData,
        metricLabel: statusColumn,
        unit: "",
      }
    : {
        kind: "pie",
        data: groupedData,
      };
};

const inferSectionChartKind = (section: DashboardSection) => {
  const key = `${section.id} ${section.title} ${section.subtitle || ""}`.toLowerCase();
  if (
    key.includes("recruitment") ||
    key.includes("candidate") ||
    key.includes("tuyển") ||
    key.includes("ứng viên") ||
    key.includes("pipeline")
  ) {
    return "funnel";
  }
  return "bar";
};

const findBestMetricColumn = (table: DashboardTable) => {
  const preferred = [
    "Ứng viên",
    "Chỉ tiêu",
    "Tiến độ",
    "Công",
    "Đi muộn",
    "OT",
    "Mức đầy đủ",
    "Số phiếu",
    "Tổng chi phí",
    "DN đóng",
    "Gross",
    "Giờ duyệt",
    "Thành tiền",
  ];

  const candidates = table.columns.filter((column) =>
    table.rows.some((row) => parseDisplayValue(String(row[column] || "")).chartValue > 0),
  );

  return (
    preferred.find((column) => candidates.includes(column)) ||
    candidates.find((column) => !column.toLowerCase().includes("mã") && !column.toLowerCase().includes("ngày"))
  );
};

const findStatusColumn = (table: DashboardTable) =>
  table.columns.find((column) => column.toLowerCase().includes("trạng thái")) ||
  table.columns[table.columns.length - 1] ||
  table.columns[0];

const inferColumnUnit = (column: string, sample?: string | null) => {
  if (column.toLowerCase().includes("chi phí") || column.toLowerCase().includes("gross") || column.toLowerCase().includes("thành tiền")) {
    return "triệu đ";
  }
  if (String(sample || "").includes("%") || column.toLowerCase().includes("tiến độ") || column.toLowerCase().includes("đầy đủ")) {
    return "%";
  }
  if (column.toLowerCase().includes("ot") || column.toLowerCase().includes("giờ")) {
    return "giờ";
  }
  return "";
};

const parseDisplayValue = (value?: string | number | null) => {
  const text = String(value ?? "").trim();
  if (!text) return { chartValue: 0, gaugeValue: 0, unit: "" };

  if (text.includes("đ")) {
    const amount = Number(text.replace(/[^\d-]/g, ""));
    const million = Number.isFinite(amount) ? amount / 1_000_000 : 0;
    return {
      chartValue: Math.max(0, million),
      gaugeValue: Math.max(0, Math.min(100, million)),
      unit: "triệu đ",
    };
  }

  const match = text.replace(",", ".").match(/-?\d+(\.\d+)?/);
  const number = match ? Number(match[0]) : 0;
  const safe = Number.isFinite(number) ? Math.max(0, number) : 0;

  return {
    chartValue: safe,
    gaugeValue: text.includes("%") ? safe : Math.min(100, safe),
    unit: text.includes("%") ? "%" : "",
  };
};

const chartTooltipValue = (value: number, unit?: string) => {
  if (unit === "triệu đ") return `${value.toLocaleString("vi-VN", { maximumFractionDigits: 1 })} triệu đ`;
  if (unit === "%") return `${value.toLocaleString("vi-VN", { maximumFractionDigits: 1 })}%`;
  if (unit === "giờ") return `${value.toLocaleString("vi-VN", { maximumFractionDigits: 1 })} giờ`;
  return value.toLocaleString("vi-VN", { maximumFractionDigits: 1 });
};

const compactLabel = (value: string, max = 14) => {
  if (value.length <= max) return value;
  return `${value.slice(0, max - 1)}…`;
};

const getDrilldownContext = (type?: string | null): DrilldownContext => {
  const key = (type || "").toLowerCase();

  if (key.includes("payroll-preflight")) {
    return {
      summary: "Kiểm tra các lỗi có thể chặn chốt lương.",
      source: "Hồ sơ, hợp đồng, công, OT, nghỉ phép và chính sách lương.",
      action: "Mở từng dòng lỗi để xử lý tại phân hệ liên quan.",
    };
  }

  if (key.includes("payroll-slip") || key.includes("payroll-summary")) {
    return {
      summary: "Xem cấu phần lương và các khoản cộng trừ trong kỳ.",
      source: "Phiếu lương, hợp đồng, bảng công, OT, phụ cấp, thuế và bảo hiểm.",
      action: "Đối chiếu dòng lương bất thường hoặc tải phiếu khi cần.",
    };
  }

  if (key.includes("approval")) {
    return {
      summary: "Xem việc đang chờ quyết định và mức độ ưu tiên.",
      source: "Yêu cầu nghiệp vụ, người gửi, phòng ban, SLA và lịch sử xử lý.",
      action: "Mở hồ sơ liên quan trước khi duyệt, từ chối hoặc yêu cầu bổ sung.",
    };
  }

  if (key.includes("attendance") || key.includes("calendar")) {
    return {
      summary: "Đối chiếu lịch làm việc, công, nghỉ phép và OT.",
      source: "Lịch làm việc, chấm công, đơn nghỉ phép và yêu cầu làm thêm.",
      action: "Xử lý các dòng thiếu công, trễ hạn hoặc còn chờ duyệt.",
    };
  }

  if (key.includes("recruitment") || key.includes("candidate")) {
    return {
      summary: "Theo dõi tiến độ tuyển dụng và ứng viên đang chờ xử lý.",
      source: "Nhu cầu tuyển dụng, vị trí, pipeline ứng viên và lịch phỏng vấn.",
      action: "Mở ứng viên hoặc vị trí để gửi phản hồi, cập nhật bước tiếp theo.",
    };
  }

  if (key.includes("personnel")) {
    return {
      summary: "Xem tác động của thay đổi nhân sự trước khi thực thi.",
      source: "Hồ sơ nhân sự, phòng ban, chức danh, hợp đồng, lương và timeline.",
      action: "Kiểm tra rủi ro, phát hành quyết định hoặc tiếp tục bước xử lý.",
    };
  }

  if (key.includes("contract")) {
    return {
      summary: "Theo dõi vòng đời hợp đồng và phụ lục cần xử lý.",
      source: "Hồ sơ nhân sự, hợp đồng, phụ lục, phiên bản soạn thảo và phê duyệt.",
      action: "Mở hợp đồng để soạn, chỉnh sửa, duyệt hoặc phát hành.",
    };
  }

  if (key.includes("profile")) {
    return {
      summary: "Kiểm tra hồ sơ còn thiếu hoặc đang chờ xác nhận.",
      source: "Thông tin cá nhân, định danh, thuế, ngân hàng, bảo hiểm và tài liệu.",
      action: "Yêu cầu bổ sung hoặc mở hồ sơ để cập nhật dữ liệu.",
    };
  }

  if (key.includes("system") || key.includes("audit")) {
    return {
      summary: "Theo dõi cấu hình, quyền truy cập và thay đổi nhạy cảm.",
      source: "Cấu hình hệ thống, tài khoản, phân quyền, MFA và nhật ký thao tác.",
      action: "Mở cấu hình hoặc nhật ký để xử lý cảnh báo.",
    };
  }

  return {
    summary: "Xem dữ liệu phía sau chỉ số đang chọn.",
    source: "Các hồ sơ nghiệp vụ liên quan trong phạm vi bạn được xem.",
    action: "Mở dòng dữ liệu cần chú ý để xử lý tiếp.",
  };
};

const severityLabel = (severity?: string) => {
  const value = normalizeSeverity(severity);
  if (value === "success") return "Ổn định";
  if (value === "info") return "Theo dõi";
  if (value === "warning") return "Cần chú ý";
  if (value === "danger") return "Ưu tiên xử lý";
  return "Bình thường";
};

const normalizeSeverity = (severity?: string): DashboardSeverity =>
  ["success", "info", "warning", "danger", "neutral"].includes(severity || "")
    ? (severity as DashboardSeverity)
    : "neutral";

const severityColor = (severity?: string, fallbackIndex = 0) =>
  SEVERITY_COLORS[normalizeSeverity(severity)] || CHART_COLORS[fallbackIndex % CHART_COLORS.length];

const severityIconClass = (severity?: string) => {
  const map: Record<DashboardSeverity, string> = {
    neutral: "bg-gray-100 text-gray-700",
    success: "bg-emerald-50 text-emerald-700",
    info: "bg-blue-50 text-blue-700",
    warning: "bg-amber-50 text-amber-700",
    danger: "bg-red-50 text-red-700",
  };
  return map[normalizeSeverity(severity)];
};

const severityBadgeClass = (severity?: string) => {
  const map: Record<DashboardSeverity, string> = {
    neutral: "bg-gray-100 text-gray-700",
    success: "bg-emerald-50 text-emerald-700",
    info: "bg-blue-50 text-blue-700",
    warning: "bg-amber-50 text-amber-700",
    danger: "bg-red-50 text-red-700",
  };
  return map[normalizeSeverity(severity)];
};

const widgetIcon = (id: string, type?: string | null): ReactNode => {
  const key = `${id} ${type || ""}`.toLowerCase();
  if (key.includes("payroll")) return <WalletCards size={22} />;
  if (key.includes("attendance")) return <CalendarCheck size={22} />;
  if (key.includes("recruitment")) return <BriefcaseBusiness size={22} />;
  if (key.includes("approval")) return <ClipboardCheck size={22} />;
  if (key.includes("contract")) return <FileText size={22} />;
  if (key.includes("profile")) return <UserRoundCheck size={22} />;
  if (key.includes("system") || key.includes("audit")) return <ShieldCheck size={22} />;
  if (key.includes("kpi")) return <Target size={22} />;
  if (key.includes("training")) return <GraduationCap size={22} />;
  if (key.includes("risk")) return <AlertTriangle size={22} />;
  if (key.includes("personnel")) return <UsersRound size={22} />;
  return <LineChartIcon size={22} />;
};

const actionIcon = (icon?: string | null): ReactNode => {
  const key = icon || "";
  if (key.includes("wallet")) return <WalletCards size={16} />;
  if (key.includes("check")) return <CheckCircle2 size={16} />;
  if (key.includes("clock")) return <Clock3 size={16} />;
  if (key.includes("shield")) return <ShieldCheck size={16} />;
  if (key.includes("activity")) return <Activity size={16} />;
  if (key.includes("target")) return <Target size={16} />;
  if (key.includes("briefcase")) return <BriefcaseBusiness size={16} />;
  if (key.includes("file")) return <FileText size={16} />;
  if (key.includes("user")) return <UserRoundCheck size={16} />;
  return <ChevronRight size={16} />;
};

export default DashboardPage;
