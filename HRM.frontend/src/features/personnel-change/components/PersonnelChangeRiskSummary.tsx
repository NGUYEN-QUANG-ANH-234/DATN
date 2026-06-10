import { Card } from "../../../components/ui";
import type { PersonnelChangeRiskSummary as RiskSummary } from "../types/personnelChange";

type Props = {
  summary?: RiskSummary | null;
};

export const PersonnelChangeRiskSummary = ({ summary }: Props) => {
  const employee = summary?.employee;
  const contract = summary?.currentContract;
  const penalty = summary?.penaltySummary;

  return (
    <Card title="Dữ liệu tham chiếu" description="Thông tin hỗ trợ đánh giá hồ sơ biến động nhân sự.">
      {!summary ? (
        <p className="text-sm text-[var(--hicas-text-secondary)]">Chọn một hồ sơ để xem dữ liệu tham chiếu.</p>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <SummaryItem label="Nhân sự" value={employee?.fullName ?? "Chưa chọn"} detail={employee?.employeeCode} />
          <SummaryItem label="Phòng ban" value={employee?.departmentName ?? "-"} detail={employee?.positionName} />
          <SummaryItem label="Hợp đồng" value={contract?.contractNumber ?? "-"} detail={contract?.status} />
          <SummaryItem
            label="Rủi ro"
            value={`${penalty?.personnelImpactRecords ?? 0} hồ sơ`}
            detail={`Điểm trừ ${penalty?.totalPenaltyPoint ?? 0}`}
          />
        </div>
      )}
    </Card>
  );
};

const SummaryItem = ({ label, value, detail }: { label: string; value: string; detail?: string | null }) => (
  <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4">
    <p className="text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">{label}</p>
    <p className="mt-2 text-base font-semibold text-[var(--hicas-text-main)]">{value}</p>
    {detail && <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">{detail}</p>}
  </div>
);
