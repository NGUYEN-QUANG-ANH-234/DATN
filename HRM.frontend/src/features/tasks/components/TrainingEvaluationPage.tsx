import { useEffect, useState } from "react";
import { Check, RotateCcw } from "lucide-react";
import { FeatureCard, FeaturePage, fieldClass, primaryButtonClass, secondaryButtonClass } from "../../../core/components/FeatureShell";
import { useNotification } from "../../../core/context/NotificationContext";
import { trainingApi, type TrainingSummary } from "../api/trainingApi";

export const TrainingEvaluationPage = () => {
  const { triggerAlert } = useNotification();
  const [items, setItems] = useState<TrainingSummary[]>([]);
  const [selected, setSelected] = useState<TrainingSummary | null>(null);

  const loadData = async () => {
    const response = await trainingApi.getPending();
    setItems(response.data || []);
  };

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void loadData();
    }, 0);
    return () => window.clearTimeout(timer);
  }, []);

  const openSummary = async (id: number) => {
    const response = await trainingApi.getSummary(id);
    setSelected(response.data);
  };

  const evaluate = async (isApproved: boolean) => {
    if (!selected) return;
    await trainingApi.evaluate({
      trainingId: selected.id,
      isApproved,
      finalScore: selected.finalScore || undefined,
      managerEvaluation: selected.managerEvaluation || undefined,
      createPromotionRequest: true,
    });
    triggerAlert("success", "Đã cập nhật đào tạo", isApproved ? "Đã đề xuất chuyển trạng thái nhân sự." : "Đã yêu cầu bổ sung đào tạo.");
    setSelected(null);
    await loadData();
  };

  return (
    <FeaturePage title="Đánh giá đào tạo" description="Tổng hợp kết quả đào tạo của thực tập sinh và nhân sự thử việc trước khi đề xuất thay đổi trạng thái." width="wide">
      <FeatureCard title="Hồ sơ chờ đánh giá">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[760px] text-left text-sm">
            <thead className="border-b bg-gray-50 text-xs uppercase text-gray-500">
              <tr>
                <th className="px-3 py-2">Nhân sự</th>
                <th className="px-3 py-2">Khóa đào tạo</th>
                <th className="px-3 py-2">Trạng thái</th>
                <th className="px-3 py-2">Hạn đánh giá</th>
                <th className="px-3 py-2"></th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.id} className="border-b">
                  <td className="px-3 py-2 font-medium">{item.employeeName}</td>
                  <td className="px-3 py-2">{item.courseName || item.trainingType || "-"}</td>
                  <td className="px-3 py-2">{item.status}</td>
                  <td className="px-3 py-2">{item.evaluationDeadline?.slice(0, 10) || "-"}</td>
                  <td className="px-3 py-2 text-right">
                    <button className={secondaryButtonClass} onClick={() => openSummary(item.id)}>Đánh giá</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </FeatureCard>

      {selected && (
        <FeatureCard title={`Tổng hợp đào tạo: ${selected.employeeName}`}>
          <div className="space-y-4">
            <div className="grid gap-3 md:grid-cols-3">
              <input className={fieldClass} type="number" value={selected.finalScore || ""} onChange={(e) => setSelected({ ...selected, finalScore: Number(e.target.value) })} placeholder="Điểm tổng kết" />
              <input className={fieldClass} value={selected.managerEvaluation || ""} onChange={(e) => setSelected({ ...selected, managerEvaluation: e.target.value })} placeholder="Nhận xét quản lý" />
              <div className="text-sm text-gray-600">Số công việc: {selected.tasks.length}</div>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full min-w-[680px] text-left text-sm">
                <thead className="border-b bg-gray-50 text-xs uppercase text-gray-500">
                  <tr>
                    <th className="px-3 py-2">Công việc</th>
                    <th className="px-3 py-2">Tiến độ</th>
                    <th className="px-3 py-2">Trạng thái</th>
                    <th className="px-3 py-2">Hạn</th>
                  </tr>
                </thead>
                <tbody>
                  {selected.tasks.map((task) => (
                    <tr key={task.id} className="border-b">
                      <td className="px-3 py-2 font-medium">{task.title}</td>
                      <td className="px-3 py-2">{task.progressPercent}%</td>
                      <td className="px-3 py-2">{task.status}</td>
                      <td className="px-3 py-2">{task.deadline?.slice(0, 10) || "-"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="flex justify-end gap-2">
              <button className={secondaryButtonClass} onClick={() => evaluate(false)}><RotateCcw size={16} /> Bổ sung đào tạo</button>
              <button className={primaryButtonClass} onClick={() => evaluate(true)}><Check size={16} /> Đạt và đề xuất</button>
            </div>
          </div>
        </FeatureCard>
      )}
    </FeaturePage>
  );
};
