import React, { useEffect, useState } from "react";
import { CheckCircle2, Clock, FileText, UserCheck, XCircle } from "lucide-react";
import { PageHeader } from "../../../components/layout";
import { Badge, Button, Card, EmptyState } from "../../../components/ui";
import { BACKEND_URL } from "../../../core/api/config";
import { useNotification } from "../../../core/context/NotificationContext";
import { dependentApi } from "../api/dependentApi";
import { hrProfileApi } from "../api/hrProfileApi";
import type { PendingDependentRequest } from "../types/dependent";
import type { PendingProfileRequest } from "../types/profileRequest";

const formatDateTime = (value?: string | null) =>
  value ? new Date(value).toLocaleString("vi-VN") : "-";

const profileFieldLabels: Record<string, string> = {
  FullName: "Họ tên mới",
  IdentityNumber: "CCCD mới",
  TaxCode: "Mã số thuế",
  SocialInsCode: "Mã số BHXH",
  SocialInsJoinDate: "Ngày tham gia BHXH",
  InsuranceHospital: "Nơi khám chữa bệnh",
  BankAccount: "Số tài khoản",
  BankName: "Ngân hàng",
  PhoneNumber: "Số điện thoại",
  PersonalEmail: "Email cá nhân",
  CurrentAddress: "Chỗ ở hiện tại",
  PermanentAddress: "Địa chỉ thường trú",
  EmergencyContactName: "Người liên hệ khẩn cấp",
  EmergencyPhone: "Số điện thoại khẩn cấp",
  EmergencyRelation: "Quan hệ khẩn cấp",
};

const dependentFieldLabels: Record<string, string> = {
  FullName: "Họ tên",
  Relationship: "Quan hệ",
  IdNumber: "CCCD",
  TaxDependentCode: "MST phụ thuộc",
  BirthDate: "Ngày sinh",
  ValidFrom: "Hiệu lực từ",
  ValidTo: "Hiệu lực đến",
  Note: "Ghi chú",
};

const relationLabel = (value: unknown) =>
  ["Con", "Cha/Mẹ", "Vợ/Chồng", "Khác"][Number(value)] ?? String(value ?? "-");

const safeParse = (jsonString: string): Record<string, unknown> | null => {
  try {
    const parsed = JSON.parse(jsonString);
    return parsed && typeof parsed === "object" ? parsed : null;
  } catch {
    return null;
  }
};

type ChangeListProps = {
  jsonString: string;
  labels: Record<string, string>;
  evidenceUrl?: string | null;
  dependentMode?: boolean;
};

const ChangeList = ({ jsonString, labels, evidenceUrl, dependentMode = false }: ChangeListProps) => {
  const data = safeParse(jsonString);

  if (!data) {
    return <span className="text-sm text-[var(--hicas-danger)]">Không đọc được dữ liệu yêu cầu.</span>;
  }

  const hiddenKeys = new Set(["IdentityFrontUrl", "IdentityBackUrl", "CertificateUrl"]);
  const items = Object.entries(data).filter(
    ([key, value]) => !hiddenKeys.has(key) && value !== null && value !== undefined && value !== "",
  );

  return (
    <div className="space-y-2">
      {items.length === 0 ? (
        <p className="text-sm text-[var(--hicas-text-secondary)]">Không có trường thay đổi.</p>
      ) : (
        items.map(([key, value]) => (
          <div
            key={key}
            className="rounded-[var(--radius-md)] border border-[var(--hicas-border-soft)] bg-[var(--hicas-bg)] px-3 py-2 text-sm"
          >
            <span className="block text-xs font-medium text-[var(--hicas-text-secondary)]">
              {labels[key] || key}
            </span>
            <span className="mt-1 block font-semibold text-[var(--hicas-text-main)]">
              {dependentMode && key === "Relationship" ? relationLabel(value) : String(value)}
            </span>
          </div>
        ))
      )}

      {(data.IdentityFrontUrl || data.IdentityBackUrl || data.CertificateUrl || evidenceUrl) && (
        <div className="flex flex-wrap gap-2 pt-1">
          {Boolean(data.IdentityFrontUrl) && (
            <a
              href={`${BACKEND_URL}${String(data.IdentityFrontUrl)}`}
              target="_blank"
              rel="noreferrer"
              className="text-xs font-semibold text-[var(--hicas-orange-dark)] hover:underline"
            >
              CCCD mặt trước
            </a>
          )}
          {Boolean(data.IdentityBackUrl) && (
            <a
              href={`${BACKEND_URL}${String(data.IdentityBackUrl)}`}
              target="_blank"
              rel="noreferrer"
              className="text-xs font-semibold text-[var(--hicas-orange-dark)] hover:underline"
            >
              CCCD mặt sau
            </a>
          )}
          {Boolean(data.CertificateUrl) && (
            <a
              href={`${BACKEND_URL}${String(data.CertificateUrl)}`}
              target="_blank"
              rel="noreferrer"
              className="text-xs font-semibold text-[var(--hicas-orange-dark)] hover:underline"
            >
              Chứng chỉ / bằng cấp
            </a>
          )}
          {evidenceUrl && (
            <a
              href={`${BACKEND_URL}${evidenceUrl}`}
              target="_blank"
              rel="noreferrer"
              className="text-xs font-semibold text-[var(--hicas-orange-dark)] hover:underline"
            >
              Minh chứng
            </a>
          )}
        </div>
      )}
    </div>
  );
};

export const HRProfileReviewList: React.FC = () => {
  const [requests, setRequests] = useState<PendingProfileRequest[]>([]);
  const [dependentRequests, setDependentRequests] = useState<PendingDependentRequest[]>([]);
  const [loading, setLoading] = useState(false);
  const [processingId, setProcessingId] = useState<number | null>(null);
  const { triggerAlert } = useNotification();

  const [rejectingId, setRejectingId] = useState<number | null>(null);
  const [rejectReason, setRejectReason] = useState("");
  const [dependentRejectingId, setDependentRejectingId] = useState<number | null>(null);
  const [dependentRejectReason, setDependentRejectReason] = useState("");

  useEffect(() => {
    const fetchRequests = async () => {
      setLoading(true);
      try {
        const [profileRes, dependentRes] = await Promise.all([
          hrProfileApi.getPendingRequests(),
          dependentApi.getPendingRequests(),
        ]);
        setRequests(profileRes.data || profileRes || []);
        setDependentRequests(dependentRes.data || dependentRes || []);
      } catch (error) {
        console.error("Lỗi tải danh sách:", error);
      } finally {
        setLoading(false);
      }
    };
    fetchRequests();
  }, []);

  const executeReview = async (id: number, isApproved: boolean, reason?: string) => {
    setProcessingId(id);
    try {
      const response: unknown = await hrProfileApi.reviewRequest(id, {
        isApproved,
        rejectReason: reason,
      });

      const msg =
        (response as { message?: string })?.message ||
        (response as { Message?: string })?.Message ||
        "Thao tác thành công.";
      triggerAlert("success", "Thành công", msg);

      setRequests((prev) => prev.filter((item) => item.id !== id));
      setRejectingId(null);
      setRejectReason("");
    } catch (error: unknown) {
      console.error(error);
      triggerAlert(
        "error",
        "Lỗi xử lý",
        "Không thể xử lý yêu cầu hồ sơ. Vui lòng tải lại trang để kiểm tra trạng thái mới nhất.",
      );
    } finally {
      setProcessingId(null);
    }
  };

  const executeDependentReview = async (id: number, isApproved: boolean, reason?: string) => {
    setProcessingId(id);
    try {
      const response = await dependentApi.reviewRequest(id, {
        isApproved,
        rejectReason: reason,
      });
      triggerAlert(
        "success",
        "Thành công",
        response.message || "Đã xử lý yêu cầu người phụ thuộc.",
      );
      setDependentRequests((prev) => prev.filter((item) => item.id !== id));
      setDependentRejectingId(null);
      setDependentRejectReason("");
    } catch (error: unknown) {
      const msg =
        error instanceof Error ? error.message : "Không thể xử lý yêu cầu người phụ thuộc.";
      triggerAlert("error", "Lỗi xử lý", msg);
    } finally {
      setProcessingId(null);
    }
  };

  const handleApprove = (id: number) => {
    triggerAlert(
      "confirm",
      "Xác nhận phê duyệt",
      "Xác nhận hồ sơ hợp lệ và ghi đè dữ liệu gốc?",
      () => executeReview(id, true),
    );
  };

  const handleConfirmReject = (id: number) => {
    if (!rejectReason.trim()) {
      triggerAlert("warning", "Thiếu lý do", "Bạn phải nhập lý do từ chối.");
      return;
    }
    executeReview(id, false, rejectReason.trim());
  };

  const handleConfirmDependentReject = (id: number) => {
    if (!dependentRejectReason.trim()) {
      triggerAlert("warning", "Thiếu lý do", "Bạn phải nhập lý do từ chối.");
      return;
    }
    executeDependentReview(id, false, dependentRejectReason.trim());
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="Phê duyệt cập nhật hồ sơ"
        description="Kiểm tra yêu cầu thay đổi hồ sơ trước khi cập nhật."
        breadcrumb={[
          { label: "Hồ sơ & hợp đồng" },
          { label: "Phê duyệt hồ sơ" },
        ]}
        actions={
          <div className="flex flex-wrap gap-2">
            <Badge variant="warning">{requests.length} hồ sơ</Badge>
            <Badge variant="info">{dependentRequests.length} người phụ thuộc</Badge>
          </div>
        }
      />

      {loading ? (
        <Card>
          <div className="py-12 text-center text-sm text-[var(--hicas-text-secondary)]">
            Đang tải dữ liệu...
          </div>
        </Card>
      ) : (
        <div className="space-y-6">
          <Card
            title="Yêu cầu cập nhật hồ sơ"
            description="Các thay đổi thông tin cá nhân và tài liệu minh chứng."
            actions={<UserCheck size={20} className="text-[var(--hicas-orange)]" />}
          >
            {requests.length === 0 ? (
              <EmptyState
                title="Chưa có hồ sơ chờ duyệt"
                description="Hiện không có yêu cầu cập nhật hồ sơ đang tồn đọng."
              />
            ) : (
              <div className="space-y-4">
                {requests.map((req) => (
                  <article
                    key={req.id}
                    className="grid gap-4 rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-white p-4 xl:grid-cols-[260px_minmax(0,1fr)_220px]"
                  >
                    <div className="space-y-3">
                      <div>
                        <h3 className="font-semibold text-[var(--hicas-text-main)]">
                          {req.employeeName}
                        </h3>
                        <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">
                          Mã NV: {req.employeeCode}
                        </p>
                      </div>
                      <Badge variant="warning">
                        <span className="inline-flex items-center gap-1">
                          <Clock size={13} />
                          SLA: {formatDateTime(req.deadlineSLA)}
                        </span>
                      </Badge>
                    </div>

                    <ChangeList jsonString={req.requestedDataJson} labels={profileFieldLabels} />

                    <div className="flex flex-col justify-center gap-2">
                      {rejectingId === req.id ? (
                        <>
                          <input
                            type="text"
                            autoFocus
                            placeholder="Lý do từ chối"
                            value={rejectReason}
                            onChange={(event) => setRejectReason(event.target.value)}
                            className="hicas-input w-full"
                          />
                          <div className="grid grid-cols-2 gap-2">
                            <Button
                              type="button"
                              size="sm"
                              variant="danger"
                              onClick={() => handleConfirmReject(req.id)}
                              disabled={processingId === req.id}
                            >
                              Chốt
                            </Button>
                            <Button
                              type="button"
                              size="sm"
                              variant="secondary"
                              onClick={() => {
                                setRejectingId(null);
                                setRejectReason("");
                              }}
                              disabled={processingId === req.id}
                            >
                              Hủy
                            </Button>
                          </div>
                        </>
                      ) : (
                        <>
                          <Button
                            type="button"
                            size="sm"
                            iconLeft={<CheckCircle2 size={15} />}
                            onClick={() => handleApprove(req.id)}
                            disabled={processingId === req.id || rejectingId !== null}
                            isLoading={processingId === req.id}
                          >
                            Phê duyệt
                          </Button>
                          <Button
                            type="button"
                            size="sm"
                            variant="danger"
                            iconLeft={<XCircle size={15} />}
                            onClick={() => setRejectingId(req.id)}
                            disabled={processingId === req.id || rejectingId !== null}
                          >
                            Từ chối
                          </Button>
                        </>
                      )}
                    </div>
                  </article>
                ))}
              </div>
            )}
          </Card>

          <Card
            title="Yêu cầu người phụ thuộc"
            description="Các yêu cầu thêm, sửa hoặc ngừng hiệu lực người phụ thuộc của nhân viên."
            actions={<FileText size={20} className="text-[var(--hicas-orange)]" />}
          >
            {dependentRequests.length === 0 ? (
              <EmptyState
                title="Chưa có yêu cầu người phụ thuộc"
                description="Hiện không có yêu cầu người phụ thuộc đang chờ duyệt."
              />
            ) : (
              <div className="space-y-4">
                {dependentRequests.map((req) => (
                  <article
                    key={req.id}
                    className="grid gap-4 rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-white p-4 xl:grid-cols-[260px_minmax(0,1fr)_220px]"
                  >
                    <div className="space-y-3">
                      <div>
                        <h3 className="font-semibold text-[var(--hicas-text-main)]">
                          {req.employeeName}
                        </h3>
                        <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">
                          Mã NV: {req.employeeCode}
                        </p>
                      </div>
                      <Badge variant="info">
                        {req.actionType === "CREATE"
                          ? "Thêm mới"
                          : req.actionType === "UPDATE"
                            ? "Cập nhật"
                            : "Ngừng hiệu lực"}
                      </Badge>
                    </div>

                    <ChangeList
                      jsonString={req.requestedDataJson}
                      labels={dependentFieldLabels}
                      evidenceUrl={req.evidenceUrl}
                      dependentMode
                    />

                    <div className="flex flex-col justify-center gap-2">
                      {dependentRejectingId === req.id ? (
                        <>
                          <input
                            type="text"
                            autoFocus
                            placeholder="Lý do từ chối"
                            value={dependentRejectReason}
                            onChange={(event) => setDependentRejectReason(event.target.value)}
                            className="hicas-input w-full"
                          />
                          <div className="grid grid-cols-2 gap-2">
                            <Button
                              type="button"
                              size="sm"
                              variant="danger"
                              onClick={() => handleConfirmDependentReject(req.id)}
                              disabled={processingId === req.id}
                            >
                              Chốt
                            </Button>
                            <Button
                              type="button"
                              size="sm"
                              variant="secondary"
                              onClick={() => {
                                setDependentRejectingId(null);
                                setDependentRejectReason("");
                              }}
                              disabled={processingId === req.id}
                            >
                              Hủy
                            </Button>
                          </div>
                        </>
                      ) : (
                        <>
                          <Button
                            type="button"
                            size="sm"
                            iconLeft={<CheckCircle2 size={15} />}
                            onClick={() => executeDependentReview(req.id, true)}
                            disabled={processingId === req.id || dependentRejectingId !== null}
                            isLoading={processingId === req.id}
                          >
                            Phê duyệt
                          </Button>
                          <Button
                            type="button"
                            size="sm"
                            variant="danger"
                            iconLeft={<XCircle size={15} />}
                            onClick={() => setDependentRejectingId(req.id)}
                            disabled={processingId === req.id || dependentRejectingId !== null}
                          >
                            Từ chối
                          </Button>
                        </>
                      )}
                    </div>
                  </article>
                ))}
              </div>
            )}
          </Card>
        </div>
      )}
    </div>
  );
};
