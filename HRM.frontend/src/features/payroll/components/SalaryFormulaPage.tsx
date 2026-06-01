import { GitBranch, ShieldCheck } from "lucide-react";
import { Button, Card } from "../../../components/ui";
import { FeaturePage } from "../../../core/components/FeatureShell";
import { formulaPreviewLines } from "../utils";
import { SalaryFormulaPreviewTable } from "./SalaryFormulaPreviewTable";

export const SalaryFormulaPage = () => (
  <FeaturePage
    title="Định nghĩa công thức lương"
    description="Màn hình chuẩn bị cho PayrollFormula và PayrollFormulaLine: HR cấu hình công thức theo component code, scope áp dụng, ngày hiệu lực và trạng thái phê duyệt."
    width="wide"
  >
    <div className="grid gap-4 md:grid-cols-3">
      <Card title="Scope công thức">
        <div className="space-y-2 text-sm leading-6 text-[var(--hicas-text-secondary)]">
          <p>Hỗ trợ lọc theo loại hợp đồng, pay basis, loại nhân sự, phòng ban, vị trí và job level.</p>
          <p>Backend đã có entity công thức và engine tính toán; API CRUD/phê duyệt công thức sẽ nối ở bước tiếp theo.</p>
        </div>
      </Card>

      <Card title="Biến đầu vào">
        <div className="space-y-2 text-sm leading-6 text-[var(--hicas-text-secondary)]">
          <p>Biến lương lấy từ cấu hình F0.1, bảng công, KPI, OT, thuế, bảo hiểm và chính sách thâm niên.</p>
          <p>Ví dụ: base_salary, actual_workdays, payable_work_hours, service_months, seniority_allowance, kpi_bonus_amount.</p>
        </div>
      </Card>

      <Card title="Snapshot">
        <div className="space-y-2 text-sm leading-6 text-[var(--hicas-text-secondary)]">
          <p>Mỗi dòng tính lương được lưu vào PayrollDetail để khóa lịch sử khi chốt lương.</p>
          <p>Không tính động lại phiếu lương đã khóa.</p>
        </div>
      </Card>
    </div>

    <Card
      title="Dòng công thức mẫu"
      actions={
        <Button variant="secondary" disabled>
          <GitBranch size={16} />
          API quản lý công thức chưa mở
        </Button>
      }
    >
      <SalaryFormulaPreviewTable lines={formulaPreviewLines} />
    </Card>

    <Card title="Ý nghĩa ComponentCode">
      <div className="flex gap-3 rounded-[var(--radius-lg)] border border-[var(--hicas-info-soft)] bg-[var(--hicas-info-soft)] p-4 text-sm leading-6 text-[var(--hicas-info)]">
        <ShieldCheck className="mt-0.5 shrink-0" size={20} />
        <p>
          ComponentCode là mã khoản lương hoặc khoản giảm hợp lệ được engine dùng để tính, lưu snapshot và xuất phiếu lương.
          Các lỗi hiện diện không đi vào đây như khoản tiền trực tiếp, mà được phản ánh qua bảng công đã chốt.
        </p>
      </div>
    </Card>
  </FeaturePage>
);
