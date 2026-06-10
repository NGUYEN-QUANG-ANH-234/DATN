import { GitBranch, ShieldCheck } from "lucide-react";
import { Button, Card } from "../../../components/ui";
import { FeaturePage } from "../../../core/components/FeatureShell";
import { formulaPreviewLines } from "../utils";
import { SalaryFormulaPreviewTable } from "./SalaryFormulaPreviewTable";

export const SalaryFormulaPage = () => (
  <FeaturePage
    title="Công thức lương"
    description="Thiết lập cách tính các khoản lương, phụ cấp và khấu trừ theo từng nhóm nhân sự."
    width="wide"
  >
    <div className="grid gap-4 md:grid-cols-3">
      <Card title="Phạm vi áp dụng">
        <div className="space-y-2 text-sm leading-6 text-[var(--hicas-text-secondary)]">
          <p>Lọc theo loại hợp đồng, hình thức trả lương, loại nhân sự, phòng ban, vị trí và cấp bậc.</p>
          <p>Mỗi công thức có thời gian hiệu lực và trạng thái phê duyệt riêng.</p>
        </div>
      </Card>

      <Card title="Biến đầu vào">
        <div className="space-y-2 text-sm leading-6 text-[var(--hicas-text-secondary)]">
          <p>Biến lương lấy từ bảng công, KPI, làm thêm giờ, thuế, bảo hiểm và chính sách thâm niên.</p>
          <p>HR chỉ chọn nguồn dữ liệu đã được hệ thống cho phép khi thiết lập công thức.</p>
        </div>
      </Card>

      <Card title="Lưu lịch sử">
        <div className="space-y-2 text-sm leading-6 text-[var(--hicas-text-secondary)]">
          <p>Mỗi kỳ lương được lưu lại theo dữ liệu tại thời điểm chốt.</p>
          <p>Không tính động lại phiếu lương đã khóa.</p>
        </div>
      </Card>
    </div>

    <Card
      title="Dòng công thức mẫu"
      actions={
        <Button variant="secondary" disabled>
          <GitBranch size={16} />
          Chưa cho phép chỉnh sửa
        </Button>
      }
    >
      <SalaryFormulaPreviewTable lines={formulaPreviewLines} />
    </Card>

    <Card title="Ý nghĩa khoản lương">
      <div className="flex gap-3 rounded-[var(--radius-lg)] border border-[var(--hicas-info-soft)] bg-[var(--hicas-info-soft)] p-4 text-sm leading-6 text-[var(--hicas-info)]">
        <ShieldCheck className="mt-0.5 shrink-0" size={20} />
        <p>
          Mỗi khoản lương hoặc khoản giảm được xác định rõ để tính toán, lưu lịch sử và xuất phiếu lương.
          Các lỗi hiện diện được phản ánh qua bảng công đã chốt trước khi tính lương.
        </p>
      </div>
    </Card>
  </FeaturePage>
);
