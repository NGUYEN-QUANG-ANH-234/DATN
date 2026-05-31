import { useCurrentUser } from "../../core/auth/hooks/useCurrentUser";
import {
  Announcements,
  DepartmentDonutChart,
  EmployeeTrendChart,
  MetricCard,
  QuickActions,
  RecentActivities,
  RecruitmentPipeline,
} from "../../components/dashboard";
import { dashboardMetrics } from "../../data";

const DashboardPage = () => {
  const { user } = useCurrentUser();
  const displayName = user?.name?.trim() || "Người dùng";

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-sm font-semibold uppercase tracking-[0.18em] text-[var(--hicas-orange)]">
            HICAS HR Portal
          </p>
          <h1 className="mt-2 text-3xl font-bold tracking-tight text-[var(--hicas-text-main)]">
            Chào mừng trở lại, {displayName}
          </h1>
          <p className="mt-2 text-sm leading-6 text-[var(--hicas-text-secondary)]">
            Tổng quan vận hành nhân sự, tuyển dụng, chấm công và lương thưởng trong một
            không gian làm việc sáng, gọn và dễ theo dõi.
          </p>
        </div>
        <div className="rounded-2xl border border-[var(--hicas-border)] bg-white px-4 py-3 text-sm font-semibold text-[var(--hicas-text-secondary)] shadow-[var(--shadow-card)]">
          Kỳ hiện tại: <span className="text-[var(--hicas-text-main)]">Tháng 05/2026</span>
        </div>
      </div>

      <section className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">
        {dashboardMetrics.map((metric) => (
          <MetricCard key={metric.label} {...metric} />
        ))}
      </section>

      <section className="grid gap-6 xl:grid-cols-[minmax(0,1.55fr)_minmax(360px,0.95fr)]">
        <EmployeeTrendChart />
        <DepartmentDonutChart />
      </section>

      <section className="grid gap-6 xl:grid-cols-[minmax(280px,0.8fr)_minmax(0,1fr)_minmax(320px,0.85fr)]">
        <QuickActions />
        <RecruitmentPipeline />
        <RecentActivities />
      </section>

      <Announcements />
    </div>
  );
};

export default DashboardPage;
