import { useCallback, useEffect, useState } from "react";
import { Button, Card } from "../../../components/ui";
import { FeaturePage } from "../../../core/components/FeatureShell";
import { useCurrentUser } from "../../../core/auth/hooks/useCurrentUser";
import { useNotification } from "../../../core/context/NotificationContext";
import { overtimeApi, type OvertimeRequest } from "../api/overtimeApi";
import { OvertimeActionButtons, OvertimeTable } from "./OvertimeTable";

export const OvertimeApprovalPage = () => {
  const { user } = useCurrentUser();
  const role = user?.role || "";
  const isManager = role === "Manager" || role === "Admin";
  const isHr = role === "HR" || role === "Admin";
  const { triggerAlert } = useNotification();

  const [loading, setLoading] = useState(false);
  const [managerRequests, setManagerRequests] = useState<OvertimeRequest[]>([]);
  const [hrRequests, setHrRequests] = useState<OvertimeRequest[]>([]);
  const [approvedRequests, setApprovedRequests] = useState<OvertimeRequest[]>([]);

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      if (isManager) {
        try {
          const managerRes = await overtimeApi.getPendingManager();
          setManagerRequests(managerRes.data);
        } catch {
          setManagerRequests([]);
        }
      }

      if (isHr) {
        try {
          const hrRes = await overtimeApi.getPendingHr();
          const approvedRes = await overtimeApi.getApproved();
          setHrRequests(hrRes.data);
          setApprovedRequests(approvedRes.data);
        } catch {
          setHrRequests([]);
          setApprovedRequests([]);
        }
      }
    } finally {
      setLoading(false);
    }
  }, [isHr, isManager]);

  useEffect(() => {
    void fetchData();
  }, [fetchData]);

  const review = (
    id: number,
    isApproved: boolean,
    scope: "manager" | "hr",
  ) => {
    triggerAlert(
      "confirm",
      isApproved ? "Xác nhận phê duyệt" : "Xác nhận từ chối",
      isApproved
        ? "Bạn chắc chắn muốn phê duyệt yêu cầu làm thêm này?"
        : "Bạn chắc chắn muốn từ chối yêu cầu làm thêm này?",
      async () => {
        try {
          if (scope === "manager") {
            await overtimeApi.managerReview(id, { isApproved });
          } else {
            await overtimeApi.hrConfirm(id, { isApproved });
          }

          triggerAlert(
            "success",
            "Đã xử lý yêu cầu",
            "Trạng thái yêu cầu làm thêm đã được cập nhật.",
          );
          await fetchData();
        } catch (error) {
          triggerAlert("error", "Không thể xử lý yêu cầu", getErrorMessage(error));
        }
      },
    );
  };

  const reconcile = async (id: number) => {
    try {
      await overtimeApi.reconcile(id);
      triggerAlert(
        "success",
        "Đã đối chiếu làm thêm",
        "Yêu cầu làm thêm đã được đối chiếu với dữ liệu chấm công.",
      );
      await fetchData();
    } catch (error) {
      triggerAlert("error", "Không thể đối chiếu làm thêm", getErrorMessage(error));
    }
  };

  return (
    <FeaturePage
      title="Phê duyệt làm thêm"
      description="Xử lý yêu cầu làm thêm theo vai trò được phân công."
      width="wide"
    >
      {loading && (
        <Card className="border-[var(--hicas-info)] bg-[var(--hicas-info-soft)] text-sm text-[var(--hicas-info)]">
          Đang tải dữ liệu...
        </Card>
      )}

      {isManager && (
        <OvertimeTable
          title="Chờ Trưởng phòng duyệt"
          data={managerRequests}
          emptyText="Chưa có yêu cầu làm thêm chờ duyệt."
          renderActions={(item) => (
            <OvertimeActionButtons
              onApprove={() => review(item.id, true, "manager")}
              onReject={() => review(item.id, false, "manager")}
            />
          )}
        />
      )}

      {isHr && (
        <OvertimeTable
          title="Chờ HR xác nhận"
          data={hrRequests}
          emptyText="Chưa có yêu cầu làm thêm chờ HR xác nhận."
          renderActions={(item) => (
            <OvertimeActionButtons
              onApprove={() => review(item.id, true, "hr")}
              onReject={() => review(item.id, false, "hr")}
            />
          )}
        />
      )}

      {isHr && (
        <OvertimeTable
          title="Làm thêm đã duyệt cần đối chiếu"
          data={approvedRequests}
          emptyText="Chưa có dữ liệu"
          renderActions={(item) =>
            item.isPayrollLocked ? (
              <span className="text-xs font-semibold text-[var(--hicas-text-secondary)]">
                Đã khóa lương
              </span>
            ) : (
              <Button size="sm" variant="secondary" onClick={() => reconcile(item.id)}>
                Đối chiếu
              </Button>
            )
          }
        />
      )}

      {!isManager && !isHr && (
        <Card className="border-[var(--hicas-warning)] bg-[var(--hicas-warning-soft)] text-sm text-amber-800">
          Vai trò hiện tại không có quyền phê duyệt làm thêm.
        </Card>
      )}
    </FeaturePage>
  );
};

const getErrorMessage = (error: unknown) =>
  error instanceof Error ? error.message : "Đã có lỗi xảy ra.";
