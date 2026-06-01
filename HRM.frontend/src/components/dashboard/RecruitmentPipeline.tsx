import { recruitmentPipeline } from "../../data";
import { Badge, Card } from "../ui";
import type { BadgeVariant } from "../ui/Badge";

export const RecruitmentPipeline = () => {
  const maxCount = Math.max(...recruitmentPipeline.map((item) => item.count));

  return (
    <Card title="Pipeline tuyển dụng" description="Tình trạng ứng viên theo từng vòng">
      <div className="space-y-4">
        {recruitmentPipeline.map((item) => (
          <div key={item.stage}>
            <div className="mb-2 flex items-center justify-between gap-3">
              <span className="text-sm font-semibold text-[var(--hicas-text-main)]">
                {item.stage}
              </span>
              <div className="flex items-center gap-2">
                <span className="text-sm font-bold text-[var(--hicas-text-main)]">
                  {item.count}
                </span>
                <Badge variant={item.variant as BadgeVariant}>{item.delta}</Badge>
              </div>
            </div>
            <div className="h-2 overflow-hidden rounded-full bg-[var(--hicas-bg-soft)]">
              <div
                className="h-full rounded-full bg-[linear-gradient(135deg,#FF7A00,#FF8A1F)]"
                style={{ width: `${(item.count / maxCount) * 100}%` }}
              />
            </div>
          </div>
        ))}
      </div>
    </Card>
  );
};
