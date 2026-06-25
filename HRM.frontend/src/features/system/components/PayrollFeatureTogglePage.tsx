import { PageHeader } from "../../../components/layout";
import { PayrollFeatureTogglePanel } from "./PayrollFeatureTogglePanel";

export const PayrollFeatureTogglePage = () => (
  <div className="space-y-6">
    <PageHeader
      title="Nguồn tính lương"
      description="Bật hoặc tắt các nguồn dữ liệu được đưa vào kỳ lương."
      breadcrumb={[
        { label: "Cấu hình hệ thống" },
        { label: "Nguồn tính lương" },
      ]}
    />

    <PayrollFeatureTogglePanel />
  </div>
);
