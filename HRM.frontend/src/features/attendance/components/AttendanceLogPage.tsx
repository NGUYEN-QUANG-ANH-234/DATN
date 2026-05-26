import { useCallback, useEffect, useMemo, useState } from "react";
import {
  FeatureCard,
  FeaturePage,
  primaryButtonClass,
} from "../../../core/components/FeatureShell";
import { useCurrentUser } from "../../../core/auth/hooks/useCurrentUser";
import { useNotification } from "../../../core/context/NotificationContext";
import {
  attendanceApi,
  type AttendanceLogResult,
  type AttendanceNetworkInfo,
  type AttendanceTodayStatus,
} from "../api/attendanceApi";

type PositionState = {
  latitude: number;
  longitude: number;
  accuracy?: number;
};

export const AttendanceLogPage = () => {
  const [loading, setLoading] = useState(false);
  const [statusLoading, setStatusLoading] = useState(false);
  const [lastPosition, setLastPosition] = useState<PositionState | null>(null);
  const [lastResult, setLastResult] = useState<AttendanceLogResult | null>(null);
  const [todayStatus, setTodayStatus] = useState<AttendanceTodayStatus | null>(null);
  const [networkInfo, setNetworkInfo] = useState<AttendanceNetworkInfo | null>(null);
  const { triggerAlert } = useNotification();
  const { user } = useCurrentUser();
  const isAdmin = user?.role?.toLowerCase() === "admin";

  const currentDate = useMemo(() => {
    return new Intl.DateTimeFormat("vi-VN", {
      weekday: "long",
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
    }).format(new Date());
  }, []);

  const fetchTodayStatus = useCallback(async () => {
    if (isAdmin) {
      setTodayStatus(null);
      setStatusLoading(false);
      return;
    }

    setStatusLoading(true);
    try {
      const res = await attendanceApi.getTodayStatus();
      setTodayStatus(res.data);
    } catch (error) {
      console.error("Không thể lấy trạng thái chấm công hôm nay:", error);
    } finally {
      setStatusLoading(false);
    }
  }, [isAdmin]);

  useEffect(() => {
    void fetchTodayStatus();
  }, [fetchTodayStatus]);

  useEffect(() => {
    if (!isAdmin) return;

    let cancelled = false;

    const fetchNetworkInfo = async () => {
      try {
        const res = await attendanceApi.getMyNetwork();
        if (!cancelled) setNetworkInfo(res.data);
      } catch (error) {
        console.error("Không thể lấy thông tin mạng:", error);
      }
    };

    void fetchNetworkInfo();
    return () => {
      cancelled = true;
    };
  }, [isAdmin]);

  const recordAttendance = () => {
    if (isAdmin) {
      triggerAlert(
        "warning",
        "Tài khoản quản trị",
        "Admin chỉ quản trị cấu hình, OT và bảng công; check-in/check-out cá nhân cần dùng tài khoản nhân viên có hồ sơ nhân sự.",
      );
      return;
    }

    if (todayStatus?.nextAction === "DONE") {
      triggerAlert("warning", "Đã hoàn tất", "Bạn đã check-in và check-out trong ngày hôm nay.");
      return;
    }

    if (!navigator.geolocation) {
      triggerAlert("error", "Không hỗ trợ GPS", "Trình duyệt hiện tại không hỗ trợ Geolocation API.");
      return;
    }

    setLoading(true);
    navigator.geolocation.getCurrentPosition(
      async (position) => {
        const gps = {
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
          accuracy: position.coords.accuracy,
        };

        setLastPosition(gps);

        try {
          const res = await attendanceApi.log({
            latitude: gps.latitude,
            longitude: gps.longitude,
          });

          setLastResult(res.data);
          await fetchTodayStatus();
          triggerAlert("success", "Chấm công thành công", res.data.message || res.message);
        } catch (error) {
          const message = error instanceof Error ? error.message : "Không thể ghi nhận chấm công.";
          triggerAlert("error", "Chấm công thất bại", message);
        } finally {
          setLoading(false);
        }
      },
      (error) => {
        setLoading(false);
        const message =
          error.code === error.PERMISSION_DENIED
            ? "Bạn cần cho phép trình duyệt truy cập vị trí để chấm công."
            : "Không thể lấy vị trí hiện tại. Vui lòng thử lại.";
        triggerAlert("warning", "Không lấy được GPS", message);
      },
      {
        enableHighAccuracy: true,
        timeout: 10000,
        maximumAge: 30000,
      },
    );
  };

  const handleAttendance = () => {
    if (isAdmin) {
      triggerAlert(
        "warning",
        "Tài khoản quản trị",
        "Admin không thực hiện check-in/check-out cá nhân. Hãy dùng các mục quản trị chấm công, OT hoặc tổng hợp bảng công.",
      );
      return;
    }

    if (todayStatus?.nextAction === "DONE") {
      triggerAlert("warning", "Đã hoàn tất", "Bạn đã check-in và check-out trong ngày hôm nay.");
      return;
    }

    const actionLabel = getActionLabel(todayStatus?.nextAction).toLowerCase();
    triggerAlert(
      "confirm",
      `Xác nhận ${actionLabel}`,
      `Bạn có chắc chắn muốn ${actionLabel} vào thời điểm hiện tại không?`,
      () => recordAttendance(),
    );
  };

  return (
    <FeaturePage
      title="Chấm công"
      description="Ghi nhận check-in/check-out qua Web. Hệ thống xác thực IP từ request và lưu GPS để đối chiếu khi cần."
      width="normal"
    >
      {isAdmin && (
        <FeatureCard title="Chế độ quản trị chấm công">
          <p className="text-sm leading-6 text-gray-600">
            Tài khoản Admin không dùng để check-in/check-out cá nhân vì không đại diện cho một hồ sơ nhân sự cụ thể.
            Admin vẫn có thể cấu hình tham số chấm công, xem mạng backend, quản lý OT và tổng hợp bảng công.
          </p>
        </FeatureCard>
      )}

      <FeatureCard>
        <div className="flex flex-col gap-6 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p className="text-sm font-medium text-gray-500">{currentDate}</p>
            <h2 className="mt-2 text-3xl font-bold text-gray-900">
              {new Date().toLocaleTimeString("vi-VN", {
                hour: "2-digit",
                minute: "2-digit",
              })}
            </h2>
            <p className="mt-2 text-sm leading-6 text-gray-600">
              {todayStatus?.message || "Hãy dùng đúng mạng đã cấu hình trước khi chấm công."}
            </p>
          </div>

          <button
            type="button"
            className={`${primaryButtonClass} min-h-12 px-6`}
            disabled={isAdmin || loading || statusLoading || todayStatus?.nextAction === "DONE"}
            onClick={handleAttendance}
          >
            {isAdmin ? "Chỉ dành cho nhân viên" : loading ? "Đang xác thực..." : getActionLabel(todayStatus?.nextAction)}
          </button>
        </div>
      </FeatureCard>

      <FeatureCard title="Khung thời gian làm việc">
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <InfoItem label="Ca làm" value={todayStatus?.shiftName || "Chưa cấu hình"} />
          <InfoItem label="Giờ vào" value={todayStatus?.startTime || "Chưa cấu hình"} />
          <InfoItem label="Giờ ra" value={todayStatus?.endTime || "Chưa cấu hình"} />
          <InfoItem label="Nghỉ giữa ca" value={formatBreakTime(todayStatus)} />
        </div>
      </FeatureCard>

      <FeatureCard title="Thông tin ghi nhận hôm nay">
        <div className="grid gap-4 sm:grid-cols-2">
          <InfoItem label="Trạng thái" value={todayStatus?.message || "Chưa có dữ liệu"} />
          <InfoItem label="Hành động kế tiếp" value={getActionLabel(todayStatus?.nextAction)} />
          <InfoItem label="Check-in" value={formatDateTime(todayStatus?.checkIn || lastResult?.checkIn)} />
          <InfoItem label="Check-out" value={formatDateTime(todayStatus?.checkOut || lastResult?.checkOut)} />
          <InfoItem label="Đi muộn" value={formatMinutes(todayStatus?.lateMinutes ?? lastResult?.lateMinutes)} />
          <InfoItem label="Về sớm" value={formatMinutes(todayStatus?.earlyLeaveMinutes ?? lastResult?.earlyLeaveMinutes)} />
          <InfoItem label="Ở lại sau ca" value={formatMinutes(todayStatus?.overtimeMinutes ?? lastResult?.overtimeMinutes)} />
          <InfoItem label="GPS gần nhất" value={formatGps(lastPosition)} />
          <InfoItem label="Độ chính xác GPS" value={lastPosition?.accuracy ? `${Math.round(lastPosition.accuracy)} m` : "Chưa có"} />
        </div>
      </FeatureCard>

      {isAdmin && (
        <FeatureCard title="Mạng backend đang nhìn thấy">
          <div className="grid gap-4 sm:grid-cols-3">
            <InfoItem label="Client IP" value={networkInfo?.clientIp || "Chưa xác định"} />
            <InfoItem label="CIDR gợi ý" value={networkInfo?.suggestedCidr || "Chưa xác định"} />
            <InfoItem label="Nguồn đọc IP" value={networkInfo?.source || "Chưa xác định"} />
          </div>
          <p className="mt-3 text-sm leading-6 text-gray-500">
            Khối này chỉ hiển thị với Admin để hỗ trợ cấu hình dải IP chấm công.
          </p>
        </FeatureCard>
      )}
    </FeaturePage>
  );
};

const InfoItem = ({ label, value }: { label: string; value: string }) => (
  <div className="rounded-lg border border-gray-200 bg-gray-50 p-4">
    <p className="text-xs font-semibold uppercase text-gray-500">{label}</p>
    <p className="mt-1 break-words text-sm font-medium text-gray-900">{value}</p>
  </div>
);

const getActionLabel = (action?: string) => {
  if (action === "CHECK_IN") return "Check-in";
  if (action === "CHECK_OUT") return "Check-out";
  if (action === "DONE") return "Đã hoàn tất";
  return "Chấm công";
};

const formatDateTime = (value?: string | null) => {
  if (!value) return "Chưa có";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "Không xác định";
  return date.toLocaleString("vi-VN");
};

const formatGps = (position: PositionState | null) => {
  if (!position) return "Chưa có";
  return `${position.latitude.toFixed(6)}, ${position.longitude.toFixed(6)}`;
};

const formatMinutes = (value?: number | null) => {
  if (!value || value <= 0) return "0 phút";
  const hours = Math.floor(value / 60);
  const minutes = value % 60;
  if (hours === 0) return `${minutes} phút`;
  if (minutes === 0) return `${hours} giờ`;
  return `${hours} giờ ${minutes} phút`;
};

const formatBreakTime = (status: AttendanceTodayStatus | null) => {
  if (!status?.breakStartTime || !status.breakEndTime) return "Không có";
  return `${status.breakStartTime} - ${status.breakEndTime}`;
};
