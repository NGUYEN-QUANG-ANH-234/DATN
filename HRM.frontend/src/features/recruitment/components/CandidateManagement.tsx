import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  CheckCircle2,
  ClipboardCheck,
  Eye,
  FileText,
  RefreshCw,
  Search,
  XCircle,
} from "lucide-react";
import { BACKEND_URL } from "../../../core/api/config";
import { useCurrentUser } from "../../../core/auth/hooks/useCurrentUser";
import {
  EmptyState,
  FeatureCard,
  FeaturePage,
  dangerButtonClass,
  primaryButtonClass,
  secondaryButtonClass,
} from "../../../core/components/FeatureShell";
import { useNotification } from "../../../core/context/NotificationContext";
import { candidateApi } from "../api/candidateApi";
import type { CandidateHistoryDto } from "../types/candidate";

const statusLabels: Record<string, string> = {
  New: "Chờ HR sàng lọc",
  Interview_Pending: "Chờ phỏng vấn chuyên môn",
  Interview_Passed: "Đã qua phỏng vấn",
  Offer: "Chờ hoàn thiện hồ sơ",
  Hired: "Đã tuyển",
  Rejected: "Không phù hợp",
  SLA_Expired: "Quá hạn xử lý",
};

const statusClasses: Record<string, string> = {
  New: "bg-amber-50 text-amber-700 ring-amber-100",
  Interview_Pending: "bg-blue-50 text-blue-700 ring-blue-100",
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

const getErrorMessage = (error: unknown) => {
  const err = error as {
    message?: string;
    response?: {
      data?: {
        message?: string;
        Message?: string;
      };
    };
  };

  return (
    err.response?.data?.message ||
    err.response?.data?.Message ||
    err.message ||
    "Không thể xử lý hồ sơ. Vui lòng thử lại."
  );
};

const canScreenByRole = (role?: string | null) => role === "Admin" || role === "HR";

export const CandidateManagement = () => {
  const { user } = useCurrentUser();
  const { triggerAlert } = useNotification();
  const [candidates, setCandidates] = useState<CandidateHistoryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("ALL");
  const [submittingKey, setSubmittingKey] = useState<string | null>(null);

  const canHrScreen = canScreenByRole(user?.role);

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
        getStatusLabel(candidate.status),
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();

      return matchesStatus && (!normalizedQuery || searchable.includes(normalizedQuery));
    });
  }, [candidates, query, status]);

  const pendingHrCandidates = useMemo(
    () =>
      candidates
        .filter((candidate) => candidate.status === "New")
        .sort((a, b) => new Date(b.appliedDate).getTime() - new Date(a.appliedDate).getTime()),
    [candidates],
  );

  const stats = useMemo(
    () => ({
      total: candidates.length,
      pendingHr: pendingHrCandidates.length,
      active: candidates.filter((candidate) => activeStatuses.has(candidate.status)).length,
      hired: candidates.filter((candidate) => candidate.status === "Hired").length,
    }),
    [candidates, pendingHrCandidates.length],
  );

  const handleHrScreen = async (candidate: CandidateHistoryDto, passed: boolean) => {
    if (!passed) {
      const confirmed = window.confirm(
        `Xác nhận đánh dấu hồ sơ của ${candidate.fullName} là không phù hợp?`,
      );
      if (!confirmed) return;
    }

    const actionKey = `${candidate.candidateId}-${passed ? "pass" : "reject"}`;
    setSubmittingKey(actionKey);

    try {
      if (passed) {
        await candidateApi.hrApprove(candidate.candidateId);
        triggerAlert(
          "success",
          "Đã qua vòng HR",
          "Hồ sơ đã được chuyển sang vòng phỏng vấn chuyên môn.",
        );
      } else {
        await candidateApi.rejectCandidate(candidate.candidateId);
        triggerAlert("success", "Đã cập nhật hồ sơ", "Ứng viên đã được đánh dấu không phù hợp.");
      }

      await loadCandidates();
    } catch (error) {
      console.error("Không thể xử lý sàng lọc ứng viên:", error);
      triggerAlert("error", "Không thể xử lý hồ sơ", getErrorMessage(error));
    } finally {
      setSubmittingKey(null);
    }
  };

  const renderCandidateActions = (candidate: CandidateHistoryDto) => {
    const isPendingHr = candidate.status === "New";
    const passKey = `${candidate.candidateId}-pass`;
    const rejectKey = `${candidate.candidateId}-reject`;

    return (
      <div className="flex flex-wrap justify-end gap-2">
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
          <span className="inline-flex items-center gap-2 rounded-md bg-gray-50 px-3 py-2 text-sm text-[var(--hicas-text-secondary)]">
            <FileText size={16} />
            Chưa có CV
          </span>
        )}

        {canHrScreen && isPendingHr && (
          <>
            <button
              type="button"
              onClick={() => void handleHrScreen(candidate, true)}
              className={primaryButtonClass}
              disabled={submittingKey === passKey}
            >
              <CheckCircle2 size={16} />
              Đạt sơ lọc
            </button>
            <button
              type="button"
              onClick={() => void handleHrScreen(candidate, false)}
              className={dangerButtonClass}
              disabled={submittingKey === rejectKey}
            >
              <XCircle size={16} />
              Không phù hợp
            </button>
          </>
        )}
      </div>
    );
  };

  return (
    <FeaturePage
      title="Ứng viên"
      description="Theo dõi hồ sơ ứng tuyển, CV đã nộp và các bước xử lý tuyển dụng."
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
            Bàn phê duyệt
          </Link>
        </div>
      }
    >
      <div className="grid gap-3 md:grid-cols-4">
        {[
          ["Tổng ứng viên", stats.total],
          ["Chờ HR sàng lọc", stats.pendingHr],
          ["Đang xử lý", stats.active],
          ["Đã tuyển", stats.hired],
        ].map(([label, value]) => (
          <div key={label} className="hicas-card hicas-card-padded">
            <p className="text-sm font-medium text-[var(--hicas-text-secondary)]">{label}</p>
            <p className="mt-2 text-2xl font-bold text-[var(--hicas-text-main)]">{value}</p>
          </div>
        ))}
      </div>

      {canHrScreen && (
        <FeatureCard
          title="HR sàng lọc"
          description="Kiểm tra hồ sơ mới nộp trước khi chuyển sang vòng chuyên môn."
        >
          {loading ? (
            <div className="py-8 text-center text-sm text-[var(--hicas-text-secondary)]">
              Đang tải dữ liệu...
            </div>
          ) : pendingHrCandidates.length === 0 ? (
            <EmptyState
              title="Không có hồ sơ chờ sàng lọc"
              description="Các hồ sơ mới phù hợp sẽ xuất hiện tại đây để HR xử lý nhanh."
            />
          ) : (
            <div className="grid gap-3">
              {pendingHrCandidates.slice(0, 5).map((candidate) => (
                <div
                  key={candidate.candidateId}
                  className="rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white p-4"
                >
                  <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
                    <div className="min-w-0">
                      <p className="text-base font-semibold text-[var(--hicas-text-main)]">
                        {candidate.fullName}
                      </p>
                      <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">
                        {[candidate.email, candidate.jobTitle, candidate.departmentName]
                          .filter(Boolean)
                          .join(" · ")}
                      </p>
                      <p className="mt-2 text-xs font-medium text-[var(--hicas-text-muted)]">
                        Nộp ngày{" "}
                        {candidate.appliedDate
                          ? new Date(candidate.appliedDate).toLocaleDateString("vi-VN")
                          : "-"}
                      </p>
                    </div>
                    {renderCandidateActions(candidate)}
                  </div>
                </div>
              ))}
            </div>
          )}
        </FeatureCard>
      )}

      <FeatureCard
        title="Danh sách ứng viên"
        description="Tra cứu hồ sơ và theo dõi trạng thái xử lý của từng ứng viên."
      >
        <div className="mb-4 grid gap-3 md:grid-cols-[1fr_220px]">
          <label className="relative block">
            <Search
              size={18}
              className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[var(--hicas-text-muted)]"
            />
            <input
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Tìm theo tên, email, vị trí hoặc phòng ban"
              className="hicas-input pl-10"
            />
          </label>
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
          <div className="max-h-[620px] overflow-auto rounded-[var(--radius-md)] border border-[var(--hicas-border)]">
            <table className="min-w-[980px] divide-y divide-[var(--hicas-border-soft)] text-sm">
              <thead className="sticky top-0 z-10 bg-[var(--hicas-bg-soft)] text-left text-xs font-semibold uppercase tracking-wide text-[var(--hicas-text-secondary)]">
                <tr>
                  <th className="px-4 py-3">Ứng viên</th>
                  <th className="px-4 py-3">Vị trí ứng tuyển</th>
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
                      {renderCandidateActions(candidate)}
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
