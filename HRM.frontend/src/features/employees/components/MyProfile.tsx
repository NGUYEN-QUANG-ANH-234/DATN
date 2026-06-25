import React from "react";
import {
  Building2,
  FileText,
  History,
  HeartPulse,
  IdCard,
  Landmark,
  Mail,
  Phone,
  UserRound,
} from "lucide-react";
import { PageHeader } from "../../../components/layout";
import { Badge, Button, Card, EmptyState, LoadingState } from "../../../components/ui";
import { BACKEND_URL } from "../../../core/api/config";
import { useMyProfileData } from "../hooks/useMyProfileData";

const formatDate = (value?: string | null) =>
  value ? new Date(value).toLocaleDateString("vi-VN") : "Chưa cập nhật";

const fallback = (value?: string | null) => value || "Chưa cập nhật";

const getGenderText = (value?: string | null) => {
  if (!value) return "Chưa cập nhật";

  const normalized = value.trim().toLowerCase();
  if (["0", "male", "nam"].includes(normalized)) return "Nam";
  if (["1", "female", "nu", "nữ"].includes(normalized)) return "Nữ";
  if (["2", "other", "khac", "khác"].includes(normalized)) return "Khác";

  return value;
};

const getDependentRelationText = (value: number) =>
  ["Con", "Cha/Mẹ", "Vợ/Chồng", "Khác"][value] ?? "Khác";

const getStatusLabel = (status?: string | null) => {
  switch ((status || "").toLowerCase()) {
    case "active":
      return "Đang làm việc";
    case "inactive":
      return "Tạm ngưng";
    case "resigned":
      return "Đã nghỉ việc";
    case "dismissed":
      return "Đã chấm dứt";
    default:
      return status || "Chưa cập nhật";
  }
};

const getInitials = (fullName?: string) => {
  const parts = (fullName || "HICAS").trim().split(/\s+/);
  return parts
    .slice(-2)
    .map((part) => part[0])
    .join("")
    .toUpperCase();
};

type InfoItemProps = {
  label: string;
  value: React.ReactNode;
};

const InfoItem = ({ label, value }: InfoItemProps) => (
  <div className="rounded-[var(--radius-md)] border border-[var(--hicas-border-soft)] bg-[var(--hicas-bg)] px-4 py-3">
    <p className="text-xs font-medium text-[var(--hicas-text-secondary)]">{label}</p>
    <p className="mt-1 break-words text-sm font-semibold text-[var(--hicas-text-main)]">
      {value}
    </p>
  </div>
);

type InfoSectionProps = {
  title: string;
  icon: React.ReactNode;
  children: React.ReactNode;
};

const InfoSection = ({ title, icon, children }: InfoSectionProps) => (
  <Card title={title} actions={<span className="text-[var(--hicas-orange)]">{icon}</span>}>
    <div className="grid gap-3 sm:grid-cols-2">{children}</div>
  </Card>
);

export const MyProfile: React.FC = () => {
  const { profile, dependents, loadingDependents, loadingProfile } = useMyProfileData({
    includeContracts: false,
  });

  if (loadingProfile) {
    return <LoadingState title="Đang tải hồ sơ cá nhân..." />;
  }

  if (!profile) {
    return (
      <Card>
        <EmptyState
          title="Không tìm thấy hồ sơ"
          description="Tài khoản hiện tại chưa được liên kết với hồ sơ nhân sự."
        />
      </Card>
    );
  }

  const avatarSrc = profile.avatarUrl ? `${BACKEND_URL}${profile.avatarUrl}` : "";
  const documentLinks = [
    { label: "CCCD mặt trước", url: profile.identityFrontUrl },
    { label: "CCCD mặt sau", url: profile.identityBackUrl },
    { label: "Bằng cấp hoặc chứng chỉ", url: profile.certificateUrl },
  ].filter((item) => item.url);

  const isActive = profile.status?.toLowerCase().includes("active");

  return (
    <div className="space-y-6">
      <PageHeader
        title="Hồ sơ cá nhân"
        description="Xem thông tin cá nhân, giấy tờ, bảo hiểm và người phụ thuộc."
        breadcrumb={[
          { label: "Hồ sơ & hợp đồng" },
          { label: "Hồ sơ cá nhân" },
        ]}
        actions={
          <div className="flex flex-wrap gap-2">
            <Button
              variant="secondary"
              iconLeft={<History size={16} />}
              onClick={() => window.location.assign("/employee-contract/history?type=PROFILE")}
            >
              Lịch sử hồ sơ
            </Button>
            <Badge variant={isActive ? "success" : "neutral"}>
              {getStatusLabel(profile.status)}
            </Badge>
          </div>
        }
      />

      <Card className="overflow-hidden" padded={false}>
        <div className="border-b border-[var(--hicas-border-soft)] bg-[var(--hicas-bg)] px-6 py-5">
          <div className="flex flex-col gap-5 md:flex-row md:items-center md:justify-between">
            <div className="flex items-center gap-5">
              {avatarSrc ? (
                <img
                  src={avatarSrc}
                  alt={profile.fullName}
                  className="h-20 w-20 rounded-[var(--radius-xl)] border border-[var(--hicas-border)] object-cover shadow-sm"
                />
              ) : (
                <div className="flex h-20 w-20 items-center justify-center rounded-[var(--radius-xl)] border border-[var(--hicas-border)] bg-white text-xl font-bold text-[var(--hicas-orange-dark)] shadow-sm">
                  {getInitials(profile.fullName)}
                </div>
              )}
              <div>
                <h2 className="text-2xl font-bold text-[var(--hicas-text-main)]">
                  {profile.fullName}
                </h2>
                <div className="mt-2 flex flex-wrap items-center gap-2">
                  <Badge variant="orange">Mã NV: {profile.employeeCode}</Badge>
                  <Badge variant="info">Ngày vào: {formatDate(profile.joinedDate)}</Badge>
                </div>
              </div>
            </div>
            <div className="grid gap-2 text-sm text-[var(--hicas-text-secondary)] sm:grid-cols-2 md:min-w-[360px]">
              <span className="flex items-center gap-2">
                <Mail size={16} />
                {fallback(profile.personalEmail)}
              </span>
              <span className="flex items-center gap-2">
                <Phone size={16} />
                {fallback(profile.phoneNumber)}
              </span>
            </div>
          </div>
        </div>
      </Card>

      <div className="grid gap-6 xl:grid-cols-2">
        <InfoSection title="Cá nhân & liên hệ" icon={<UserRound size={18} />}>
          <InfoItem label="Giới tính" value={getGenderText(profile.gender)} />
          <InfoItem label="Ngày sinh" value={formatDate(profile.birthDate)} />
          <InfoItem label="Quốc tịch" value={fallback(profile.nationality)} />
          <InfoItem label="Dân tộc" value={fallback(profile.ethnicity)} />
          <InfoItem label="Số điện thoại" value={fallback(profile.phoneNumber)} />
          <InfoItem label="Email cá nhân" value={fallback(profile.personalEmail)} />
          <InfoItem label="Chỗ ở hiện tại" value={fallback(profile.currentAddress)} />
          <InfoItem label="Địa chỉ thường trú" value={fallback(profile.permanentAddress)} />
        </InfoSection>

        <InfoSection title="Liên hệ khẩn cấp" icon={<HeartPulse size={18} />}>
          <InfoItem label="Người liên hệ" value={fallback(profile.emergencyContactName)} />
          <InfoItem label="Số điện thoại" value={fallback(profile.emergencyPhone)} />
          <InfoItem label="Quan hệ" value={fallback(profile.emergencyRelation)} />
        </InfoSection>

        <InfoSection title="Giấy tờ & bảo hiểm" icon={<IdCard size={18} />}>
          <InfoItem label="Số CCCD" value={fallback(profile.identityNumber)} />
          <InfoItem label="Mã số thuế" value={fallback(profile.taxCode)} />
          <InfoItem label="Số BHXH" value={fallback(profile.socialInsCode)} />
          <InfoItem label="Ngày tham gia BHXH" value={formatDate(profile.socialInsJoinDate)} />
          <InfoItem label="Nơi khám chữa bệnh" value={fallback(profile.insuranceHospital)} />
        </InfoSection>

        <InfoSection title="Thanh toán lương" icon={<Landmark size={18} />}>
          <InfoItem label="Ngân hàng" value={fallback(profile.bankName)} />
          <InfoItem label="Số tài khoản" value={fallback(profile.bankAccount)} />
          <InfoItem label="Ngày gia nhập" value={formatDate(profile.joinedDate)} />
        </InfoSection>
      </div>

      <div className="grid gap-6 xl:grid-cols-[minmax(0,0.85fr)_minmax(0,1.15fr)]">
        <Card
          title="Tài liệu minh chứng"
          description="Các giấy tờ đã được lưu kèm hồ sơ cá nhân."
          actions={<FileText size={20} className="text-[var(--hicas-orange)]" />}
        >
          {documentLinks.length === 0 ? (
            <EmptyState
              title="Chưa có tài liệu"
              description="Hồ sơ hiện chưa có tài liệu minh chứng được tải lên."
            />
          ) : (
            <div className="grid gap-3">
              {documentLinks.map((item) => (
                <a
                  key={item.label}
                  href={`${BACKEND_URL}${item.url}`}
                  target="_blank"
                  rel="noreferrer"
                  className="flex items-center justify-between rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white px-4 py-3 text-sm font-semibold text-[var(--hicas-text-main)] transition hover:border-[var(--hicas-orange)] hover:bg-[var(--hicas-orange-lighter)]"
                >
                  <span className="flex items-center gap-2">
                    <FileText size={16} className="text-[var(--hicas-orange)]" />
                    {item.label}
                  </span>
                  <span className="text-xs text-[var(--hicas-text-secondary)]">Mở</span>
                </a>
              ))}
            </div>
          )}
        </Card>

        <Card
          title="Người phụ thuộc"
          description="Danh sách người phụ thuộc đang được ghi nhận trong hồ sơ."
          actions={<Building2 size={20} className="text-[var(--hicas-orange)]" />}
        >
          {loadingDependents ? (
            <LoadingState title="Đang tải người phụ thuộc..." className="border-0 bg-transparent" />
          ) : dependents.length === 0 ? (
            <EmptyState
              title="Chưa có người phụ thuộc"
              description="Hiện chưa có người phụ thuộc đang hiệu lực."
            />
          ) : (
            <div className="overflow-auto rounded-[var(--radius-lg)] border border-[var(--hicas-border)]">
              <table className="min-w-full divide-y divide-[var(--hicas-border-soft)] text-sm">
                <thead className="bg-[var(--hicas-bg)] text-left text-xs font-semibold uppercase text-[var(--hicas-text-secondary)]">
                  <tr>
                    <th className="px-4 py-3">Họ tên</th>
                    <th className="px-4 py-3">Quan hệ</th>
                    <th className="px-4 py-3">MST phụ thuộc</th>
                    <th className="px-4 py-3">Hiệu lực</th>
                    <th className="px-4 py-3">Trạng thái</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[var(--hicas-border-soft)] bg-white">
                  {dependents.map((item) => (
                    <tr key={item.id}>
                      <td className="px-4 py-3">
                        <div className="font-semibold text-[var(--hicas-text-main)]">
                          {item.fullName}
                        </div>
                        <div className="text-xs text-[var(--hicas-text-secondary)]">
                          {item.idNumber || "Chưa có CCCD"}
                        </div>
                      </td>
                      <td className="px-4 py-3">{getDependentRelationText(item.relationship)}</td>
                      <td className="px-4 py-3">{item.taxDependentCode || "-"}</td>
                      <td className="px-4 py-3">
                        {formatDate(item.validFrom)}
                        {item.validTo ? ` - ${formatDate(item.validTo)}` : ""}
                      </td>
                      <td className="px-4 py-3">
                        <Badge variant={item.isActive ? "success" : "neutral"}>
                          {item.isActive ? "Đang hiệu lực" : "Ngừng hiệu lực"}
                        </Badge>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>
      </div>
    </div>
  );
};
