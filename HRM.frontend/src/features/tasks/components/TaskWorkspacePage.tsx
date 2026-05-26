import { useEffect, useState } from "react";
import { Check, RotateCcw, Upload } from "lucide-react";
import { FeatureCard, FeaturePage, fieldClass, primaryButtonClass, secondaryButtonClass } from "../../../core/components/FeatureShell";
import { useNotification } from "../../../core/context/NotificationContext";
import { performanceApi, type PerformanceEvaluation } from "../api/performanceApi";
import { taskApi, type TaskItem } from "../api/taskApi";

export const TaskWorkspacePage = () => {
  const { triggerAlert } = useNotification();
  const [myKpis, setMyKpis] = useState<PerformanceEvaluation[]>([]);
  const [myTasks, setMyTasks] = useState<TaskItem[]>([]);
  const [pending, setPending] = useState<TaskItem[]>([]);
  const [selectedTaskId, setSelectedTaskId] = useState<number | null>(null);
  const [selectedKpi, setSelectedKpi] = useState<PerformanceEvaluation | null>(null);
  const [kpiRows, setKpiRows] = useState<Record<number, { employeeSelfPercent: number; actualValue: string; employeeComment: string }>>({});
  const [progressPercent, setProgressPercent] = useState(100);
  const [note, setNote] = useState("");
  const [file, setFile] = useState<File | null>(null);

  const loadData = async () => {
    const [kpis, mine, reviews] = await Promise.allSettled([
      performanceApi.getMy(),
      taskApi.getMy(),
      taskApi.getPendingReview(),
    ]);
    if (kpis.status === "fulfilled") setMyKpis(kpis.value.data || []);
    if (mine.status === "fulfilled") setMyTasks(mine.value.data || []);
    if (reviews.status === "fulfilled") setPending(reviews.value.data || []);
  };

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void loadData();
    }, 0);
    return () => window.clearTimeout(timer);
  }, []);

  const submitProgress = async () => {
    if (!selectedTaskId) return;
    await taskApi.updateProgress(selectedTaskId, { progressPercent, note, evidenceFile: file });
    triggerAlert("success", "Da cap nhat tien do", "Cong viec da duoc gui cho truong phong duyet.");
    setSelectedTaskId(null);
    setNote("");
    setFile(null);
    await loadData();
  };

  const openKpiUpdate = (review: PerformanceEvaluation) => {
    setSelectedKpi(review);
    setKpiRows(Object.fromEntries(
      review.details.map((detail) => [
        detail.id,
        {
          employeeSelfPercent: detail.employeeSelfPercent || 0,
          actualValue: detail.actualValue == null ? "" : String(detail.actualValue),
          employeeComment: detail.employeeComment || "",
        },
      ]),
    ));
  };

  const updateKpiRow = (
    detailId: number,
    patch: Partial<{ employeeSelfPercent: number; actualValue: string; employeeComment: string }>,
  ) => {
    setKpiRows((prev) => ({
      ...prev,
      [detailId]: {
        employeeSelfPercent: prev[detailId]?.employeeSelfPercent ?? 0,
        actualValue: prev[detailId]?.actualValue ?? "",
        employeeComment: prev[detailId]?.employeeComment ?? "",
        ...patch,
      },
    }));
  };

  const submitKpiProgress = async () => {
    if (!selectedKpi) return;
    await performanceApi.updateProgress(selectedKpi.id, {
      details: selectedKpi.details.map((detail) => {
        const row = kpiRows[detail.id];
        const actualValue = row?.actualValue?.trim();
        return {
          detailId: detail.id,
          employeeSelfPercent: Math.max(0, Math.min(100, row?.employeeSelfPercent ?? 0)),
          actualValue: actualValue ? Number(actualValue) : null,
          employeeComment: row?.employeeComment,
        };
      }),
    });

    triggerAlert("success", "Da gui tien do KPI", "Ket qua KPI da duoc chuyen cho truong phong danh gia.");
    setSelectedKpi(null);
    setKpiRows({});
    await loadData();
  };

  const approve = async (id: number) => {
    await taskApi.approve(id);
    triggerAlert("success", "Da duyet cong viec", "Ket qua cong viec da duoc chap nhan.");
    await loadData();
  };

  const feedback = async (id: number) => {
    const content = window.prompt("Nhap noi dung yeu cau dieu chinh") || "";
    if (!content.trim()) return;
    await taskApi.provideFeedback(id, content);
    triggerAlert("success", "Da gui phan hoi", "Nhan vien se cap nhat lai tien do.");
    await loadData();
  };

  return (
    <FeaturePage title="Cong viec va tien do" description="Nhan vien cap nhat tien do, truong phong duyet hoac yeu cau dieu chinh." width="wide">
      <FeatureCard title="KPI cua toi">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[860px] text-left text-sm">
            <thead className="border-b bg-gray-50 text-xs uppercase text-gray-500">
              <tr>
                <th className="px-3 py-2">Ky KPI</th>
                <th className="px-3 py-2">So chi tieu</th>
                <th className="px-3 py-2">Tong trong so</th>
                <th className="px-3 py-2">Trang thai</th>
                <th className="px-3 py-2">Xep loai</th>
                <th className="px-3 py-2"></th>
              </tr>
            </thead>
            <tbody>
              {myKpis.map((review) => (
                <tr key={review.id} className="border-b">
                  <td className="px-3 py-2 font-medium">{review.period}</td>
                  <td className="px-3 py-2">{review.details.length}</td>
                  <td className="px-3 py-2">{review.totalWeight}%</td>
                  <td className="px-3 py-2">{review.status}</td>
                  <td className="px-3 py-2">{review.finalRating || "-"}</td>
                  <td className="px-3 py-2 text-right">
                    <button
                      className={secondaryButtonClass}
                      disabled={review.status !== "PendingEmployeeUpdate" && review.status !== "ReworkRequired"}
                      onClick={() => openKpiUpdate(review)}
                    >
                      <Upload size={16} /> {review.status === "PendingEvaluation" ? "Da gui" : "Cap nhat KPI"}
                    </button>
                  </td>
                </tr>
              ))}
              {myKpis.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-3 py-6 text-center text-gray-500">
                    Chua co KPI nao duoc giao cho tai khoan nay.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </FeatureCard>

      {selectedKpi && (
        <FeatureCard title={`Cap nhat tien do KPI ky ${selectedKpi.period}`}>
          <div className="overflow-x-auto">
            <table className="w-full min-w-[960px] text-left text-sm">
              <thead className="border-b bg-gray-50 text-xs uppercase text-gray-500">
                <tr>
                  <th className="px-3 py-2">Ma KPI</th>
                  <th className="px-3 py-2">Chi tieu</th>
                  <th className="px-3 py-2">Trong so</th>
                  <th className="px-3 py-2">Muc tieu</th>
                  <th className="px-3 py-2">Thuc te</th>
                  <th className="px-3 py-2">Tu danh gia</th>
                  <th className="px-3 py-2">Ghi chu</th>
                </tr>
              </thead>
              <tbody>
                {selectedKpi.details.map((detail) => (
                  <tr key={detail.id} className="border-b align-top">
                    <td className="px-3 py-2 font-mono">{detail.kpiCode}</td>
                    <td className="px-3 py-2 font-medium">{detail.kpiName}</td>
                    <td className="px-3 py-2">{detail.weightPercent}%</td>
                    <td className="px-3 py-2">
                      {detail.targetValue ?? "-"} {detail.unit || ""}
                    </td>
                    <td className="px-3 py-2">
                      <input
                        className={fieldClass}
                        type="number"
                        value={kpiRows[detail.id]?.actualValue ?? ""}
                        onChange={(event) => updateKpiRow(detail.id, { actualValue: event.target.value })}
                      />
                    </td>
                    <td className="px-3 py-2">
                      <input
                        className={fieldClass}
                        type="number"
                        min={0}
                        max={100}
                        value={kpiRows[detail.id]?.employeeSelfPercent ?? 0}
                        onChange={(event) => updateKpiRow(detail.id, { employeeSelfPercent: Number(event.target.value) })}
                      />
                    </td>
                    <td className="px-3 py-2">
                      <input
                        className={fieldClass}
                        value={kpiRows[detail.id]?.employeeComment ?? ""}
                        onChange={(event) => updateKpiRow(detail.id, { employeeComment: event.target.value })}
                        placeholder="Ghi chu"
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="mt-4 flex justify-end gap-2">
            <button className={secondaryButtonClass} onClick={() => setSelectedKpi(null)}>
              Huy
            </button>
            <button className={primaryButtonClass} onClick={submitKpiProgress}>
              Gui tien do KPI
            </button>
          </div>
        </FeatureCard>
      )}

      <FeatureCard title="Cong viec cua toi">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[760px] text-left text-sm">
            <thead className="border-b bg-gray-50 text-xs uppercase text-gray-500">
              <tr>
                <th className="px-3 py-2">Cong viec</th>
                <th className="px-3 py-2">Tien do</th>
                <th className="px-3 py-2">Trang thai</th>
                <th className="px-3 py-2">Han nop</th>
                <th className="px-3 py-2"></th>
              </tr>
            </thead>
            <tbody>
              {myTasks.map((task) => (
                <tr key={task.id} className="border-b">
                  <td className="px-3 py-2 font-medium">{task.title}</td>
                  <td className="px-3 py-2">{task.progressPercent}%</td>
                  <td className="px-3 py-2">{task.status}</td>
                  <td className="px-3 py-2">{task.deadline?.slice(0, 10) || "-"}</td>
                  <td className="px-3 py-2 text-right">
                    <button className={secondaryButtonClass} onClick={() => setSelectedTaskId(task.id)}>
                      <Upload size={16} /> Cap nhat
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </FeatureCard>

      {selectedTaskId && (
        <FeatureCard title="Cap nhat tien do">
          <div className="grid gap-3 md:grid-cols-[160px_1fr_1fr_auto] md:items-end">
            <input className={fieldClass} type="number" min={0} max={100} value={progressPercent} onChange={(e) => setProgressPercent(Number(e.target.value))} />
            <input className={fieldClass} value={note} onChange={(e) => setNote(e.target.value)} placeholder="Ghi chu tien do" />
            <input className={fieldClass} type="file" onChange={(e) => setFile(e.target.files?.[0] || null)} />
            <button className={primaryButtonClass} onClick={submitProgress}>Gui</button>
          </div>
        </FeatureCard>
      )}

      <FeatureCard title="Cho truong phong duyet">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[760px] text-left text-sm">
            <thead className="border-b bg-gray-50 text-xs uppercase text-gray-500">
              <tr>
                <th className="px-3 py-2">Nhan vien</th>
                <th className="px-3 py-2">Cong viec</th>
                <th className="px-3 py-2">Tien do</th>
                <th className="px-3 py-2">Han duyet</th>
                <th className="px-3 py-2"></th>
              </tr>
            </thead>
            <tbody>
              {pending.map((task) => (
                <tr key={task.id} className="border-b">
                  <td className="px-3 py-2">{task.employeeName || "-"}</td>
                  <td className="px-3 py-2 font-medium">{task.title}</td>
                  <td className="px-3 py-2">{task.progressPercent}%</td>
                  <td className="px-3 py-2">{task.reviewDeadline?.slice(0, 10) || "-"}</td>
                  <td className="px-3 py-2">
                    <div className="flex justify-end gap-2">
                      <button className={secondaryButtonClass} onClick={() => feedback(task.id)}><RotateCcw size={16} /> Sua</button>
                      <button className={primaryButtonClass} onClick={() => approve(task.id)}><Check size={16} /> Duyet</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </FeatureCard>
    </FeaturePage>
  );
};
