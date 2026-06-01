import { departmentData } from "../../data";
import { Card } from "../ui";

export const DepartmentDonutChart = () => {
  const totalEmployees = departmentData.reduce((sum, item) => sum + item.value, 0);
  const segments = departmentData.map((item) => ({
    ...item,
    percentValue: Number.parseFloat(item.percent.replace("%", "")),
  }));
  let offset = 0;

  return (
    <Card title="Nhân sự theo phòng ban" description="Cơ cấu hiện tại của HICAS">
      <div className="grid gap-5 lg:grid-cols-[1fr_1.05fr]">
        <div className="relative h-[260px]">
          <svg
            className="h-full w-full"
            viewBox="0 0 220 220"
            role="img"
            aria-label="Biểu đồ cơ cấu nhân sự theo phòng ban"
          >
            <circle
              cx="110"
              cy="110"
              r="78"
              fill="none"
              stroke="#F3F4F6"
              strokeWidth="32"
            />
            {segments.map((item) => {
              const currentOffset = offset;
              const visibleValue = Math.max(item.percentValue - 1.2, 0);
              offset += item.percentValue;

              return (
                <circle
                  key={item.name}
                  cx="110"
                  cy="110"
                  r="78"
                  fill="none"
                  stroke={item.color}
                  strokeDasharray={`${visibleValue} ${100 - visibleValue}`}
                  strokeDashoffset={-currentOffset}
                  strokeLinecap="round"
                  strokeWidth="32"
                  pathLength="100"
                  transform="rotate(-90 110 110)"
                >
                  <title>
                    {item.name}: {item.value} nhân sự ({item.percent})
                  </title>
                </circle>
              );
            })}
          </svg>

          <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center">
            <span className="text-3xl font-bold text-[var(--hicas-text-main)]">
              {totalEmployees}
            </span>
            <span className="text-sm font-medium text-[var(--hicas-text-secondary)]">
              Tổng nhân sự
            </span>
          </div>
        </div>

        <div className="space-y-3">
          {departmentData.map((item) => (
            <div key={item.name} className="flex items-center justify-between gap-3">
              <div className="flex min-w-0 items-center gap-3">
                <span
                  className="h-3 w-3 shrink-0 rounded-full"
                  style={{ backgroundColor: item.color }}
                />
                <span className="truncate text-sm font-medium text-[var(--hicas-text-main)]">
                  {item.name}
                </span>
              </div>
              <div className="shrink-0 text-right">
                <span className="text-sm font-semibold text-[var(--hicas-text-main)]">
                  {item.value}
                </span>
                <span className="ml-2 text-xs text-[var(--hicas-text-secondary)]">
                  {item.percent}
                </span>
              </div>
            </div>
          ))}
        </div>
      </div>
    </Card>
  );
};
