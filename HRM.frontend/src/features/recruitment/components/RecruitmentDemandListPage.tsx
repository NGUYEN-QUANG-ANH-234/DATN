import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Lock, Plus, RefreshCw } from "lucide-react";
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

export const RecruitmentDemandListPage = () => {
  const { user } = useCurrentUser();
  const { triggerAlert } = useNotification();
  const [requests, setRequests] = useState<RecruitmentRequestListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("ALL");
  const [closingId, setClosingId] = useState<number | null>(null);

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

  const handleClose = async (item: RecruitmentRequestListItem) => {
    const reason = window.prompt("Lý do đóng tuyển dụng", "");
    if (reason === null) return;

    setClosingId(item.id);
    try {
      const response = await recruitmentApi.closeRequest(item.id, { reason });
      setRequests((current) =>
        current.map((request) => (request.id === item.id ? response.data : request)),
      );
      triggerAlert("success", "Đã đóng tuyển dụng", "Tin tuyển không còn nhận hồ sơ mới.");
    } catch (error) {
      console.error("Không thể đóng tuyển dụng:", error);
      triggerAlert(
        "error",
        "Không thể đóng tuyển dụng",
        "Vui lòng kiểm tra quyền hoặc trạng thái tin tuyển.",
      );
    } finally {
      setClosingId(null);
    }
  };

  return (
    <FeaturePage
      title="Quản lý nhu cầu tuyển dụng"
      description="Theo dõi nhu cầu đã tạo, tin đang tuyển, chỉ tiêu và trạng thái đóng tuyển."
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
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-[var(--hicas-border-soft)] text-sm">
              <thead className="bg-[var(--hicas-bg-soft)] text-left text-xs font-semibold uppercase tracking-wide text-[var(--hicas-text-secondary)]">
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
                  const canClose = item.status === "Approved" && !item.isClosed;

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
                          <p className="mt-2 text-xs font-medium text-amber-700">
                            Đã quá hạn nộp
                          </p>
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
                      <td className="px-4 py-4 text-right">
                        <button
                          type="button"
                          className={secondaryButtonClass}
                          disabled={!canClose || closingId === item.id}
                          onClick={() => void handleClose(item)}
                          title={canClose ? "Đóng tin tuyển dụng" : "Chỉ đóng được tin đang tuyển"}
                        >
                          <Lock size={16} />
                          {closingId === item.id ? "Đang đóng..." : "Đóng tuyển"}
                        </button>
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
