import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import {
  EmptyState,
  FeatureCard,
  FeaturePage,
  fieldClass,
  primaryButtonClass,
  secondaryButtonClass,
} from "../../../core/components/FeatureShell";
import { useNotification } from "../../../core/context/NotificationContext";
import { myProfileApi } from "../api/myProfileApi";
import type {
  ConsolidatedHistoryItem,
  HistoryEventType,
  PaginatedHistoryResponse,
} from "../types/myProfile";

const typeOptions: { value: HistoryEventType; label: string }[] = [
  { value: "ALL", label: "Tất cả" },
  { value: "PROFILE", label: "Hồ sơ" },
  { value: "CONTRACT", label: "Hợp đồng" },
  { value: "ADDENDUM", label: "Phụ lục" },
  { value: "EMPLOYMENT", label: "Biến động nhân sự" },
];

const typeStyles: Record<string, { label: string; className: string; dot: string }> = {
  PROFILE: {
    label: "Hồ sơ",
    className: "border-blue-200 bg-blue-50 text-blue-700",
    dot: "bg-blue-500",
  },
  CONTRACT: {
    label: "Hợp đồng",
    className: "border-emerald-200 bg-emerald-50 text-emerald-700",
    dot: "bg-emerald-500",
  },
  ADDENDUM: {
    label: "Phụ lục",
    className: "border-amber-200 bg-amber-50 text-amber-700",
    dot: "bg-amber-500",
  },
  EMPLOYMENT: {
    label: "Biến động",
    className: "border-slate-200 bg-slate-50 text-slate-700",
    dot: "bg-slate-500",
  },
};

const defaultPage: PaginatedHistoryResponse = {
  items: [],
  totalCount: 0,
  page: 1,
  size: 10,
  totalPages: 0,
};

const parseHistoryType = (value: string | null): HistoryEventType => {
  const normalized = (value || "").toUpperCase();
  return typeOptions.some((item) => item.value === normalized)
    ? (normalized as HistoryEventType)
    : "ALL";
};

export const EmployeeHistoryTimeline = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const [year, setYear] = useState<number | "">("");
  const [type, setType] = useState<HistoryEventType>(() =>
    parseHistoryType(new URLSearchParams(window.location.search).get("type")),
  );
  const [page, setPage] = useState(1);
  const [history, setHistory] = useState<PaginatedHistoryResponse>(defaultPage);
  const [loading, setLoading] = useState(false);
  const { triggerAlert } = useNotification();

  const yearOptions = useMemo(() => {
    const current = new Date().getFullYear();
    return Array.from({ length: 6 }, (_, index) => current - index);
  }, []);

  useEffect(() => {
    const nextType = parseHistoryType(searchParams.get("type"));
    setType((current) => (current === nextType ? current : nextType));
    setPage(1);
  }, [searchParams]);

  useEffect(() => {
    let cancelled = false;

    const fetchHistory = async () => {
      setLoading(true);
      try {
        const res = await myProfileApi.getHistory({
          ...(year ? { year } : {}),
          type,
          page,
          size: 10,
        });
        if (!cancelled) setHistory(res.data || defaultPage);
      } catch (error) {
        if (!cancelled) {
          console.error("Không thể tải lịch sử hồ sơ & hợp đồng:", error);
          triggerAlert("error", "Không thể tải lịch sử", "Vui lòng thử lại.");
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    fetchHistory();
    return () => {
      cancelled = true;
    };
  }, [year, type, page, triggerAlert]);

  const updateQueryType = (nextType: HistoryEventType) => {
    const next = new URLSearchParams(searchParams);
    if (nextType === "ALL") next.delete("type");
    else next.set("type", nextType);
    setSearchParams(next);
  };

  const resetFilter = () => {
    setYear("");
    setPage(1);
    updateQueryType("ALL");
  };

  const handleTypeChange = (nextType: HistoryEventType) => {
    setType(nextType);
    setPage(1);
    updateQueryType(nextType);
  };

  const handleYearChange = (nextYear: string) => {
    setYear(nextYear ? Number(nextYear) : "");
    setPage(1);
  };

  return (
    <FeaturePage
      title="Lịch sử hồ sơ & hợp đồng"
      description="Theo dõi các thay đổi đã ghi nhận về hồ sơ, hợp đồng, phụ lục và biến động nhân sự."
      width="wide"
    >
      <FeatureCard>
        <div className="grid gap-4 md:grid-cols-[1fr_1fr_auto] md:items-end">
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">
              Năm
            </label>
            <select
              className={fieldClass}
              value={year}
              onChange={(event) => handleYearChange(event.target.value)}
            >
              <option value="">Tất cả năm</option>
              {yearOptions.map((item) => (
                <option key={item} value={item}>
                  {item}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">
              Loại thay đổi
            </label>
            <select
              className={fieldClass}
              value={type}
              onChange={(event) => handleTypeChange(event.target.value as HistoryEventType)}
            >
              {typeOptions.map((item) => (
                <option key={item.value} value={item.value}>
                  {item.label}
                </option>
              ))}
            </select>
          </div>

          <button type="button" className={secondaryButtonClass} onClick={resetFilter}>
            Đặt lại
          </button>
        </div>
      </FeatureCard>

      <FeatureCard
        title="Dòng thời gian"
        description={`${history.totalCount} bản ghi phù hợp`}
        actions={
          <span className="rounded-lg border border-gray-200 bg-gray-50 px-3 py-2 text-sm font-medium text-gray-600">
            Trang {history.totalPages === 0 ? 0 : history.page}/{history.totalPages}
          </span>
        }
      >
        {loading ? (
          <div className="space-y-3">
            {Array.from({ length: 4 }).map((_, index) => (
              <div key={index} className="h-20 animate-pulse rounded-lg bg-gray-100" />
            ))}
          </div>
        ) : history.items.length === 0 ? (
          <EmptyState
            title="Chưa có biến động phù hợp"
            description="Thử đổi bộ lọc năm hoặc loại thay đổi để xem thêm dữ liệu."
          />
        ) : (
          <div className="space-y-0">
            {history.items.map((item, index) => (
              <TimelineItem
                key={`${item.eventType}-${item.refId ?? index}-${item.date}`}
                item={item}
                isLast={index === history.items.length - 1}
              />
            ))}
          </div>
        )}

        <div className="mt-6 flex flex-col gap-3 border-t border-gray-100 pt-4 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-gray-500">
            Hiển thị tối đa {history.size} bản ghi mỗi trang.
          </p>
          <div className="flex gap-2">
            <button
              type="button"
              className={secondaryButtonClass}
              disabled={loading || history.page <= 1}
              onClick={() => setPage((current) => Math.max(1, current - 1))}
            >
              Trước
            </button>
            <button
              type="button"
              className={primaryButtonClass}
              disabled={loading || history.totalPages === 0 || history.page >= history.totalPages}
              onClick={() => setPage((current) => current + 1)}
            >
              Sau
            </button>
          </div>
        </div>
      </FeatureCard>
    </FeaturePage>
  );
};

const TimelineItem = ({
  item,
  isLast,
}: {
  item: ConsolidatedHistoryItem;
  isLast: boolean;
}) => {
  const style = typeStyles[item.eventType] || typeStyles.EMPLOYMENT;

  return (
    <div className="grid grid-cols-[1.5rem_1fr] gap-4">
      <div className="relative flex justify-center">
        <span className={`mt-2 h-3 w-3 rounded-full ${style.dot}`} />
        {!isLast && <span className="absolute top-6 h-full w-px bg-gray-200" />}
      </div>
      <article className="pb-6">
        <div className="rounded-lg border border-gray-200 bg-white p-4">
          <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <h3 className="font-semibold text-gray-900">{item.title}</h3>
              <p className="mt-1 text-sm leading-6 text-gray-600">{item.description}</p>
            </div>
            <div className="flex shrink-0 flex-col gap-2 sm:items-end">
              <span className={`inline-flex rounded-full border px-2.5 py-1 text-xs font-semibold ${style.className}`}>
                {style.label}
              </span>
              <span className="text-xs font-medium text-gray-500">
                {formatDate(item.date)}
              </span>
            </div>
          </div>

          {(item.oldValue || item.newValue) && (
            <div className="mt-3 grid gap-3 rounded-lg bg-gray-50 p-3 text-xs text-gray-600 sm:grid-cols-2">
              {item.oldValue && (
                <div>
                  <span className="block font-semibold text-gray-500">Trước</span>
                  <span className="break-words">{item.oldValue}</span>
                </div>
              )}
              {item.newValue && (
                <div>
                  <span className="block font-semibold text-gray-500">Sau</span>
                  <span className="break-words">{item.newValue}</span>
                </div>
              )}
            </div>
          )}
        </div>
      </article>
    </div>
  );
};

const formatDate = (value: string) => {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "Không xác định";
  return date.toLocaleDateString("vi-VN");
};
