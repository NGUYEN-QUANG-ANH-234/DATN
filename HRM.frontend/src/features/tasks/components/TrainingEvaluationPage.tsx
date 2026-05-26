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
    triggerAlert("success", "Da cap nhat dao tao", isApproved ? "Da de xuat chuyen trang thai nhan su." : "Da yeu cau bo sung dao tao.");
    setSelected(null);
    await loadData();
  };

  return (
    <FeaturePage title="Danh gia dao tao" description="Tong hop task dao tao cua thuc tap sinh/nhan su thu viec va de xuat bien dong nhan su khi dat." width="wide">
      <FeatureCard title="Ho so cho danh gia">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[760px] text-left text-sm">
            <thead className="border-b bg-gray-50 text-xs uppercase text-gray-500">
              <tr>
                <th className="px-3 py-2">Nhan su</th>
                <th className="px-3 py-2">Khoa dao tao</th>
                <th className="px-3 py-2">Trang thai</th>
                <th className="px-3 py-2">Han danh gia</th>
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
                    <button className={secondaryButtonClass} onClick={() => openSummary(item.id)}>Danh gia</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </FeatureCard>

      {selected && (
        <FeatureCard title={`Tong hop dao tao: ${selected.employeeName}`}>
          <div className="space-y-4">
            <div className="grid gap-3 md:grid-cols-3">
              <input className={fieldClass} type="number" value={selected.finalScore || ""} onChange={(e) => setSelected({ ...selected, finalScore: Number(e.target.value) })} placeholder="Diem tong ket" />
              <input className={fieldClass} value={selected.managerEvaluation || ""} onChange={(e) => setSelected({ ...selected, managerEvaluation: e.target.value })} placeholder="Nhan xet quan ly" />
              <div className="text-sm text-gray-600">So task: {selected.tasks.length}</div>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full min-w-[680px] text-left text-sm">
                <thead className="border-b bg-gray-50 text-xs uppercase text-gray-500">
                  <tr>
                    <th className="px-3 py-2">Task</th>
                    <th className="px-3 py-2">Tien do</th>
                    <th className="px-3 py-2">Trang thai</th>
                    <th className="px-3 py-2">Han</th>
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
              <button className={secondaryButtonClass} onClick={() => evaluate(false)}><RotateCcw size={16} /> Bo sung dao tao</button>
              <button className={primaryButtonClass} onClick={() => evaluate(true)}><Check size={16} /> Dat va de xuat</button>
            </div>
          </div>
        </FeatureCard>
      )}
    </FeaturePage>
  );
};
