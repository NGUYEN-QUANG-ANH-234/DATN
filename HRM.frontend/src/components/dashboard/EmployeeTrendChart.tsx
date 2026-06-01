import { employeeTrendData } from "../../data";
import { Card } from "../ui";

const chartWidth = 640;
const chartHeight = 260;
const padding = { top: 18, right: 20, bottom: 36, left: 44 };
const plotWidth = chartWidth - padding.left - padding.right;
const plotHeight = chartHeight - padding.top - padding.bottom;

const toPoint = (value: number, index: number, values: number[]) => {
  const min = Math.min(...values);
  const max = Math.max(...values);
  const range = Math.max(max - min, 1);

  return {
    x: padding.left + (index / Math.max(values.length - 1, 1)) * plotWidth,
    y: padding.top + (1 - (value - min) / range) * plotHeight,
  };
};

export const EmployeeTrendChart = () => {
  const employeeValues = employeeTrendData.map((item) => item.employees);
  const hireValues = employeeTrendData.map((item) => item.hires);
  const employeePoints = employeeTrendData.map((item, index) =>
    toPoint(item.employees, index, employeeValues),
  );
  const hirePoints = employeeTrendData.map((item, index) => toPoint(item.hires, index, hireValues));
  const baseline = chartHeight - padding.bottom;
  const employeeLine = employeePoints.map((point) => `${point.x},${point.y}`).join(" ");
  const hireLine = hirePoints.map((point) => `${point.x},${point.y}`).join(" ");
  const areaPath = `${padding.left},${baseline} ${employeeLine} ${
    padding.left + plotWidth
  },${baseline}`;
  const gridLines = [0, 1, 2, 3].map((line) => padding.top + (line / 3) * plotHeight);

  return (
    <Card
      title="Xu hướng nhân sự"
      description="Tổng nhân sự và số tuyển mới trong 6 tháng gần nhất"
      actions={
        <select className="hicas-select h-10 rounded-xl text-sm">
          <option>6 tháng gần nhất</option>
          <option>12 tháng gần nhất</option>
        </select>
      }
    >
      <div className="h-[310px]">
        <svg
          className="h-full w-full overflow-visible"
          viewBox={`0 0 ${chartWidth} ${chartHeight}`}
          preserveAspectRatio="none"
          role="img"
          aria-label="Biểu đồ xu hướng nhân sự"
        >
          <defs>
            <linearGradient id="employeeTrendFill" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#FF7A00" stopOpacity="0.28" />
              <stop offset="100%" stopColor="#FF7A00" stopOpacity="0.02" />
            </linearGradient>
          </defs>

          {gridLines.map((y) => (
            <line
              key={y}
              x1={padding.left}
              x2={chartWidth - padding.right}
              y1={y}
              y2={y}
              stroke="#EEF0F3"
              strokeWidth="1"
            />
          ))}

          <polygon points={areaPath} fill="url(#employeeTrendFill)" />
          <polyline points={employeeLine} fill="none" stroke="#FF7A00" strokeWidth="3.5" />
          <polyline
            points={hireLine}
            fill="none"
            stroke="#9CA3AF"
            strokeDasharray="8 8"
            strokeWidth="2.4"
          />

          {employeePoints.map((point, index) => (
            <circle
              key={employeeTrendData[index].month}
              cx={point.x}
              cy={point.y}
              r="4.5"
              fill="#FF7A00"
              stroke="#FFFFFF"
              strokeWidth="2"
            >
              <title>
                {employeeTrendData[index].month}: {employeeTrendData[index].employees} nhân sự,{" "}
                {employeeTrendData[index].hires} tuyển mới
              </title>
            </circle>
          ))}

          {employeeTrendData.map((item, index) => {
            const x = padding.left + (index / Math.max(employeeTrendData.length - 1, 1)) * plotWidth;

            return (
              <text
                key={item.month}
                x={x}
                y={chartHeight - 10}
                textAnchor="middle"
                className="fill-[var(--hicas-text-secondary)] text-[11px] font-medium"
              >
                {item.month}
              </text>
            );
          })}
        </svg>

        <div className="mt-3 flex flex-wrap items-center gap-4 text-sm text-[var(--hicas-text-secondary)]">
          <span className="inline-flex items-center gap-2">
            <span className="h-2.5 w-2.5 rounded-full bg-[var(--hicas-orange)]" />
            Tổng nhân sự
          </span>
          <span className="inline-flex items-center gap-2">
            <span className="h-0.5 w-5 rounded-full border-t-2 border-dashed border-[#9CA3AF]" />
            Tuyển mới
          </span>
        </div>
      </div>
    </Card>
  );
};
