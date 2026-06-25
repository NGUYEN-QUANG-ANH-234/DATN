import { useEffect, useState } from "react";
import { BarChart3, BriefcaseBusiness, Check, RotateCcw, Upload } from "lucide-react";
import {
  FeatureCard,
  FeaturePage,
  fieldClass,
  primaryButtonClass,
  secondaryButtonClass,
  textareaClass,
} from "../../../core/components/FeatureShell";
import { useCurrentUser } from "../../../core/auth/hooks/useCurrentUser";
import { normalizeRole } from "../../../core/auth/roleAccess";
import { useNotification } from "../../../core/context/NotificationContext";
import { performanceApi, type PerformanceEvaluation } from "../api/performanceApi";
import { taskApi, type TaskItem } from "../api/taskApi";

type KpiRowState = {
  employeeSelfPercent: number;
  actualValue: string;
  employeeComment: string;
};

const formatValue = (value?: number | null, unit?: string | null) =>
  value == null ? "-" : `${value}${unit ? ` ${unit}` : ""}`;

const formatPercent = (value?: number | null) =>
  value == null ? "-" : `${Number(value).toFixed(2).replace(/\.00$/, "")}%`;

const parseOptionalNumber = (value?: string) => {
  if (!value?.trim()) return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
};

const previewAchievedPercent = (
  detail: PerformanceEvaluation["details"][number],
  row?: KpiRowState,
) => {
  const actualValue = parseOptionalNumber(row?.actualValue);
  const selfPercent = Math.max(0, Math.min(100, Number(row?.employeeSelfPercent ?? 0)));

  if (detail.targetValue && detail.targetValue > 0 && actualValue != null) {
    return Math.min(999.99, Math.max(0, (actualValue / detail.targetValue) * 100));
  }

  return selfPercent;
};

const achievedPreviewNote = (
  detail: PerformanceEvaluation["details"][number],
  row?: KpiRowState,
) => {
  const actualValue = parseOptionalNumber(row?.actualValue);
  if (detail.targetValue && detail.targetValue > 0 && actualValue != null) {
    return "Tự tính từ thực tế / mục tiêu";
  }
  return "Dùng % tự đánh giá";
};

const statusLabel = (status?: string | null) => {
  const map: Record<string, string> = {
    Draft: "Nháp",
    PendingEmployeeUpdate: "Chờ cập nhật",
    ReworkRequired: "Cần cập nhật lại",
    PendingEvaluation: "Đã gửi chấm điểm",
    Evaluated: "Đã chốt",
    AutoEvaluated: "Tự động chốt",
    Approved: "Đã duyệt",
    Rejected: "Từ chối",
    Cancelled: "Đã hủy",
  };
  return status ? map[status] || status : "-";
};

const taskStatusLabel = (status?: string | null) => {
  const map: Record<string, string> = {
    Assigned: "Đã giao",
    InProgress: "Đang thực hiện",
    PendingReview: "Chờ duyệt",
    ReworkRequired: "Cần cập nhật lại",
    Completed: "Hoàn thành",
    Cancelled: "Đã hủy",
  };
  return status ? map[status] || status : "-";
};

const canUpdateKpi = (review: PerformanceEvaluation) =>
  review.status === "PendingEmployeeUpdate" || review.status === "ReworkRequired";

const canUpdateTask = (task: TaskItem) =>
  task.status === "Assigned" ||
  task.status === "InProgress" ||
  task.status === "ReworkRequired";

const kpiActionLabel = (review: PerformanceEvaluation) => {
  if (review.status === "PendingEvaluation") return "Đã gửi chấm điểm";
  if (canUpdateKpi(review)) return "Cập nhật KPI";
  return "Xem chi tiết";
};

const canReviewWorkItems = (role?: string | null) => {
  const normalized = normalizeRole(role);
  return normalized === "Admin" || normalized === "Manager";
};

export const TaskWorkspacePage = () => {
  const { triggerAlert } = useNotification();
  const { user } = useCurrentUser();
  const canReviewTasks = canReviewWorkItems(user?.role);
  const [myKpis, setMyKpis] = useState<PerformanceEvaluation[]>([]);
  const [myTasks, setMyTasks] = useState<TaskItem[]>([]);
  const [pending, setPending] = useState<TaskItem[]>([]);
  const [selectedTaskId, setSelectedTaskId] = useState<number | null>(null);
  const [selectedKpi, setSelectedKpi] = useState<PerformanceEvaluation | null>(null);
  const [kpiRows, setKpiRows] = useState<Record<number, KpiRowState>>({});
  const [progressPercent, setProgressPercent] = useState(100);
  const [note, setNote] = useState("");
  const [file, setFile] = useState<File | null>(null);

  const loadData = async () => {
    const [kpis, mine] = await Promise.allSettled([
      performanceApi.getMy(),
      taskApi.getMy(),
    ]);

    setMyKpis(kpis.status === "fulfilled" ? kpis.value.data || [] : []);
    setMyTasks(mine.status === "fulfilled" ? mine.value.data || [] : []);

    if (!canReviewTasks) {
      setPending([]);
      return;
    }

    const reviews = await taskApi
      .getPendingReview()
      .catch(() => ({ success: false, data: [] as TaskItem[] }));
    setPending(reviews.data || []);
  };

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void loadData();
    }, 0);
    return () => window.clearTimeout(timer);
  }, [canReviewTasks]);

  const submitProgress = async () => {
    if (!selectedTaskId) return;
    await taskApi.updateProgress(selectedTaskId, { progressPercent, note, evidenceFile: file });
    triggerAlert(
      "success",
      "Đã cập nhật tiến độ",
      "Công việc đã được gửi cho quản lý duyệt.",
    );
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

  const updateKpiRow = (detailId: number, patch: Partial<KpiRowState>) => {
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

    triggerAlert(
      "success",
      "Đã gửi kết quả KPI",
      "Kết quả thực tế và tự đánh giá đã được chuyển cho trưởng phòng chốt điểm.",
    );
    setSelectedKpi(null);
    setKpiRows({});
    await loadData();
  };

  const approve = async (id: number) => {
    await taskApi.approve(id);
    triggerAlert("success", "Đã duyệt công việc", "Kết quả công việc đã được chấp nhận.");
    await loadData();
  };

  const feedback = async (id: number) => {
    const content = window.prompt("Nhập nội dung yêu cầu điều chỉnh") || "";
    if (!content.trim()) return;
    await taskApi.provideFeedback(id, content);
    triggerAlert("success", "Đã gửi phản hồi", "Nhân viên sẽ cập nhật lại tiến độ.");
    await loadData();
  };

  return (
    <FeaturePage
      title="KPI và công việc"
      description="Cập nhật kết quả KPI theo kỳ và theo dõi các công việc được giao."
      width="wide"
    >
      <div className="grid gap-4 lg:grid-cols-2">
        <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4 shadow-sm">
          <div className="flex items-start gap-3">
            <span className="rounded-xl bg-[var(--hicas-orange-lighter)] p-2 text-[var(--hicas-orange-dark)]">
              <BarChart3 size={20} />
            </span>
            <div>
              <h2 className="text-lg font-bold text-[var(--hicas-text-main)]">KPI của tôi</h2>
              <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">
                Chỉ tiêu theo kỳ đánh giá. Bạn nhập kết quả thực tế và tự đánh giá, trưởng phòng chốt điểm chính thức.
              </p>
            </div>
          </div>
        </div>
        <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4 shadow-sm">
          <div className="flex items-start gap-3">
            <span className="rounded-xl bg-slate-100 p-2 text-slate-700">
              <BriefcaseBusiness size={20} />
            </span>
            <div>
              <h2 className="text-lg font-bold text-[var(--hicas-text-main)]">Công việc của tôi</h2>
              <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">
                Nhiệm vụ hằng ngày hoặc theo dự án. Bạn cập nhật tiến độ và minh chứng để quản lý duyệt.
              </p>
            </div>
          </div>
        </div>
      </div>

      <FeatureCard title="KPI của tôi" description="Chọn kỳ cần cập nhật kết quả thực tế và tự đánh giá.">
        {myKpis.length === 0 ? (
          <div className="rounded-[var(--radius-md)] border border-dashed border-[var(--hicas-border)] bg-white px-4 py-8 text-center text-sm text-[var(--hicas-text-secondary)]">
            Chưa có KPI nào được giao cho tài khoản này.
          </div>
        ) : (
          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            {myKpis.map((review) => {
              const updatable = canUpdateKpi(review);
              const completed = review.status === "Evaluated" || review.status === "Approved" || review.status === "AutoEvaluated";

              return (
                <article
                  key={review.id}
                  className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4 shadow-sm"
                >
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">Kỳ KPI</p>
                      <h3 className="mt-1 text-xl font-bold text-[var(--hicas-text-main)]">{review.period}</h3>
                    </div>
                    <span className={`rounded-md px-2 py-1 text-xs font-bold ${
                      updatable
                        ? "bg-orange-50 text-orange-700"
                        : completed
                          ? "bg-emerald-50 text-emerald-700"
                          : "bg-slate-100 text-slate-700"
                    }`}>
                      {statusLabel(review.status)}
                    </span>
                  </div>

                  <div className="mt-4 grid grid-cols-3 gap-2 text-sm">
                    <div className="rounded-lg bg-[var(--hicas-bg-soft)] px-3 py-2">
                      <p className="text-xs text-[var(--hicas-text-secondary)]">Chỉ tiêu</p>
                      <p className="font-bold text-[var(--hicas-text-main)]">{review.details.length}</p>
                    </div>
                    <div className="rounded-lg bg-[var(--hicas-bg-soft)] px-3 py-2">
                      <p className="text-xs text-[var(--hicas-text-secondary)]">Trọng số</p>
                      <p className="font-bold text-[var(--hicas-text-main)]">{review.totalWeight}%</p>
                    </div>
                    <div className="rounded-lg bg-[var(--hicas-bg-soft)] px-3 py-2">
                      <p className="text-xs text-[var(--hicas-text-secondary)]">Điểm</p>
                      <p className="font-bold text-[var(--hicas-text-main)]">{completed ? review.totalScore || "-" : "-"}</p>
                    </div>
                  </div>

                  {review.finalRating && (
                    <p className="mt-3 text-sm font-semibold text-[var(--hicas-text-secondary)]">
                      Xếp loại: <span className="text-[var(--hicas-text-main)]">{review.finalRating}</span>
                    </p>
                  )}

                  <button
                    className={`mt-4 w-full ${updatable ? primaryButtonClass : secondaryButtonClass}`}
                    disabled={review.status === "PendingEvaluation"}
                    onClick={() => openKpiUpdate(review)}
                  >
                    <Upload size={16} /> {kpiActionLabel(review)}
                  </button>
                </article>
              );
            })}
          </div>
        )}
      </FeatureCard>

      {selectedKpi && (
        <FeatureCard
          title={`Cập nhật KPI kỳ ${selectedKpi.period}`}
          description={
            canUpdateKpi(selectedKpi)
              ? "Nhập kết quả thực tế và tự đánh giá cho từng chỉ tiêu."
              : "Kỳ KPI này đã được gửi hoặc đã chốt, chỉ hiển thị để đối chiếu."
          }
        >
          <div className="mb-4 grid gap-3 rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-[var(--hicas-bg-soft)] p-4 text-sm md:grid-cols-4">
            <div>
              <p className="text-xs font-semibold text-[var(--hicas-text-secondary)]">Trạng thái</p>
              <p className="mt-1 font-bold text-[var(--hicas-text-main)]">{statusLabel(selectedKpi.status)}</p>
            </div>
            <div>
              <p className="text-xs font-semibold text-[var(--hicas-text-secondary)]">Số chỉ tiêu</p>
              <p className="mt-1 font-bold text-[var(--hicas-text-main)]">{selectedKpi.details.length}</p>
            </div>
            <div>
              <p className="text-xs font-semibold text-[var(--hicas-text-secondary)]">Tổng trọng số</p>
              <p className="mt-1 font-bold text-[var(--hicas-text-main)]">{selectedKpi.totalWeight}%</p>
            </div>
            <div>
              <p className="text-xs font-semibold text-[var(--hicas-text-secondary)]">Điểm chính thức</p>
              <p className="mt-1 font-bold text-[var(--hicas-text-main)]">{selectedKpi.totalScore || "-"}</p>
            </div>
          </div>

          <div className="grid gap-4">
            {selectedKpi.details.map((detail) => {
              const row = kpiRows[detail.id];
              const readonly = !canUpdateKpi(selectedKpi);

              return (
                <section
                  key={detail.id}
                  className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4 shadow-sm"
                >
                  <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
                    <div className="min-w-0">
                      <p className="font-mono text-xs font-bold uppercase text-[var(--hicas-orange-dark)]">
                        {detail.kpiCode}
                      </p>
                      <h3 className="mt-1 break-words text-lg font-bold text-[var(--hicas-text-main)]">
                        {detail.kpiName}
                      </h3>
                    </div>
                    <div className="flex flex-wrap gap-2 text-xs font-bold">
                      <span className="rounded-md bg-[var(--hicas-orange-lighter)] px-2 py-1 text-[var(--hicas-orange-dark)]">
                        Trọng số {detail.weightPercent}%
                      </span>
                      <span className="rounded-md bg-[var(--hicas-bg-soft)] px-2 py-1 text-[var(--hicas-text-secondary)]">
                        Mục tiêu {formatValue(detail.targetValue, detail.unit)}
                      </span>
                    </div>
                  </div>

                  <div className="mt-4 grid gap-3 lg:grid-cols-[1fr_1fr_180px]">
                    <label className="block">
                      <span className="mb-1 block text-sm font-semibold text-[var(--hicas-text-main)]">
                        Kết quả thực tế
                      </span>
                      <input
                        className={fieldClass}
                        type="number"
                        min={0}
                        disabled={readonly}
                        value={row?.actualValue ?? ""}
                        onChange={(event) => updateKpiRow(detail.id, { actualValue: event.target.value })}
                        placeholder="Nhập kết quả"
                      />
                    </label>

                    <label className="block">
                      <span className="mb-1 block text-sm font-semibold text-[var(--hicas-text-main)]">
                        Tự đánh giá (%)
                      </span>
                      <input
                        className={fieldClass}
                        type="number"
                        min={0}
                        max={100}
                        disabled={readonly}
                        value={row?.employeeSelfPercent ?? 0}
                        onChange={(event) => updateKpiRow(detail.id, { employeeSelfPercent: Number(event.target.value) })}
                      />
                    </label>

                    <div className="rounded-lg border border-[var(--hicas-border-soft)] bg-[var(--hicas-bg-soft)] px-3 py-2">
                      <p className="text-xs font-semibold text-[var(--hicas-text-secondary)]">Hệ thống tính</p>
                      <p className="mt-1 text-xl font-bold text-[var(--hicas-text-main)]">
                        {formatPercent(previewAchievedPercent(detail, row))}
                      </p>
                      <p className="mt-1 text-xs text-[var(--hicas-text-secondary)]">
                        {achievedPreviewNote(detail, row)}
                      </p>
                    </div>
                  </div>

                  <label className="mt-3 block">
                    <span className="mb-1 block text-sm font-semibold text-[var(--hicas-text-main)]">
                      Giải trình
                    </span>
                    <textarea
                      className={textareaClass}
                      disabled={readonly}
                      value={row?.employeeComment ?? ""}
                      onChange={(event) => updateKpiRow(detail.id, { employeeComment: event.target.value })}
                      placeholder="Ghi chú ngắn về kết quả đã đạt được"
                    />
                  </label>
                </section>
              );
            })}
          </div>
          <div className="mt-4 flex justify-end gap-2">
            <button className={secondaryButtonClass} onClick={() => setSelectedKpi(null)}>
              Hủy
            </button>
            {canUpdateKpi(selectedKpi) && (
              <button className={primaryButtonClass} onClick={submitKpiProgress}>
                Gửi kết quả KPI
              </button>
            )}
          </div>
        </FeatureCard>
      )}

      <FeatureCard title="Công việc của tôi" description="Theo dõi nhiệm vụ được giao và gửi tiến độ khi có cập nhật.">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[760px] text-left text-sm">
            <thead className="border-b bg-gray-50 text-xs uppercase text-gray-500">
              <tr>
                <th className="px-3 py-2">Công việc</th>
                <th className="px-3 py-2">Tiến độ</th>
                <th className="px-3 py-2">Trạng thái</th>
                <th className="px-3 py-2">Hạn nộp</th>
                <th className="px-3 py-2"></th>
              </tr>
            </thead>
            <tbody>
              {myTasks.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-3 py-8 text-center text-[var(--hicas-text-secondary)]">
                    Chưa có công việc nào được giao.
                  </td>
                </tr>
              ) : (
                myTasks.map((task) => (
                  <tr key={task.id} className="border-b">
                    <td className="px-3 py-2 font-medium">{task.title}</td>
                    <td className="px-3 py-2">{task.progressPercent}%</td>
                    <td className="px-3 py-2">{taskStatusLabel(task.status)}</td>
                    <td className="px-3 py-2">{task.deadline?.slice(0, 10) || "-"}</td>
                    <td className="px-3 py-2 text-right">
                      <button
                        className={secondaryButtonClass}
                        disabled={!canUpdateTask(task)}
                        onClick={() => setSelectedTaskId(task.id)}
                      >
                        <Upload size={16} /> {canUpdateTask(task) ? "Cập nhật" : "Đã gửi"}
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </FeatureCard>

      {selectedTaskId && (
        <FeatureCard title="Cập nhật tiến độ">
          <div className="grid gap-3 md:grid-cols-[160px_1fr_1fr_auto] md:items-end">
            <input
              className={fieldClass}
              type="number"
              min={0}
              max={100}
              value={progressPercent}
              onChange={(event) => setProgressPercent(Number(event.target.value))}
            />
            <input
              className={fieldClass}
              value={note}
              onChange={(event) => setNote(event.target.value)}
              placeholder="Ghi chú tiến độ"
            />
            <input
              className={fieldClass}
              type="file"
              onChange={(event) => setFile(event.target.files?.[0] || null)}
            />
            <button className={primaryButtonClass} onClick={submitProgress}>Gửi</button>
          </div>
        </FeatureCard>
      )}

      {canReviewTasks && (
        <FeatureCard title="Công việc chờ duyệt" description="Chỉ hiển thị với quản lý phụ trách hoặc quản trị hệ thống.">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[760px] text-left text-sm">
              <thead className="border-b bg-gray-50 text-xs uppercase text-gray-500">
                <tr>
                  <th className="px-3 py-2">Nhân viên</th>
                  <th className="px-3 py-2">Công việc</th>
                  <th className="px-3 py-2">Tiến độ</th>
                  <th className="px-3 py-2">Hạn duyệt</th>
                  <th className="px-3 py-2"></th>
                </tr>
              </thead>
              <tbody>
                {pending.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="px-3 py-8 text-center text-[var(--hicas-text-secondary)]">
                      Không có công việc đang chờ duyệt.
                    </td>
                  </tr>
                ) : (
                  pending.map((task) => (
                    <tr key={task.id} className="border-b">
                      <td className="px-3 py-2">{task.employeeName || "-"}</td>
                      <td className="px-3 py-2 font-medium">{task.title}</td>
                      <td className="px-3 py-2">{task.progressPercent}%</td>
                      <td className="px-3 py-2">{task.reviewDeadline?.slice(0, 10) || "-"}</td>
                      <td className="px-3 py-2">
                        <div className="flex justify-end gap-2">
                          <button className={secondaryButtonClass} onClick={() => feedback(task.id)}>
                            <RotateCcw size={16} /> Yêu cầu sửa
                          </button>
                          <button className={primaryButtonClass} onClick={() => approve(task.id)}>
                            <Check size={16} /> Duyệt
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </FeatureCard>
      )}
    </FeaturePage>
  );
};
