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
      isApproved ? "Xác nhận duyệt OT" : "Xác nhận từ chối OT",
      isApproved
        ? "Bạn chắc chắn muốn duyệt yêu cầu OT này?"
        : "Bạn chắc chắn muốn từ chối yêu cầu OT này?",
      async () => {
        try {
          if (scope === "manager") {
            await overtimeApi.managerReview(id, { isApproved });
          } else {
            await overtimeApi.hrConfirm(id, { isApproved });
          }

          triggerAlert(
            "success",
            "Đã xử lý OT",
            "Trạng thái yêu cầu OT đã được cập nhật.",
          );
          await fetchData();
        } catch (error) {
          triggerAlert("error", "Không thể xử lý OT", getErrorMessage(error));
        }
      },
    );
  };

  const reconcile = async (id: number) => {
    try {
      await overtimeApi.reconcile(id);
      triggerAlert(
        "success",
        "Đã đối chiếu OT",
        "Hệ thống đã đối chiếu yêu cầu OT với dữ liệu chấm công.",
      );
      await fetchData();
    } catch (error) {
      triggerAlert("error", "Không thể đối chiếu OT", getErrorMessage(error));
    }
  };

  return (
    <FeaturePage
      title="Phê duyệt OT"
      description="Xử lý yêu cầu OT theo vai trò: Trưởng phòng duyệt nghiệp vụ, HR xác nhận chính sách và đối chiếu chấm công."
      width="wide"
    >
      {loading && (
        <Card className="border-[var(--hicas-info)] bg-[var(--hicas-info-soft)] text-sm text-[var(--hicas-info)]">
          Đang tải danh sách phê duyệt OT...
        </Card>
      )}

      {isManager && (
        <OvertimeTable
          title="Chờ Trưởng phòng duyệt"
          data={managerRequests}
          emptyText="Không có yêu cầu OT chờ duyệt nghiệp vụ."
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
          emptyText="Không có yêu cầu OT chờ HR xác nhận."
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
          title="OT đã duyệt cần đối chiếu"
          data={approvedRequests}
          emptyText="Chưa có OT đã duyệt trong danh sách."
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
          Vai trò hiện tại không có quyền phê duyệt OT.
        </Card>
      )}
    </FeaturePage>
  );
};

const getErrorMessage = (error: unknown) =>
  error instanceof Error ? error.message : "Đã có lỗi xảy ra.";
