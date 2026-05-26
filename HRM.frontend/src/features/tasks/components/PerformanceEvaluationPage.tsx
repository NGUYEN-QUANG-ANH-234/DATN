import { useEffect, useState } from "react";
import { Check, RotateCcw } from "lucide-react";
import { FeatureCard, FeaturePage, fieldClass, primaryButtonClass, secondaryButtonClass } from "../../../core/components/FeatureShell";
import { useNotification } from "../../../core/context/NotificationContext";
import { performanceApi, type PerformanceEvaluation } from "../api/performanceApi";

export const PerformanceEvaluationPage = () => {
  const { triggerAlert } = useNotification();
  const [items, setItems] = useState<PerformanceEvaluation[]>([]);
  const [selected, setSelected] = useState<PerformanceEvaluation | null>(null);

  const loadData = async () => {
    const response = await performanceApi.getPending();
    setItems(response.data || []);
  };

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void loadData();
    }, 0);
    return () => window.clearTimeout(timer);
  }, []);

  const openDetail = async (id: number) => {
    const response = await performanceApi.getDetail(id);
    setSelected(response.data);
  };

  const updateDetail = (detailId: number, patch: Partial<PerformanceEvaluation["details"][number]>) => {
    if (!selected) return;
    setSelected({
      ...selected,
      details: selected.details.map((detail) => detail.id === detailId ? { ...detail, ...patch } : detail),
    });
  };

  const finalize = async (isApproved: boolean) => {
    if (!selected) return;
    await performanceApi.finalizeScore(selected.id, {
      isApproved,
      finalRating: selected.finalRating || undefined,
      finalComment: selected.finalComment || undefined,
      details: selected.details.map((detail) => ({
        detailId: detail.id,
        managerScore: Number(detail.managerScore || 0),
        manualPenaltyPoint: Number(detail.manualPenaltyPoint || 0),
        manualPenaltyReason: detail.manualPenaltyReason || undefined,
        managerComment: detail.managerComment || undefined,
      })),
    });
    triggerAlert("success", "Da cap nhat danh gia", isApproved ? "Ket qua KPI da duoc chot." : "Da yeu cau nhan vien dieu chinh.");
    setSelected(null);
    await loadData();
  };

  return (
    <FeaturePage title="Danh gia KPI" description="Truong phong cham diem, bo sung diem tru thu cong va he thong tu tong hop diem tru da phat sinh." width="wide">
      <FeatureCard title="Cho danh gia">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[720px] text-left text-sm">
            <thead className="border-b bg-gray-50 text-xs uppercase text-gray-500">
              <tr>
                <th className="px-3 py-2">Nhan vien</th>
                <th className="px-3 py-2">Phong ban</th>
                <th className="px-3 py-2">Ky</th>
                <th className="px-3 py-2">Diem tru he thong</th>
                <th className="px-3 py-2"></th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.id} className="border-b">
                  <td className="px-3 py-2 font-medium">{item.employeeName}</td>
                  <td className="px-3 py-2">{item.departmentName || "-"}</td>
                  <td className="px-3 py-2">{item.period}</td>
                  <td className="px-3 py-2">{item.systemPenaltyPoint}</td>
                  <td className="px-3 py-2 text-right">
                    <button className={secondaryButtonClass} onClick={() => openDetail(item.id)}>Cham diem</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </FeatureCard>

      {selected && (
        <FeatureCard title={`Cham diem: ${selected.employeeName}`}>
          <div className="space-y-3">
            {selected.details.map((detail) => (
              <div key={detail.id} className="grid gap-3 rounded-lg border border-gray-200 p-3 md:grid-cols-[1fr_120px_120px_1fr] md:items-center">
                <div>
                  <p className="font-semibold">{detail.kpiName}</p>
                  <p className="text-xs text-gray-500">{detail.kpiCode} - Trong so {detail.weightPercent}</p>
                </div>
                <input className={fieldClass} type="number" value={detail.managerScore} onChange={(e) => updateDetail(detail.id, { managerScore: Number(e.target.value) })} placeholder="Diem" />
                <input className={fieldClass} type="number" value={detail.manualPenaltyPoint} onChange={(e) => updateDetail(detail.id, { manualPenaltyPoint: Number(e.target.value) })} placeholder="Diem tru" />
                <input className={fieldClass} value={detail.manualPenaltyReason || ""} onChange={(e) => updateDetail(detail.id, { manualPenaltyReason: e.target.value })} placeholder="Ly do tru diem" />
              </div>
            ))}
            <textarea className={fieldClass} value={selected.finalComment || ""} onChange={(e) => setSelected({ ...selected, finalComment: e.target.value })} placeholder="Nhan xet tong ket" />
            <div className="flex justify-end gap-2">
              <button className={secondaryButtonClass} onClick={() => finalize(false)}><RotateCcw size={16} /> Yeu cau sua</button>
              <button className={primaryButtonClass} onClick={() => finalize(true)}><Check size={16} /> Chot diem</button>
            </div>
          </div>
        </FeatureCard>
      )}
    </FeaturePage>
  );
};
