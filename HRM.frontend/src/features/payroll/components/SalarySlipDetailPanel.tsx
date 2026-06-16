import { Card, DataTable } from "../../../components/ui";
import type { DataTableColumn } from "../../../components/ui";
import type { SalarySlipDetail, SalarySlipDetailPanelProps } from "../types/payroll";
import { formatMoney, formatNumber } from "../utils";
import { PayrollMetricCard } from "./PayrollMetricCard";

const formatMinutes = (value?: number) => `${value ?? 0} phút`;

export const SalarySlipDetailPanel = ({ slip }: SalarySlipDetailPanelProps) => {
  const projectBonusSources = slip.details.flatMap((detail) =>
    detail.componentCode === "PROJECT_BONUS" ? detail.projectBonusSources ?? [] : [],
  );
  const externalTimesheetSources = slip.details.flatMap((detail) =>
    detail.componentCode === "EXTERNAL_TIMESHEET_PAY" ? detail.externalTimesheetSources ?? [] : [],
  );

  const columns: Array<DataTableColumn<SalarySlipDetail>> = [
    { key: "name", header: "Tên khoản", render: (detail) => detail.componentName },
    { key: "amount", header: "Số tiền", render: (detail) => formatMoney(detail.amount) },
    {
      key: "taxable",
      header: "Chịu thuế",
      render: (detail) => formatMoney(detail.taxableAmount),
    },
    {
      key: "insurance",
      header: "Tính bảo hiểm",
      render: (detail) => formatMoney(detail.insuranceBaseAmount),
    },
    { key: "note", header: "Ghi chú", render: (detail) => detail.note || "" },
  ];

  return (
    <Card
      title={`Chi tiết phiếu lương - ${slip.employeeName}`}
      description={`Kỳ ${slip.period}. Dữ liệu được tổng hợp từ bảng công và chính sách lương đã áp dụng.`}
    >
      <div className="mb-5 grid gap-3 text-sm md:grid-cols-4">
        <PayrollMetricCard label="Lương hợp đồng" value={formatMoney(slip.baseSalary)} />
        <PayrollMetricCard label="Tổng thu nhập" value={formatMoney(slip.grossIncome)} />
        <PayrollMetricCard label="Thu nhập tính thuế" value={formatMoney(slip.taxableIncome)} />
        <PayrollMetricCard label="Thực nhận" value={formatMoney(slip.netSalary)} strong />
      </div>

      <div className="mb-5 grid gap-3 text-sm md:grid-cols-4">
        <PayrollMetricCard label="Công chuẩn" value={formatNumber(slip.standardWorkDays)} />
        <PayrollMetricCard label="Công thực tế" value={formatNumber(slip.actualWorkDays)} />
        <PayrollMetricCard label="Giờ tính lương" value={formatNumber(slip.payableWorkHours)} />
        <PayrollMetricCard label="Giờ làm ghi nhận" value={formatNumber(slip.actualWorkHours)} />
        <PayrollMetricCard label="Đi muộn" value={formatMinutes(slip.lateMinutes)} />
        <PayrollMetricCard label="Về sớm" value={formatMinutes(slip.earlyLeaveMinutes)} />
        <PayrollMetricCard label="Nghỉ không lương" value={`${formatNumber(slip.unpaidLeaveWorkdays)} công`} />
        <PayrollMetricCard label="Nguồn chính" value="Bảng công đã chốt" />
      </div>

      <div className="mb-5 grid gap-3 text-sm md:grid-cols-4">
        <PayrollMetricCard label="Thâm niên" value={`${formatNumber(slip.serviceMonths)} tháng`} />
        <PayrollMetricCard label="Số năm thâm niên" value={formatNumber(slip.serviceYears)} />
        <PayrollMetricCard label="Tỷ lệ thâm niên" value={`${formatNumber(slip.seniorityRate)}%`} />
        <PayrollMetricCard label="Phụ cấp thâm niên" value={formatMoney(slip.seniorityAllowance)} />
      </div>

      <DataTable
        columns={columns}
        data={slip.details}
        rowKey={(row) => row.id}
        className="border-0 shadow-none"
        emptyTitle="Phiếu lương chưa có dòng chi tiết"
      />

      {projectBonusSources.length > 0 ? (
        <section className="mt-6 rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-[var(--hicas-bg-soft)] p-4">
          <div className="mb-4 flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <h3 className="text-base font-semibold text-[var(--hicas-text-main)]">Nguồn thưởng dự án</h3>
              <p className="text-sm text-[var(--hicas-text-secondary)]">
                Các dòng thưởng đã duyệt được snapshot vào phiếu lương này.
              </p>
            </div>
            <div className="rounded-[var(--radius-md)] bg-white px-3 py-2 text-sm font-semibold text-[var(--hicas-orange)]">
              {formatMoney(projectBonusSources.reduce((sum, source) => sum + source.bonusAmount, 0))}
            </div>
          </div>

          <div className="overflow-x-auto rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white">
            <table className="min-w-full text-left text-sm">
              <thead className="border-b border-[var(--hicas-border)] text-xs uppercase text-[var(--hicas-text-secondary)]">
                <tr>
                  <th className="px-3 py-3">Batch</th>
                  <th className="px-3 py-3">Dự án</th>
                  <th className="px-3 py-3">Số tiền</th>
                  <th className="px-3 py-3">Thuế</th>
                  <th className="px-3 py-3">Bảo hiểm</th>
                  <th className="px-3 py-3">Ghi chú</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--hicas-border)]">
                {projectBonusSources.map((source) => (
                  <tr key={`${source.batchId}-${source.id}`}>
                    <td className="px-3 py-3">
                      <p className="font-semibold">#{source.batchId}</p>
                      <p className="text-xs text-[var(--hicas-text-secondary)]">{source.fileName || "Không có tên file"}</p>
                    </td>
                    <td className="px-3 py-3">
                      <p className="font-semibold">{source.projectName}</p>
                      <p className="text-xs text-[var(--hicas-text-secondary)]">{source.projectCode}</p>
                    </td>
                    <td className="px-3 py-3 font-semibold">{formatMoney(source.bonusAmount)}</td>
                    <td className="px-3 py-3">{source.taxable ? "Có" : "Không"}</td>
                    <td className="px-3 py-3">{source.insuranceContributable ? "Có" : "Không"}</td>
                    <td className="px-3 py-3 text-[var(--hicas-text-secondary)]">
                      {source.reason || source.note || ""}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      ) : null}

      {externalTimesheetSources.length > 0 ? (
        <section className="mt-6 rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-[var(--hicas-bg-soft)] p-4">
          <div className="mb-4 flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <h3 className="text-base font-semibold text-[var(--hicas-text-main)]">Nguồn giờ công cộng tác viên</h3>
              <p className="text-sm text-[var(--hicas-text-secondary)]">
                Các dòng giờ công đã duyệt được snapshot vào phiếu lương này.
              </p>
            </div>
            <div className="rounded-[var(--radius-md)] bg-white px-3 py-2 text-sm font-semibold text-[var(--hicas-orange)]">
              {formatMoney(externalTimesheetSources.reduce((sum, source) => sum + source.amount, 0))}
            </div>
          </div>

          <div className="overflow-x-auto rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white">
            <table className="min-w-full text-left text-sm">
              <thead className="border-b border-[var(--hicas-border)] text-xs uppercase text-[var(--hicas-text-secondary)]">
                <tr>
                  <th className="px-3 py-3">Batch</th>
                  <th className="px-3 py-3">Ngày công</th>
                  <th className="px-3 py-3">Dự án</th>
                  <th className="px-3 py-3">Giờ</th>
                  <th className="px-3 py-3">Đơn giá</th>
                  <th className="px-3 py-3">Thành tiền</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--hicas-border)]">
                {externalTimesheetSources.map((source) => (
                  <tr key={`${source.importId}-${source.id}`}>
                    <td className="px-3 py-3">
                      <p className="font-semibold">#{source.importId}</p>
                      <p className="text-xs text-[var(--hicas-text-secondary)]">{source.fileName || "Không có tên file"}</p>
                    </td>
                    <td className="px-3 py-3">{formatDate(source.workDate)}</td>
                    <td className="px-3 py-3">
                      <p className="font-semibold">{source.projectCode}</p>
                      <p className="text-xs text-[var(--hicas-text-secondary)]">{source.taskCode || "Không có mã công việc"}</p>
                    </td>
                    <td className="px-3 py-3">{formatNumber(source.approvedHours)}</td>
                    <td className="px-3 py-3">{formatMoney(source.hourlyRate)}</td>
                    <td className="px-3 py-3 font-semibold">{formatMoney(source.amount)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      ) : null}
    </Card>
  );
};

const formatDate = (value?: string | null) => {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toLocaleDateString("vi-VN");
};
