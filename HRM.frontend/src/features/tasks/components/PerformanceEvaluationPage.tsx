import { useEffect, useState } from "react";
import { Check, RotateCcw } from "lucide-react";
import {
  FeatureCard,
  FeaturePage,
  fieldClass,
  primaryButtonClass,
  secondaryButtonClass,
  textareaClass,
} from "../../../core/components/FeatureShell";
import { useNotification } from "../../../core/context/NotificationContext";
import { performanceApi, type PerformanceEvaluation } from "../api/performanceApi";

const formatPercent = (value?: number | null) =>
  value == null ? "-" : `${Number(value).toFixed(2).replace(/\.00$/, "")}%`;

const formatValue = (value?: number | null, unit?: string | null) =>
  value == null ? "-" : `${value}${unit ? ` ${unit}` : ""}`;

const previewFinalPoint = (detail: PerformanceEvaluation["details"][number]) => {
  const weightedPoint = (Number(detail.managerScore || 0) * detail.weightPercent) / 100;
  const penaltyPoint = Number(detail.systemPenaltyPoint || 0) + Number(detail.manualPenaltyPoint || 0);
  return Math.max(0, weightedPoint - penaltyPoint);
};

const MANAGER_SCORE_COMMENT_THRESHOLD = 15;

const referenceManagerScore = (detail: PerformanceEvaluation["details"][number]) => {
  const achievedPercent = Number(detail.achievedPercent || 0);
  if (achievedPercent > 0) return Math.min(100, achievedPercent);
  return Math.max(0, Math.min(100, Number(detail.employeeSelfPercent || 0)));
};

const prefillManagerScore = (detail: PerformanceEvaluation["details"][number]) => {
  const currentScore = Number(detail.managerScore || 0);
  return currentScore > 0 ? currentScore : referenceManagerScore(detail);
};

const requiresManagerComment = (detail: PerformanceEvaluation["details"][number]) =>
  Math.abs(Number(detail.managerScore || 0) - referenceManagerScore(detail)) >=
  MANAGER_SCORE_COMMENT_THRESHOLD;

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
    setSelected({
      ...response.data,
      details: response.data.details.map((detail) => ({
        ...detail,
        managerScore: prefillManagerScore(detail),
      })),
    });
  };

  const updateDetail = (
    detailId: number,
    patch: Partial<PerformanceEvaluation["details"][number]>,
  ) => {
    if (!selected) return;
    setSelected({
      ...selected,
      details: selected.details.map((detail) =>
        detail.id === detailId ? { ...detail, ...patch } : detail,
      ),
    });
  };

  const finalize = async (isApproved: boolean) => {
    if (!selected) return;
    if (isApproved) {
      const missingComment = selected.details.find(
        (detail) => requiresManagerComment(detail) && !detail.managerComment?.trim(),
      );

      if (missingComment) {
        triggerAlert(
          "warning",
          "Cần nhận xét chấm KPI",
          `KPI ${missingComment.kpiCode} có điểm trưởng phòng lệch từ ${MANAGER_SCORE_COMMENT_THRESHOLD}% trở lên so với điểm gợi ý.`,
        );
        return;
      }
    }

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
    triggerAlert(
      "success",
      "Đã cập nhật đánh giá",
      isApproved ? "Kết quả KPI đã được chốt." : "Đã yêu cầu nhân viên cập nhật lại.",
    );
    setSelected(null);
    await loadData();
  };

  return (
    <FeaturePage
      title="Đánh giá KPI"
      description="Trưởng phòng đối chiếu kết quả thực tế và chốt điểm KPI chính thức."
      width="wide"
    >
      <FeatureCard title="Chờ đánh giá" description="Chọn hồ sơ KPI cần đối chiếu và chốt điểm chính thức.">
        {items.length === 0 ? (
          <div className="rounded-[var(--radius-md)] border border-dashed border-[var(--hicas-border)] bg-white px-4 py-8 text-center text-sm text-[var(--hicas-text-secondary)]">
            Không có phiếu KPI đang chờ đánh giá.
          </div>
        ) : (
          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            {items.map((item) => (
              <article
                key={item.id}
                className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4 shadow-sm"
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <p className="break-words text-lg font-bold text-[var(--hicas-text-main)]">
                      {item.employeeName}
                    </p>
                    <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">
                      {item.departmentName || "Chưa có phòng ban"}
                    </p>
                  </div>
                  <span className="rounded-md bg-[var(--hicas-orange-lighter)] px-2 py-1 text-xs font-bold text-[var(--hicas-orange-dark)]">
                    {item.period}
                  </span>
                </div>

                <div className="mt-4 grid grid-cols-2 gap-2 text-sm">
                  <div className="rounded-lg bg-[var(--hicas-bg-soft)] px-3 py-2">
                    <p className="text-xs text-[var(--hicas-text-secondary)]">Chỉ tiêu</p>
                    <p className="font-bold text-[var(--hicas-text-main)]">{item.details.length}</p>
                  </div>
                  <div className="rounded-lg bg-[var(--hicas-bg-soft)] px-3 py-2">
                    <p className="text-xs text-[var(--hicas-text-secondary)]">Điểm trừ hệ thống</p>
                    <p className="font-bold text-[var(--hicas-text-main)]">{Number(item.systemPenaltyPoint || 0).toFixed(2)}</p>
                  </div>
                </div>

                <button className={`mt-4 w-full ${secondaryButtonClass}`} onClick={() => openDetail(item.id)}>
                  Chấm điểm
                </button>
              </article>
            ))}
          </div>
        )}
      </FeatureCard>

      {selected && (
        <FeatureCard
          title={`Chấm điểm: ${selected.employeeName}`}
          description="Điểm cuối của từng chỉ tiêu được tính từ trọng số, điểm trưởng phòng và các điểm trừ."
        >
          <div className="space-y-3">
            {selected.details.map((detail) => (
              <div key={detail.id} className="rounded-lg border border-gray-200 p-4">
                <div className="grid gap-3 lg:grid-cols-[1.4fr_0.8fr_0.8fr_0.8fr] lg:items-start">
                  <div>
                    <p className="font-semibold text-gray-900">{detail.kpiName}</p>
                    <p className="mt-1 text-xs text-gray-500">
                      {detail.kpiCode} · Trọng số {detail.weightPercent}%
                    </p>
                    <div className="mt-3 grid gap-2 text-xs text-gray-600 sm:grid-cols-2">
                      <span>Mục tiêu: {formatValue(detail.targetValue, detail.unit)}</span>
                      <span>Thực tế: {formatValue(detail.actualValue, detail.unit)}</span>
                      <span>% đạt hệ thống: {formatPercent(detail.achievedPercent)}</span>
                      <span>Tự đánh giá: {formatPercent(detail.employeeSelfPercent)}</span>
                    </div>
                    {detail.employeeComment && (
                      <p className="mt-2 rounded bg-gray-50 px-3 py-2 text-xs text-gray-600">
                        Nhân viên ghi chú: {detail.employeeComment}
                      </p>
                    )}
                  </div>

                  <label className="block">
                    <span className="mb-1 block text-xs font-semibold text-gray-600">
                      Điểm chính thức (%)
                    </span>
                    <input
                      className={fieldClass}
                      type="number"
                      min={0}
                      max={100}
                      value={detail.managerScore}
                      onChange={(event) =>
                        updateDetail(detail.id, {
                          managerScore: Math.max(0, Math.min(100, Number(event.target.value))),
                        })
                      }
                    />
                    <span className="mt-1 block text-xs text-gray-500">
                      Gợi ý: {formatPercent(referenceManagerScore(detail))}
                    </span>
                    {requiresManagerComment(detail) && (
                      <span className="mt-1 block text-xs font-medium text-orange-600">
                        Cần nhập nhận xét khi điểm lệch nhiều.
                      </span>
                    )}
                  </label>

                  <label className="block">
                    <span className="mb-1 block text-xs font-semibold text-gray-600">
                      Điểm trừ thủ công
                    </span>
                    <input
                      className={fieldClass}
                      type="number"
                      min={0}
                      value={detail.manualPenaltyPoint}
                      onChange={(event) =>
                        updateDetail(detail.id, {
                          manualPenaltyPoint: Math.max(0, Number(event.target.value)),
                        })
                      }
                    />
                    <span className="mt-1 block text-xs text-gray-500">
                      Hệ thống phân bổ: {Number(detail.systemPenaltyPoint || 0).toFixed(2)}
                    </span>
                  </label>

                  <div>
                    <p className="mb-1 text-xs font-semibold text-gray-600">
                      Điểm đóng góp dự kiến
                    </p>
                    <div className="rounded-lg border border-orange-100 bg-orange-50 px-3 py-2 text-sm font-semibold text-orange-700">
                      {previewFinalPoint(detail).toFixed(2)}
                    </div>
                    <p className="mt-1 text-xs text-gray-500">
                      Điểm trừ: {(Number(detail.systemPenaltyPoint || 0) + Number(detail.manualPenaltyPoint || 0)).toFixed(2)}
                    </p>
                  </div>
                </div>

                <div className="mt-3 grid gap-3 md:grid-cols-2">
                  <input
                    className={fieldClass}
                    value={detail.manualPenaltyReason || ""}
                    onChange={(event) =>
                      updateDetail(detail.id, { manualPenaltyReason: event.target.value })
                    }
                    placeholder="Lý do trừ điểm"
                  />
                  <textarea
                    className={textareaClass}
                    value={detail.managerComment || ""}
                    onChange={(event) =>
                      updateDetail(detail.id, { managerComment: event.target.value })
                    }
                    placeholder="Nhận xét của trưởng phòng"
                  />
                </div>
              </div>
            ))}
            <textarea
              className={fieldClass}
              value={selected.finalComment || ""}
              onChange={(event) => setSelected({ ...selected, finalComment: event.target.value })}
              placeholder="Nhận xét tổng kết"
            />
            <div className="flex justify-end gap-2">
              <button className={secondaryButtonClass} onClick={() => finalize(false)}>
                <RotateCcw size={16} /> Yêu cầu cập nhật lại
              </button>
              <button className={primaryButtonClass} onClick={() => finalize(true)}>
                <Check size={16} /> Chốt điểm
              </button>
            </div>
          </div>
        </FeatureCard>
      )}
    </FeaturePage>
  );
};
