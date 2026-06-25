import { useCallback, useEffect, useMemo, useState } from "react";
import {
  EmptyState,
  FeatureCard,
  FeaturePage,
  fieldClass,
  secondaryButtonClass,
} from "../../../core/components/FeatureShell";
import { useCurrentUser } from "../../../core/auth/hooks/useCurrentUser";
import { recruitmentApi } from "../../recruitment/api/recruitmentApi";
import { candidateApi } from "../../recruitment/api/candidateApi";
import { contractApi, type ContractDto } from "../../employees/api/contractApi";
import {
  contractAddendumApi,
  type ContractAddendumDto,
} from "../../employees/api/contractAddendumApi";
import { overtimeApi, type OvertimeRequest } from "../../attendance/api/overtimeApi";
import { leaveRequestApi, type LeaveRequest } from "../../attendance/api/leaveRequestApi";
import type { CandidateHistoryDto } from "../../recruitment/types/candidate";
import type { ActiveJob } from "../../recruitment/types/recruitment";
import type { ApprovalTrackingFilters, TrackingItem } from "../types";
import { APPROVAL_MODULES } from "../types";
import {
  formatDate,
  getRole,
  moduleTone,
  normalizeText,
  statusLabel,
  unwrapData,
} from "../utils";

const defaultFilters: ApprovalTrackingFilters = {
  module: "ALL",
  status: "ALL",
  query: "",
};

export const ApprovalTrackingPage = () => {
  const { user } = useCurrentUser();
  const role = getRole(user?.role);
  const [items, setItems] = useState<TrackingItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [filters, setFilters] = useState<ApprovalTrackingFilters>(defaultFilters);

  const fetchItems = useCallback(async () => {
    setLoading(true);
    try {
      const next: TrackingItem[] = [];

      await Promise.all([
        loadContracts(next),
        loadAddendums(next),
        loadRecruitment(next),
        loadCandidates(next),
        loadOvertime(next),
        loadLeaves(next),
      ]);

      setItems(next);
    } finally {
      setLoading(false);
    }
  // Loader helpers below are scoped to this render and all read the same role snapshot.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [role]);

  useEffect(() => {
    void fetchItems();
  }, [fetchItems]);

  const filteredItems = useMemo(() => {
    const query = normalizeText(filters.query);

    return items.filter((item) => {
      if (filters.module !== "ALL" && item.module !== filters.module) {
        return false;
      }
      if (filters.status !== "ALL" && item.status !== filters.status) {
        return false;
      }
      if (query) {
        const haystack = normalizeText(
          `${item.title} ${item.owner} ${item.department} ${item.statusLabel}`,
        );
        if (!haystack.includes(query)) return false;
      }
      return true;
    });
  }, [filters, items]);

  const statuses = useMemo(
    () => Array.from(new Set(items.map((item) => item.status))).sort(),
    [items],
  );

  return (
    <FeaturePage
      title="Theo dõi trạng thái phê duyệt"
      description="Tra cứu trạng thái các yêu cầu theo phạm vi quyền của bạn."
      actions={
        <button className={secondaryButtonClass} onClick={() => fetchItems()}>
          Làm mới
        </button>
      }
      width="wide"
    >
      <FeatureCard title="Bộ lọc trạng thái">
        <div className="grid gap-4 md:grid-cols-3">
          <label>
            <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
              Phân hệ
            </span>
            <select
              className={fieldClass}
              value={filters.module}
              onChange={(event) =>
                setFilters({
                  ...filters,
                  module: event.target.value as ApprovalTrackingFilters["module"],
                })
              }
            >
              {APPROVAL_MODULES.map((module) => (
                <option key={module.value} value={module.value}>
                  {module.label}
                </option>
              ))}
            </select>
          </label>
          <label>
            <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
              Trạng thái
            </span>
            <select
              className={fieldClass}
              value={filters.status}
              onChange={(event) =>
                setFilters({ ...filters, status: event.target.value })
              }
            >
              <option value="ALL">Tất cả trạng thái</option>
              {statuses.map((status) => (
                <option key={status} value={status}>
                  {statusLabel(status)}
                </option>
              ))}
            </select>
          </label>
          <label>
            <span className="mb-1 block text-xs font-semibold uppercase text-gray-500">
              Tìm kiếm
            </span>
            <input
              className={fieldClass}
              value={filters.query}
              onChange={(event) =>
                setFilters({ ...filters, query: event.target.value })
              }
              placeholder="Tên, phòng ban, trạng thái..."
            />
          </label>
        </div>
      </FeatureCard>

      <FeatureCard
        title="Danh sách trạng thái"
        description={`${filteredItems.length} bản ghi theo phạm vi hiện tại.`}
      >
        {loading ? (
          <div className="py-10 text-center text-sm text-gray-500">
            Đang tải dữ liệu...
          </div>
        ) : filteredItems.length === 0 ? (
          <EmptyState title="Chưa có bản ghi phù hợp" />
        ) : (
          <div className="max-h-[620px] overflow-auto rounded-[var(--radius-md)] border border-[var(--hicas-border)]">
            <table className="w-full min-w-[920px] text-left text-sm">
              <thead className="sticky top-0 z-10 border-b bg-gray-50 text-xs uppercase text-gray-500 shadow-sm">
                <tr>
                  <th className="px-3 py-2">Phân hệ</th>
                  <th className="px-3 py-2">Nội dung</th>
                  <th className="px-3 py-2">Người liên quan</th>
                  <th className="px-3 py-2">Trạng thái</th>
                  <th className="px-3 py-2">Ngày</th>
                  <th className="px-3 py-2">Phạm vi</th>
                </tr>
              </thead>
              <tbody>
                {filteredItems.map((item) => (
                  <tr key={item.id} className="border-b">
                    <td className="px-3 py-3">
                      <span
                        className={`rounded-md border px-2 py-1 text-xs font-semibold ${moduleTone(item.module)}`}
                      >
                        {item.moduleLabel}
                      </span>
                    </td>
                    <td className="px-3 py-3">
                      <p className="font-semibold text-gray-900">{item.title}</p>
                      {item.department && (
                        <p className="text-xs text-gray-500">
                          {item.department}
                        </p>
                      )}
                    </td>
                    <td className="px-3 py-3">{item.owner || "-"}</td>
                    <td className="px-3 py-3">{item.statusLabel}</td>
                    <td className="px-3 py-3">{formatDate(item.date)}</td>
                    <td className="px-3 py-3">{item.scopeLabel}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </FeatureCard>
    </FeaturePage>
  );

  async function loadContracts(target: TrackingItem[]) {
    try {
      const res = ["Admin", "HR", "Director"].includes(role)
        ? await contractApi.getAllContracts()
        : await contractApi.getMyContracts();

      unwrapData<ContractDto>(res).forEach((item) =>
        target.push({
          id: `contract-${item.id}`,
          module: "CONTRACT",
          moduleLabel: "Hợp đồng",
          title: item.contractNumber || `Hợp đồng #${item.id}`,
          owner: item.employeeName,
          status: item.status,
          statusLabel: statusLabel(item.status),
          date: item.startDate,
          scopeLabel: scopeLabel(),
        }),
      );
    } catch {
      // Bỏ qua module không có quyền.
    }
  }

  async function loadAddendums(target: TrackingItem[]) {
    if (!["Admin", "HR", "Director"].includes(role)) return;

    try {
      const res = await contractAddendumApi.getAll();
      unwrapData<ContractAddendumDto>(res).forEach((item) =>
        target.push({
          id: `addendum-${item.id}`,
          module: "ADDENDUM",
          moduleLabel: "Phụ lục",
          title: item.addendumNumber,
          owner: item.employeeName || undefined,
          status: item.status,
          statusLabel: statusLabel(item.status),
          date: item.createdAt,
          scopeLabel: scopeLabel(),
        }),
      );
    } catch {
      // Bỏ qua.
    }
  }

  async function loadRecruitment(target: TrackingItem[]) {
    try {
      if (["Employee", "Intern", "Candidate"].includes(role)) return;

      const res = ["HR", "Director", "Admin"].includes(role)
        ? await recruitmentApi.getPendingRequests()
        : ["Manager"].includes(role)
          ? await recruitmentApi.getMyRequests()
          : await recruitmentApi.getActiveJobs();

      unwrapData<
        ActiveJob & {
          status?: string;
          createdAt?: string;
          department?: { deptName?: string };
          position?: { title?: string };
        }
      >(res).forEach(
        (item) =>
          target.push({
            id: `recruitment-${item.id}`,
            module: "RECRUITMENT",
            moduleLabel: "Tuyển dụng",
            title:
              item.positionName ||
              item.position?.title ||
              item.description ||
              `Yêu cầu #${item.id}`,
            department: item.departmentName || item.department?.deptName,
            status: item.status || "Approved",
            statusLabel: statusLabel(item.status || "Approved"),
            date: item.createdAt || item.deadline,
            scopeLabel: scopeLabel(),
          }),
      );
    } catch {
      // Bỏ qua.
    }
  }

  async function loadCandidates(target: TrackingItem[]) {
    if (role === "Employee" || role === "Intern") return;

    try {
      const res = await candidateApi.getAllCandidates();
      unwrapData<CandidateHistoryDto>(res).forEach((item) =>
        target.push({
          id: `candidate-${item.candidateId}`,
          module: "CANDIDATE",
          moduleLabel: "Ứng viên",
          title: item.jobTitle,
          owner: item.fullName,
          department: item.departmentName,
          status: item.status,
          statusLabel: statusLabel(item.status),
          date: item.appliedDate,
          scopeLabel: scopeLabel(),
        }),
      );
    } catch {
      // Bỏ qua.
    }
  }

  async function loadOvertime(target: TrackingItem[]) {
    try {
      const res = ["Admin", "HR"].includes(role)
        ? await overtimeApi.getApproved()
        : await overtimeApi.getMy();

      unwrapData<OvertimeRequest>(res).forEach((item) =>
        target.push({
          id: `overtime-${item.id}`,
          module: "OVERTIME",
          moduleLabel: "Làm thêm giờ",
          title: `${item.startTime} - ${item.endTime}`,
          owner: item.employeeName,
          department: item.departmentName,
          status: item.status,
          statusLabel: statusLabel(item.status),
          date: item.workDate,
          scopeLabel: scopeLabel(),
        }),
      );
    } catch {
      // Bỏ qua.
    }
  }

  async function loadLeaves(target: TrackingItem[]) {
    try {
      const responses = [];
      if (["Admin", "Manager"].includes(role)) {
        responses.push(await leaveRequestApi.getPendingDept());
      }
      if (["Admin", "HR"].includes(role)) {
        responses.push(await leaveRequestApi.getPendingHR());
      }
      if (["Admin", "Director"].includes(role)) {
        responses.push(await leaveRequestApi.getPendingDirector());
      }
      if (responses.length === 0) {
        responses.push(await leaveRequestApi.getMy());
      }

      responses.flatMap((res) => unwrapData<LeaveRequest>(res)).forEach((item) =>
        target.push({
          id: `leave-${item.id}`,
          module: "LEAVE",
          moduleLabel: "Nghỉ phép",
          title: `${item.leaveTypeName} (${item.requestedDays} ngày)`,
          owner: item.employeeName,
          department: item.departmentName,
          status: item.status,
          statusLabel: statusLabel(item.status),
          date: item.startDate,
          scopeLabel: scopeLabel(),
        }),
      );
    } catch {
      // Bỏ qua.
    }
  }

  function scopeLabel() {
    if (role === "Manager") return "Phòng ban của tôi";
    if (["Admin", "HR", "Director"].includes(role)) return "Tổng thể";
    return "Cá nhân";
  }
};
