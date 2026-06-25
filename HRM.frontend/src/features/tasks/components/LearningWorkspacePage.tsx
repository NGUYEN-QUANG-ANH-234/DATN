import { useEffect, useMemo, useState } from "react";
import { BookOpenCheck, CheckCircle2, Clock3, Upload } from "lucide-react";
import {
  FeatureCard,
  FeaturePage,
  fieldClass,
  primaryButtonClass,
  secondaryButtonClass,
  textareaClass,
} from "../../../core/components/FeatureShell";
import { useNotification } from "../../../core/context/NotificationContext";
import { taskApi, type TaskItem } from "../api/taskApi";
import { trainingApi, type TrainingSummary } from "../api/trainingApi";

type ProgressForm = {
  progressPercent: number;
  note: string;
  evidenceFile: File | null;
};

const initialForm: ProgressForm = {
  progressPercent: 100,
  note: "",
  evidenceFile: null,
};

const statusLabel = (status?: string | null) => {
  const map: Record<string, string> = {
    InProgress: "Đang học",
    Extended: "Cần bổ sung",
    PendingEvaluation: "Chờ đánh giá",
    Completed: "Đã hoàn thành",
    Evaluated: "Đã đánh giá",
    AutoCompleted: "Tự hoàn thành",
    Failed: "Không đạt",
    Overdue: "Quá hạn",
    Cancelled: "Đã hủy",
    Assigned: "Đã giao",
    ReworkRequired: "Cần cập nhật lại",
    PendingReview: "Chờ duyệt",
    AutoApproved: "Tự duyệt",
  };
  return status ? map[status] || status : "-";
};

const canUpdateTask = (task: TaskItem) =>
  task.status === "Assigned" ||
  task.status === "InProgress" ||
  task.status === "ReworkRequired";

const formatDate = (value?: string | null) => value?.slice(0, 10) || "-";

export const LearningWorkspacePage = () => {
  const { triggerAlert } = useNotification();
  const [items, setItems] = useState<TrainingSummary[]>([]);
  const [selectedTrainingId, setSelectedTrainingId] = useState<number | null>(null);
  const [selectedTask, setSelectedTask] = useState<TaskItem | null>(null);
  const [form, setForm] = useState<ProgressForm>(initialForm);
  const [loading, setLoading] = useState(false);

  const loadData = async () => {
    setLoading(true);
    try {
      const response = await trainingApi.getMyLearning();
      const next = response.data || [];
      setItems(next);
      setSelectedTrainingId((current) => current ?? next[0]?.id ?? null);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void loadData();
    }, 0);
    return () => window.clearTimeout(timer);
  }, []);

  const selectedTraining = useMemo(
    () => items.find((item) => item.id === selectedTrainingId) ?? items[0] ?? null,
    [items, selectedTrainingId],
  );

  const learningStats = useMemo(() => {
    const tasks = selectedTraining?.tasks ?? [];
    const completed = tasks.filter((task) =>
      ["Completed", "AutoApproved"].includes(task.status),
    ).length;
    const pendingReview = tasks.filter((task) => task.status === "PendingReview").length;
    const open = tasks.filter(canUpdateTask).length;
    return { total: tasks.length, completed, pendingReview, open };
  }, [selectedTraining]);

  const openTask = (task: TaskItem) => {
    setSelectedTask(task);
    setForm({
      progressPercent: task.progressPercent || 100,
      note: "",
      evidenceFile: null,
    });
  };

  const submitProgress = async () => {
    if (!selectedTask) return;
    await taskApi.updateProgress(selectedTask.id, form);
    triggerAlert(
      "success",
      "Đã gửi tiến độ học tập",
      "Nội dung cập nhật đã được chuyển cho quản lý phụ trách duyệt.",
    );
    setSelectedTask(null);
    setForm(initialForm);
    await loadData();
  };

  return (
    <FeaturePage
      title="Việc học tập của tôi"
      description="Theo dõi nội dung đào tạo được giao và cập nhật kết quả học tập."
      width="wide"
    >
      <div className="grid gap-4 lg:grid-cols-[320px_1fr]">
        <FeatureCard title="Khóa đào tạo" description="Chọn nội dung học tập cần cập nhật.">
          {loading ? (
            <div className="py-8 text-sm text-[var(--hicas-text-secondary)]">Đang tải dữ liệu...</div>
          ) : items.length === 0 ? (
            <div className="rounded-[var(--radius-md)] border border-dashed border-[var(--hicas-border)] px-4 py-8 text-sm text-[var(--hicas-text-secondary)]">
              Chưa có nội dung đào tạo nào được giao.
            </div>
          ) : (
            <div className="max-h-[520px] space-y-2 overflow-y-auto pr-1">
              {items.map((item) => {
                const active = selectedTraining?.id === item.id;
                const total = item.tasks.length;
                const done = item.tasks.filter((task) =>
                  ["Completed", "AutoApproved"].includes(task.status),
                ).length;

                return (
                  <button
                    key={item.id}
                    type="button"
                    className={`w-full rounded-[var(--radius-md)] border p-3 text-left transition ${
                      active
                        ? "border-[var(--hicas-orange)] bg-[var(--hicas-orange-lighter)]"
                        : "border-[var(--hicas-border)] bg-white hover:border-[var(--hicas-orange)]"
                    }`}
                    onClick={() => setSelectedTrainingId(item.id)}
                  >
                    <p className="text-sm font-bold text-[var(--hicas-text-main)]">
                      {item.courseName || item.trainingType || "Nội dung đào tạo"}
                    </p>
                    <p className="mt-1 text-xs text-[var(--hicas-text-secondary)]">
                      {statusLabel(item.status)} · {done}/{total} việc hoàn thành
                    </p>
                  </button>
                );
              })}
            </div>
          )}
        </FeatureCard>

        <div className="space-y-4">
          <FeatureCard
            title={selectedTraining?.courseName || selectedTraining?.trainingType || "Chi tiết học tập"}
            description="Cập nhật từng việc học tập, đính kèm minh chứng nếu có."
            actions={
              selectedTraining ? (
                <span className="rounded-full bg-[var(--hicas-bg-soft)] px-3 py-1 text-sm font-bold text-[var(--hicas-text-secondary)]">
                  {statusLabel(selectedTraining.status)}
                </span>
              ) : undefined
            }
          >
            {!selectedTraining ? (
              <div className="rounded-[var(--radius-md)] border border-dashed border-[var(--hicas-border)] px-4 py-8 text-center text-sm text-[var(--hicas-text-secondary)]">
                Chọn một khóa đào tạo để xem chi tiết.
              </div>
            ) : (
              <>
                <div className="mb-4 grid gap-3 sm:grid-cols-4">
                  <div className="rounded-[var(--radius-md)] bg-[var(--hicas-bg-soft)] p-3">
                    <BookOpenCheck size={18} className="text-[var(--hicas-orange)]" />
                    <p className="mt-2 text-xs font-semibold text-[var(--hicas-text-secondary)]">Tổng việc</p>
                    <p className="text-xl font-bold text-[var(--hicas-text-main)]">{learningStats.total}</p>
                  </div>
                  <div className="rounded-[var(--radius-md)] bg-emerald-50 p-3">
                    <CheckCircle2 size={18} className="text-emerald-600" />
                    <p className="mt-2 text-xs font-semibold text-emerald-700">Hoàn thành</p>
                    <p className="text-xl font-bold text-emerald-800">{learningStats.completed}</p>
                  </div>
                  <div className="rounded-[var(--radius-md)] bg-orange-50 p-3">
                    <Clock3 size={18} className="text-orange-600" />
                    <p className="mt-2 text-xs font-semibold text-orange-700">Chờ duyệt</p>
                    <p className="text-xl font-bold text-orange-800">{learningStats.pendingReview}</p>
                  </div>
                  <div className="rounded-[var(--radius-md)] bg-slate-50 p-3">
                    <Upload size={18} className="text-slate-600" />
                    <p className="mt-2 text-xs font-semibold text-slate-700">Có thể cập nhật</p>
                    <p className="text-xl font-bold text-slate-900">{learningStats.open}</p>
                  </div>
                </div>

                <div className="overflow-x-auto">
                  <table className="w-full min-w-[760px] text-left text-sm">
                    <thead className="border-b bg-[var(--hicas-bg-soft)] text-xs uppercase text-[var(--hicas-text-secondary)]">
                      <tr>
                        <th className="px-3 py-2">Việc học tập</th>
                        <th className="px-3 py-2">Tiến độ</th>
                        <th className="px-3 py-2">Trạng thái</th>
                        <th className="px-3 py-2">Hạn</th>
                        <th className="px-3 py-2"></th>
                      </tr>
                    </thead>
                    <tbody>
                      {selectedTraining.tasks.length === 0 ? (
                        <tr>
                          <td colSpan={5} className="px-3 py-8 text-center text-[var(--hicas-text-secondary)]">
                            Khóa đào tạo này chưa có việc học tập chi tiết.
                          </td>
                        </tr>
                      ) : (
                        selectedTraining.tasks.map((task) => (
                          <tr key={task.id} className="border-b">
                            <td className="px-3 py-3">
                              <p className="font-bold text-[var(--hicas-text-main)]">{task.title}</p>
                              {task.description && (
                                <p className="mt-1 line-clamp-2 text-xs text-[var(--hicas-text-secondary)]">
                                  {task.description}
                                </p>
                              )}
                            </td>
                            <td className="px-3 py-3 font-semibold">{task.progressPercent}%</td>
                            <td className="px-3 py-3">{statusLabel(task.status)}</td>
                            <td className="px-3 py-3">{formatDate(task.deadline)}</td>
                            <td className="px-3 py-3 text-right">
                              <button
                                className={canUpdateTask(task) ? primaryButtonClass : secondaryButtonClass}
                                disabled={!canUpdateTask(task)}
                                onClick={() => openTask(task)}
                              >
                                <Upload size={16} />
                                {canUpdateTask(task) ? "Cập nhật" : "Đã gửi"}
                              </button>
                            </td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                </div>
              </>
            )}
          </FeatureCard>

          {selectedTask && (
            <FeatureCard
              title={`Cập nhật: ${selectedTask.title}`}
              description="Nhập tiến độ, ghi chú và minh chứng để gửi quản lý phụ trách duyệt."
            >
              <div className="grid gap-3 lg:grid-cols-[180px_1fr_1fr_auto] lg:items-end">
                <label>
                  <span className="mb-1 block text-sm font-semibold text-[var(--hicas-text-main)]">Tiến độ (%)</span>
                  <input
                    className={fieldClass}
                    type="number"
                    min={0}
                    max={100}
                    value={form.progressPercent}
                    onChange={(event) =>
                      setForm((prev) => ({
                        ...prev,
                        progressPercent: Math.max(0, Math.min(100, Number(event.target.value))),
                      }))
                    }
                  />
                </label>
                <label>
                  <span className="mb-1 block text-sm font-semibold text-[var(--hicas-text-main)]">Ghi chú</span>
                  <textarea
                    className={textareaClass}
                    value={form.note}
                    onChange={(event) => setForm((prev) => ({ ...prev, note: event.target.value }))}
                    placeholder="Mô tả ngắn kết quả đã hoàn thành"
                  />
                </label>
                <label>
                  <span className="mb-1 block text-sm font-semibold text-[var(--hicas-text-main)]">Minh chứng</span>
                  <input
                    className={fieldClass}
                    type="file"
                    onChange={(event) =>
                      setForm((prev) => ({
                        ...prev,
                        evidenceFile: event.target.files?.[0] || null,
                      }))
                    }
                  />
                </label>
                <div className="flex gap-2">
                  <button className={secondaryButtonClass} onClick={() => setSelectedTask(null)}>
                    Hủy
                  </button>
                  <button className={primaryButtonClass} onClick={submitProgress}>
                    Gửi
                  </button>
                </div>
              </div>
            </FeatureCard>
          )}
        </div>
      </div>
    </FeaturePage>
  );
};
