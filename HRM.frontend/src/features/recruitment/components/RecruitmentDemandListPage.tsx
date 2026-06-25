import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Copy, Lock, Plus, RefreshCw, Unlock } from "lucide-react";
import {
  EmptyState,
  FeatureCard,
  FeaturePage,
  primaryButtonClass,
  secondaryButtonClass,
} from "../../../core/components/FeatureShell";
import { useCurrentUser } from "../../../core/auth/hooks/useCurrentUser";
import { useNotification } from "../../../core/context/NotificationContext";
import { canAccessPath } from "../../../routes/appRoutes";
import { recruitmentApi } from "../api/recruitmentApi";
import type { RecruitmentRequestListItem } from "../types/recruitment";

const statusLabels: Record<string, string> = {
  PendingHR: "Chờ HR duyệt",
  PendingDirector: "Chờ giám đốc duyệt",
  Approved: "Đang tuyển",
  Rejected: "Từ chối",
  Closed: "Đã đóng",
};

const statusClasses: Record<string, string> = {
  PendingHR: "bg-amber-50 text-amber-700 ring-amber-100",
  PendingDirector: "bg-indigo-50 text-indigo-700 ring-indigo-100",
  Approved: "bg-emerald-50 text-emerald-700 ring-emerald-100",
  Rejected: "bg-red-50 text-red-700 ring-red-100",
  Closed: "bg-slate-100 text-slate-700 ring-slate-200",
};

const formatDate = (value?: string) =>
  value ? new Date(value).toLocaleDateString("vi-VN") : "Không giới hạn";

const getStatusLabel = (status: string) => statusLabels[status] ?? status;

const getStatusClass = (status: string) =>
  statusClasses[status] ?? "bg-gray-100 text-gray-700 ring-gray-200";

const getErrorMessage = (error: unknown, fallback: string) => {
  if (typeof error === "object" && error !== null && "response" in error) {
    const response = (error as { response?: { data?: { message?: string; Message?: string } } })
      .response;
    return response?.data?.message || response?.data?.Message || fallback;
  }

  return fallback;
};

const getTomorrowInputValue = () => {
  const date = new Date();
  date.setDate(date.getDate() + 1);
  return date.toISOString().slice(0, 10);
};

const normalizeRole = (role?: string) => (role || "").trim().toLowerCase();

export const RecruitmentDemandListPage = () => {
  const { user } = useCurrentUser();
  const { triggerAlert } = useNotification();
  const [requests, setRequests] = useState<RecruitmentRequestListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("ALL");
  const [processingId, setProcessingId] = useState<number | null>(null);

  const loadRequests = async () => {
    setLoading(true);
    try {
      const response = await recruitmentApi.getRequests();
      setRequests(response.data || []);
    } catch (error) {
      console.error("Không thể tải nhu cầu tuyển dụng:", error);
      triggerAlert("error", "Không thể tải dữ liệu", "Vui lòng thử lại sau.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadRequests();
  }, []);

  const filteredRequests = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();

    return requests.filter((item) => {
      const matchesStatus = status === "ALL" || item.status === status;
      const searchable = [
        item.positionName,
        item.departmentName,
        item.description,
        item.status,
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();

      return matchesStatus && (!normalizedQuery || searchable.includes(normalizedQuery));
    });
  }, [query, requests, status]);

  const stats = useMemo(
    () => ({
      total: requests.length,
      open: requests.filter((item) => item.status === "Approved" && item.canApply).length,
      pending: requests.filter(
        (item) => item.status === "PendingHR" || item.status === "PendingDirector",
      ).length,
      closed: requests.filter((item) => item.status === "Closed" || item.isFull || item.isExpired)
        .length,
    }),
    [requests],
  );

  const canCreateDemand = canAccessPath("/recruitment/demands/create", user?.role, false);
  const canManagePosting = ["admin", "hr"].includes(normalizeRole(user?.role));

  const updateRequestInList = (updated: RecruitmentRequestListItem) => {
    setRequests((current) =>
      current.some((request) => request.id === updated.id)
        ? current.map((request) => (request.id === updated.id ? updated : request))
        : [updated, ...current],
    );
  };

  const handleClose = async (item: RecruitmentRequestListItem) => {
    const reason = window.prompt("Lý do đóng tin tuyển dụng", "");
    if (reason === null) return;

    setProcessingId(item.id);
    try {
      const response = await recruitmentApi.closeRequest(item.id, { reason });
      updateRequestInList(response.data);
      triggerAlert("success", "Đã đóng tuyển dụng", "Tin tuyển không còn nhận hồ sơ mới.");
    } catch (error) {
      console.error("Không thể đóng tuyển dụng:", error);
      triggerAlert(
        "error",
        "Không thể đóng tuyển dụng",
        getErrorMessage(error, "Vui lòng kiểm tra quyền hoặc trạng thái tin tuyển."),
      );
    } finally {
      setProcessingId(null);
    }
  };

  const handleReopen = async (item: RecruitmentRequestListItem) => {
    if (item.isFull) {
      triggerAlert(
        "warning",
        "Tin đã đủ chỉ tiêu",
        "Vui lòng nhân bản tin hoặc tạo nhu cầu mới cho đợt tuyển tiếp theo.",
      );
      return;
    }

    const reason = window.prompt("Lý do mở lại tin tuyển dụng", "");
    if (reason === null) return;

    const defaultDeadline = item.isExpired ? getTomorrowInputValue() : item.deadline?.slice(0, 10) || "";
    const newDeadline = window.prompt(
      "Hạn nhận hồ sơ mới (yyyy-mm-dd). Bắt buộc nếu tin đã quá hạn.",
      defaultDeadline,
    );
    if (newDeadline === null) return;

    setProcessingId(item.id);
    try {
      const response = await recruitmentApi.reopenRequest(item.id, {
        reason,
        newDeadline: newDeadline.trim() || undefined,
      });
      updateRequestInList(response.data);
      triggerAlert("success", "Đã mở lại tin tuyển dụng", "Ứng viên có thể tiếp tục nộp hồ sơ.");
    } catch (error) {
      console.error("Không thể mở lại tuyển dụng:", error);
      triggerAlert(
        "error",
        "Không thể mở lại tin",
        getErrorMessage(error, "Vui lòng kiểm tra hạn nhận hồ sơ và chỉ tiêu tuyển dụng."),
      );
    } finally {
      setProcessingId(null);
    }
  };

  const handleClone = async (item: RecruitmentRequestListItem) => {
    const reason = window.prompt("Lý do nhân bản tin tuyển dụng", "");
    if (reason === null) return;

    const quantityText = window.prompt("Số lượng tuyển cho tin mới", String(item.quantity));
    if (quantityText === null) return;

    const quantity = Number(quantityText);
    if (!Number.isFinite(quantity) || quantity <= 0) {
      triggerAlert("warning", "Số lượng không hợp lệ", "Vui lòng nhập số lượng tuyển lớn hơn 0.");
      return;
    }

    const deadline = window.prompt("Hạn nhận hồ sơ cho tin mới (yyyy-mm-dd)", getTomorrowInputValue());
    if (deadline === null) return;

    setProcessingId(item.id);
    try {
      const response = await recruitmentApi.cloneRequest(item.id, {
        reason,
        quantity,
        deadline: deadline.trim() || undefined,
      });
      updateRequestInList(response.data);
      triggerAlert(
        "success",
        "Đã nhân bản tin tuyển dụng",
        "Tin mới đã được tạo và đang chờ phê duyệt.",
      );
    } catch (error) {
      console.error("Không thể nhân bản tuyển dụng:", error);
      triggerAlert(
        "error",
        "Không thể nhân bản tin",
        getErrorMessage(error, "Vui lòng kiểm tra phòng ban, vị trí và hạn nhận hồ sơ."),
      );
    } finally {
      setProcessingId(null);
    }
  };

  return (
    <FeaturePage
      title="Quản lý nhu cầu tuyển dụng"
      description="Theo dõi chỉ tiêu, hồ sơ đang xử lý và trạng thái nhận hồ sơ của từng tin tuyển dụng."
      actions={
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={() => void loadRequests()}
            className={secondaryButtonClass}
            disabled={loading}
          >
            <RefreshCw size={16} />
            Làm mới
          </button>
          {canCreateDemand && (
            <Link to="/recruitment/demands/create" className={primaryButtonClass}>
              <Plus size={16} />
              Tạo nhu cầu
            </Link>
          )}
        </div>
      }
    >
      <div className="grid gap-3 md:grid-cols-4">
        {[
          ["Tổng nhu cầu", stats.total],
          ["Đang tuyển", stats.open],
          ["Chờ duyệt", stats.pending],
          ["Đã đóng/quá hạn", stats.closed],
        ].map(([label, value]) => (
          <div key={label} className="hicas-card hicas-card-padded">
            <p className="text-sm text-[var(--hicas-text-secondary)]">{label}</p>
            <p className="mt-2 text-2xl font-bold text-[var(--hicas-text-main)]">{value}</p>
          </div>
        ))}
      </div>

      <FeatureCard
        title="Danh sách nhu cầu"
        description="Tin đã đóng, quá hạn hoặc đủ người sẽ không còn nhận hồ sơ mới."
      >
        <div className="mb-4 grid gap-3 md:grid-cols-[1fr_220px]">
          <input
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Tìm theo vị trí, phòng ban hoặc mô tả"
            className="hicas-input"
          />
          <select
            value={status}
            onChange={(event) => setStatus(event.target.value)}
            className="hicas-input"
          >
            <option value="ALL">Tất cả trạng thái</option>
            <option value="PendingHR">Chờ HR duyệt</option>
            <option value="PendingDirector">Chờ giám đốc duyệt</option>
            <option value="Approved">Đang tuyển</option>
            <option value="Closed">Đã đóng</option>
            <option value="Rejected">Từ chối</option>
          </select>
        </div>

        {loading ? (
          <div className="py-10 text-center text-sm text-[var(--hicas-text-secondary)]">
            Đang tải dữ liệu...
          </div>
        ) : filteredRequests.length === 0 ? (
          <EmptyState
            title="Chưa có nhu cầu tuyển dụng"
            description="Tạo nhu cầu mới hoặc đổi bộ lọc để xem thêm dữ liệu."
          />
        ) : (
          <div className="max-h-[680px] overflow-auto">
            <table className="min-w-[1040px] divide-y divide-[var(--hicas-border-soft)] text-sm">
              <thead className="sticky top-0 z-10 bg-[var(--hicas-bg-soft)] text-left text-xs font-semibold uppercase tracking-wide text-[var(--hicas-text-secondary)]">
                <tr>
                  <th className="px-4 py-3">Vị trí</th>
                  <th className="px-4 py-3">Trạng thái</th>
                  <th className="px-4 py-3">Ứng viên</th>
                  <th className="px-4 py-3">Hạn nộp</th>
                  <th className="px-4 py-3 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--hicas-border-soft)] bg-white">
                {filteredRequests.map((item) => {
                  const isProcessing = processingId === item.id;
                  const canClose =
                    canManagePosting && item.status === "Approved" && !item.isClosed && !item.isFull;
                  const canReopen =
                    canManagePosting &&
                    !item.isFull &&
                    (item.status === "Closed" || (item.status === "Approved" && item.isExpired));
                  const canClone =
                    canManagePosting &&
                    (item.status === "Closed" || item.isExpired || item.isFull);

                  return (
                    <tr key={item.id} className="align-top">
                      <td className="px-4 py-4">
                        <p className="font-semibold text-[var(--hicas-text-main)]">
                          {item.positionName || "Chưa cập nhật vị trí"}
                        </p>
                        <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">
                          {item.departmentName || "Chưa cập nhật phòng ban"}
                        </p>
                        {item.description && (
                          <p className="mt-2 max-w-xl text-sm text-[var(--hicas-text-secondary)]">
                            {item.description}
                          </p>
                        )}
                      </td>
                      <td className="px-4 py-4">
                        <span
                          className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ring-1 ${getStatusClass(item.status)}`}
                        >
                          {getStatusLabel(item.status)}
                        </span>
                        {item.isExpired && item.status === "Approved" && (
                          <p className="mt-2 text-xs font-medium text-amber-700">Đã quá hạn nộp</p>
                        )}
                        {item.isFull && (
                          <p className="mt-2 text-xs font-medium text-emerald-700">
                            Đã đủ chỉ tiêu
                          </p>
                        )}
                      </td>
                      <td className="px-4 py-4">
                        <p className="font-semibold text-[var(--hicas-text-main)]">
                          {item.filledSlots}/{item.quantity} đã chốt
                        </p>
                        <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">
                          {item.activeCandidateCount} hồ sơ đang xử lý
                        </p>
                        <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">
                          Còn {item.remainingSlots} vị trí
                        </p>
                      </td>
                      <td className="px-4 py-4 text-[var(--hicas-text-secondary)]">
                        {formatDate(item.deadline)}
                      </td>
                      <td className="px-4 py-4">
                        <div className="flex flex-wrap justify-end gap-2">
                          <button
                            type="button"
                            className={secondaryButtonClass}
                            disabled={!canClose || isProcessing}
                            onClick={() => void handleClose(item)}
                            title={
                              canClose
                                ? "Đóng tin tuyển dụng"
                                : "Chỉ đóng được tin đang tuyển và chưa đủ chỉ tiêu"
                            }
                          >
                            <Lock size={16} />
                            Đóng
                          </button>
                          <button
                            type="button"
                            className={secondaryButtonClass}
                            disabled={!canReopen || isProcessing}
                            onClick={() => void handleReopen(item)}
                            title={
                              canReopen
                                ? "Mở lại tin tuyển dụng"
                                : "Chỉ mở lại tin đã đóng/quá hạn và còn chỉ tiêu"
                            }
                          >
                            <Unlock size={16} />
                            Mở lại
                          </button>
                          <button
                            type="button"
                            className={secondaryButtonClass}
                            disabled={!canClone || isProcessing}
                            onClick={() => void handleClone(item)}
                            title={
                              canClone
                                ? "Tạo nhu cầu mới từ tin này"
                                : "Nhân bản dùng cho tin đã đóng, quá hạn hoặc đủ chỉ tiêu"
                            }
                          >
                            <Copy size={16} />
                            Nhân bản
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </FeatureCard>
    </FeaturePage>
  );
};
