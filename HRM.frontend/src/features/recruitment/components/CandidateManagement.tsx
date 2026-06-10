import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { ClipboardCheck, Eye, RefreshCw } from "lucide-react";
import { BACKEND_URL } from "../../../core/api/config";
import {
  EmptyState,
  FeatureCard,
  FeaturePage,
  primaryButtonClass,
  secondaryButtonClass,
} from "../../../core/components/FeatureShell";
import { useNotification } from "../../../core/context/NotificationContext";
import { candidateApi } from "../api/candidateApi";
import type { CandidateHistoryDto } from "../types/candidate";

const statusLabels: Record<string, string> = {
  New: "Mới nộp",
  Interview_Pending: "Chờ phỏng vấn",
  Interview_Passed: "Đã qua phỏng vấn",
  Offer: "Đã gửi offer",
  Hired: "Đã tuyển",
  Rejected: "Từ chối",
  SLA_Expired: "Quá hạn xử lý",
};

const statusClasses: Record<string, string> = {
  New: "bg-blue-50 text-blue-700 ring-blue-100",
  Interview_Pending: "bg-amber-50 text-amber-700 ring-amber-100",
  Interview_Passed: "bg-indigo-50 text-indigo-700 ring-indigo-100",
  Offer: "bg-emerald-50 text-emerald-700 ring-emerald-100",
  Hired: "bg-green-50 text-green-700 ring-green-100",
  Rejected: "bg-red-50 text-red-700 ring-red-100",
  SLA_Expired: "bg-slate-100 text-slate-700 ring-slate-200",
};

const activeStatuses = new Set(["New", "Interview_Pending", "Interview_Passed", "Offer"]);

const getStatusLabel = (status: string) => statusLabels[status] ?? status;

const getStatusClass = (status: string) =>
  statusClasses[status] ?? "bg-gray-100 text-gray-700 ring-gray-200";

const getFileUrl = (path?: string) => {
  if (!path) return "#";
  if (path.startsWith("http")) return path;

  return `${BACKEND_URL}${path.startsWith("/") ? "" : "/"}${path}`;
};

export const CandidateManagement = () => {
  const { triggerAlert } = useNotification();
  const [candidates, setCandidates] = useState<CandidateHistoryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("ALL");

  const loadCandidates = useCallback(async () => {
    setLoading(true);
    try {
      const response = await candidateApi.getAllCandidates();
      setCandidates(response.data || []);
    } catch (error) {
      console.error("Không thể tải danh sách ứng viên:", error);
      triggerAlert("error", "Không thể tải danh sách ứng viên", "Vui lòng thử lại sau.");
    } finally {
      setLoading(false);
    }
  }, [triggerAlert]);

  useEffect(() => {
    void loadCandidates();
  }, [loadCandidates]);

  const filteredCandidates = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();

    return candidates.filter((candidate) => {
      const matchesStatus = status === "ALL" || candidate.status === status;
      const searchable = [
        candidate.fullName,
        candidate.email,
        candidate.jobTitle,
        candidate.departmentName,
        candidate.status,
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();

      return matchesStatus && (!normalizedQuery || searchable.includes(normalizedQuery));
    });
  }, [candidates, query, status]);

  const stats = useMemo(
    () => ({
      total: candidates.length,
      active: candidates.filter((candidate) => activeStatuses.has(candidate.status)).length,
      offer: candidates.filter((candidate) => candidate.status === "Offer").length,
      hired: candidates.filter((candidate) => candidate.status === "Hired").length,
    }),
    [candidates],
  );

  return (
    <FeaturePage
      title="Ứng viên"
      description="Theo dõi hồ sơ ứng tuyển, trạng thái xử lý và CV đã nộp."
      actions={
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={() => void loadCandidates()}
            className={secondaryButtonClass}
            disabled={loading}
          >
            <RefreshCw size={16} />
            Làm mới
          </button>
          <Link to="/approvals?module=CANDIDATE" className={primaryButtonClass}>
            <ClipboardCheck size={16} />
            Mở phê duyệt
          </Link>
        </div>
      }
    >
      <div className="grid gap-3 md:grid-cols-4">
        {[
          ["Tổng ứng viên", stats.total],
          ["Đang xử lý", stats.active],
          ["Đã offer", stats.offer],
          ["Đã tuyển", stats.hired],
        ].map(([label, value]) => (
          <div key={label} className="hicas-card hicas-card-padded">
            <p className="text-sm text-[var(--hicas-text-secondary)]">{label}</p>
            <p className="mt-2 text-2xl font-bold text-[var(--hicas-text-main)]">{value}</p>
          </div>
        ))}
      </div>

      <FeatureCard
        title="Danh sách ứng viên"
        description="Các quyết định duyệt hoặc từ chối được xử lý trong bàn phê duyệt chung."
      >
        <div className="mb-4 grid gap-3 md:grid-cols-[1fr_220px]">
          <input
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Tìm theo tên, email, vị trí hoặc phòng ban"
            className="hicas-input"
          />
          <select
            value={status}
            onChange={(event) => setStatus(event.target.value)}
            className="hicas-input"
          >
            <option value="ALL">Tất cả trạng thái</option>
            {Object.entries(statusLabels).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </div>

        {loading ? (
          <div className="py-10 text-center text-sm text-[var(--hicas-text-secondary)]">
            Đang tải dữ liệu...
          </div>
        ) : filteredCandidates.length === 0 ? (
          <EmptyState
            title="Chưa có ứng viên phù hợp"
            description="Đổi bộ lọc hoặc tải lại danh sách để xem thêm hồ sơ."
          />
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-[var(--hicas-border-soft)] text-sm">
              <thead className="bg-[var(--hicas-bg-soft)] text-left text-xs font-semibold uppercase tracking-wide text-[var(--hicas-text-secondary)]">
                <tr>
                  <th className="px-4 py-3">Ứng viên</th>
                  <th className="px-4 py-3">Vị trí</th>
                  <th className="px-4 py-3">Ngày nộp</th>
                  <th className="px-4 py-3">Trạng thái</th>
                  <th className="px-4 py-3 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--hicas-border-soft)] bg-white">
                {filteredCandidates.map((candidate) => (
                  <tr key={candidate.candidateId} className="align-top">
                    <td className="px-4 py-4">
                      <p className="font-semibold text-[var(--hicas-text-main)]">
                        {candidate.fullName}
                      </p>
                      <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">
                        {candidate.email}
                      </p>
                    </td>
                    <td className="px-4 py-4">
                      <p className="font-medium text-[var(--hicas-text-main)]">
                        {candidate.jobTitle || "Chưa cập nhật vị trí"}
                      </p>
                      <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">
                        {candidate.departmentName || "Chưa cập nhật phòng ban"}
                      </p>
                    </td>
                    <td className="px-4 py-4 text-[var(--hicas-text-secondary)]">
                      {candidate.appliedDate
                        ? new Date(candidate.appliedDate).toLocaleDateString("vi-VN")
                        : "-"}
                    </td>
                    <td className="px-4 py-4">
                      <span
                        className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ring-1 ${getStatusClass(candidate.status)}`}
                      >
                        {getStatusLabel(candidate.status)}
                      </span>
                    </td>
                    <td className="px-4 py-4 text-right">
                      {candidate.cvFilePath ? (
                        <a
                          href={getFileUrl(candidate.cvFilePath)}
                          target="_blank"
                          rel="noreferrer"
                          className={secondaryButtonClass}
                        >
                          <Eye size={16} />
                          Xem CV
                        </a>
                      ) : (
                        <span className="text-sm text-[var(--hicas-text-secondary)]">Chưa có CV</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </FeatureCard>
    </FeaturePage>
  );
};
