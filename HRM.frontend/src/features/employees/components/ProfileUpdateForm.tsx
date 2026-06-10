import React, { useState } from "react";
import { Badge, Button, Card } from "../../../components/ui";
import { PageHeader } from "../../../components/layout";
import { Building2, FileUp, HeartPulse, IdCard, Landmark, Save, UserRound } from "lucide-react";
import axiosClient from "../../../core/api/axiosClient";
import { useNotification } from "../../../core/context/NotificationContext";
import type { ProfileUpdateFormState } from "../types/profile";
import { useMyProfileData } from "../hooks/useMyProfileData";
import { DependentManagement } from "./DependentManagement";

const MAX_FILE_SIZE = 5 * 1024 * 1024;
const ALLOWED_TYPES = ["image/jpeg", "image/png", "application/pdf"];
const ALLOWED_IMAGE_TYPES = ["image/jpeg", "image/png"];

const initialFormState: ProfileUpdateFormState = {
  fullName: "",
  gender: "",
  birthDate: "",
  phoneNumber: "",
  personalEmail: "",
  currentAddress: "",
  permanentAddress: "",
  identityNumber: "",
  taxCode: "",
  socialInsCode: "",
  socialInsJoinDate: "",
  insuranceHospital: "",
  bankAccount: "",
  bankName: "",
  emergencyContactName: "",
  emergencyPhone: "",
  emergencyRelation: "",
  avatarFile: null,
  identityFrontFile: null,
  identityBackFile: null,
  certificateFile: null,
};

type TextInputProps = {
  label: string;
  value: string;
  type?: string;
  placeholder?: string;
  onChange: (value: string) => void;
};

const TextInput = ({ label, value, type = "text", placeholder, onChange }: TextInputProps) => (
  <label className="block">
    <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">{label}</span>
    <input
      type={type}
      value={value}
      placeholder={placeholder}
      onChange={(event) => onChange(event.target.value)}
      className="hicas-input w-full"
    />
  </label>
);

type SelectInputProps = {
  label: string;
  value: string;
  onChange: (value: string) => void;
  children: React.ReactNode;
};

const SelectInput = ({ label, value, onChange, children }: SelectInputProps) => (
  <label className="block">
    <span className="mb-1 block text-sm font-medium text-[var(--hicas-text-main)]">{label}</span>
    <select
      value={value}
      onChange={(event) => onChange(event.target.value)}
      className="hicas-select w-full"
    >
      {children}
    </select>
  </label>
);

type FileInputProps = {
  label: string;
  accept: string;
  onChange: (event: React.ChangeEvent<HTMLInputElement>) => void;
};

const FileInput = ({ label, accept, onChange }: FileInputProps) => (
  <label className="block rounded-[var(--radius-md)] border border-[var(--hicas-border)] bg-white px-4 py-3">
    <span className="mb-2 block text-sm font-semibold text-[var(--hicas-text-main)]">{label}</span>
    <input
      type="file"
      accept={accept}
      onChange={onChange}
      className="block w-full text-sm text-[var(--hicas-text-secondary)] file:mr-3 file:rounded-[var(--radius-sm)] file:border-0 file:bg-[var(--hicas-orange-soft)] file:px-3 file:py-2 file:text-sm file:font-semibold file:text-[var(--hicas-orange-dark)]"
    />
  </label>
);

export const ProfileUpdateForm: React.FC = () => {
  const [formState, setFormState] = useState<ProfileUpdateFormState>(initialFormState);
  const [submitting, setSubmitting] = useState(false);
  const { triggerAlert } = useNotification();
  const { dependents, loadingDependents, refreshDependents } = useMyProfileData({
    includeProfile: false,
    includeContracts: false,
  });

  const updateField = <K extends keyof ProfileUpdateFormState>(
    field: K,
    value: ProfileUpdateFormState[K],
  ) => {
    setFormState((prev) => ({ ...prev, [field]: value }));
  };

  const resetForm = () => {
    setFormState(initialFormState);
    document.querySelectorAll("input[type=file]").forEach((el: Element) => {
      (el as HTMLInputElement).value = "";
    });
  };

  const handleFileChange = (
    event: React.ChangeEvent<HTMLInputElement>,
    fieldName: keyof ProfileUpdateFormState,
    onlyImage = false,
  ) => {
    const file = event.target.files?.[0];
    if (!file) return;

    if (file.size > MAX_FILE_SIZE) {
      triggerAlert("warning", "File quá lớn", "Dung lượng file vượt quá 5MB.");
      event.target.value = "";
      return;
    }

    const typesToCheck = onlyImage ? ALLOWED_IMAGE_TYPES : ALLOWED_TYPES;
    if (!typesToCheck.includes(file.type)) {
      triggerAlert(
        "warning",
        "File không hợp lệ",
        onlyImage ? "Chỉ chấp nhận ảnh JPG, PNG." : "Chỉ chấp nhận định dạng JPG, PNG hoặc PDF.",
      );
      event.target.value = "";
      return;
    }

    updateField(fieldName, file);
  };

  const appendIfPresent = (formData: FormData, key: string, value: string | File | null) => {
    if (value instanceof File) {
      formData.append(key, value);
      return;
    }

    if (typeof value === "string" && value.trim()) {
      formData.append(key, value.trim());
    }
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    const formData = new FormData();

    appendIfPresent(formData, "FullName", formState.fullName);
    appendIfPresent(formData, "Gender", formState.gender);
    appendIfPresent(formData, "BirthDate", formState.birthDate);
    appendIfPresent(formData, "PhoneNumber", formState.phoneNumber);
    appendIfPresent(formData, "PersonalEmail", formState.personalEmail);
    appendIfPresent(formData, "CurrentAddress", formState.currentAddress);
    appendIfPresent(formData, "PermanentAddress", formState.permanentAddress);
    appendIfPresent(formData, "IdentityNumber", formState.identityNumber);
    appendIfPresent(formData, "TaxCode", formState.taxCode);
    appendIfPresent(formData, "SocialInsCode", formState.socialInsCode);
    appendIfPresent(formData, "SocialInsJoinDate", formState.socialInsJoinDate);
    appendIfPresent(formData, "InsuranceHospital", formState.insuranceHospital);
    appendIfPresent(formData, "BankAccount", formState.bankAccount);
    appendIfPresent(formData, "BankName", formState.bankName);
    appendIfPresent(formData, "EmergencyContactName", formState.emergencyContactName);
    appendIfPresent(formData, "EmergencyPhone", formState.emergencyPhone);
    appendIfPresent(formData, "EmergencyRelation", formState.emergencyRelation);
    appendIfPresent(formData, "AvatarFile", formState.avatarFile);
    appendIfPresent(formData, "IdentityFrontFile", formState.identityFrontFile);
    appendIfPresent(formData, "IdentityBackFile", formState.identityBackFile);
    appendIfPresent(formData, "CertificateFile", formState.certificateFile);

    let hasData = false;
    for (const _ of formData.entries()) {
      hasData = true;
      break;
    }

    if (!hasData) {
      triggerAlert(
        "warning",
        "Thiếu thông tin",
        "Vui lòng nhập ít nhất một thông tin cần cập nhật.",
      );
      return;
    }

    setSubmitting(true);
    try {
      const response: unknown = await axiosClient.patch("/employees/profile", formData, {
        headers: {
          "Content-Type": "multipart/form-data",
        },
      });

      const resObj = response as {
        data?: { message?: string; Message?: string };
        message?: string;
        Message?: string;
      };

      const responseData = resObj?.data !== undefined ? resObj.data : resObj;
      const msg =
        responseData?.message ||
        responseData?.Message ||
        "Đã gửi yêu cầu cập nhật thành công.";

      triggerAlert("success", "Đã gửi yêu cầu", msg);
      resetForm();
    } catch (error: unknown) {
      console.error("Lỗi giao diện/API:", error);

      const errObj = error as {
        response?: {
          data?: { message?: string; Message?: string };
          message?: string;
          Message?: string;
        };
      };

      const errData = errObj.response?.data || errObj.response || {};
      const errMsg =
        errData?.message || errData?.Message || "Đã xảy ra lỗi khi cập nhật hồ sơ.";

      triggerAlert("error", "Lỗi", errMsg);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="Cập nhật hồ sơ cá nhân"
        description="Gửi thay đổi hồ sơ để HR kiểm tra trước khi cập nhật."
        breadcrumb={[
          { label: "Hồ sơ & hợp đồng" },
          { label: "Cập nhật hồ sơ" },
        ]}
        actions={<Badge variant="warning">Chờ HR phê duyệt</Badge>}
      />

      <form onSubmit={handleSubmit} className="space-y-6">
        <div className="grid gap-6 xl:grid-cols-2">
          <Card
            title="Cá nhân & liên hệ"
            description="Thông tin nhận diện và liên hệ của nhân viên."
            actions={<UserRound size={20} className="text-[var(--hicas-orange)]" />}
          >
            <div className="grid gap-4 md:grid-cols-2">
              <TextInput
                label="Họ tên mới"
                value={formState.fullName}
                onChange={(value) => updateField("fullName", value)}
              />
              <SelectInput
                label="Giới tính"
                value={formState.gender}
                onChange={(value) => updateField("gender", value)}
              >
                <option value="">Bỏ qua</option>
                <option value="0">Nam</option>
                <option value="1">Nữ</option>
                <option value="2">Khác</option>
              </SelectInput>
              <TextInput
                label="Ngày sinh"
                type="date"
                value={formState.birthDate}
                onChange={(value) => updateField("birthDate", value)}
              />
              <TextInput
                label="Số điện thoại"
                value={formState.phoneNumber}
                onChange={(value) => updateField("phoneNumber", value)}
              />
              <div className="md:col-span-2">
                <TextInput
                  label="Email cá nhân"
                  type="email"
                  value={formState.personalEmail}
                  onChange={(value) => updateField("personalEmail", value)}
                />
              </div>
              <div className="md:col-span-2">
                <TextInput
                  label="Chỗ ở hiện tại"
                  value={formState.currentAddress}
                  onChange={(value) => updateField("currentAddress", value)}
                />
              </div>
              <div className="md:col-span-2">
                <TextInput
                  label="Địa chỉ thường trú"
                  value={formState.permanentAddress}
                  onChange={(value) => updateField("permanentAddress", value)}
                />
              </div>
            </div>
          </Card>

          <div className="grid gap-6">
            <Card
              title="Liên hệ khẩn cấp"
              description="Thông tin dùng khi công ty cần liên hệ trong tình huống khẩn cấp."
              actions={<HeartPulse size={20} className="text-[var(--hicas-orange)]" />}
            >
              <div className="grid gap-4 md:grid-cols-2">
                <div className="md:col-span-2">
                  <TextInput
                    label="Họ tên người liên hệ"
                    value={formState.emergencyContactName}
                    onChange={(value) => updateField("emergencyContactName", value)}
                  />
                </div>
                <TextInput
                  label="Số điện thoại"
                  value={formState.emergencyPhone}
                  onChange={(value) => updateField("emergencyPhone", value)}
                />
                <TextInput
                  label="Mối quan hệ"
                  placeholder="Ví dụ: Cha, Mẹ, Vợ/Chồng"
                  value={formState.emergencyRelation}
                  onChange={(value) => updateField("emergencyRelation", value)}
                />
              </div>
            </Card>

            <Card
              title="Thanh toán lương"
              description="Thông tin tài khoản ngân hàng dùng cho chi trả lương."
              actions={<Landmark size={20} className="text-[var(--hicas-orange)]" />}
            >
              <div className="grid gap-4 md:grid-cols-2">
                <TextInput
                  label="Tên ngân hàng"
                  placeholder="Ví dụ: MB Bank"
                  value={formState.bankName}
                  onChange={(value) => updateField("bankName", value)}
                />
                <TextInput
                  label="Số tài khoản"
                  value={formState.bankAccount}
                  onChange={(value) => updateField("bankAccount", value)}
                />
              </div>
            </Card>
          </div>

          <Card
            title="Định danh & bảo hiểm"
            description="Thông tin pháp lý, thuế và bảo hiểm xã hội/y tế."
            actions={<IdCard size={20} className="text-[var(--hicas-orange)]" />}
          >
            <div className="grid gap-4 md:grid-cols-2">
              <TextInput
                label="Số CCCD mới"
                value={formState.identityNumber}
                onChange={(value) => updateField("identityNumber", value)}
              />
              <TextInput
                label="Mã số thuế"
                value={formState.taxCode}
                onChange={(value) => updateField("taxCode", value)}
              />
              <TextInput
                label="Mã số BHXH"
                value={formState.socialInsCode}
                onChange={(value) => updateField("socialInsCode", value)}
              />
              <TextInput
                label="Ngày tham gia BHXH"
                type="date"
                value={formState.socialInsJoinDate}
                onChange={(value) => updateField("socialInsJoinDate", value)}
              />
              <div className="md:col-span-2">
                <TextInput
                  label="Nơi khám chữa bệnh ban đầu"
                  value={formState.insuranceHospital}
                  onChange={(value) => updateField("insuranceHospital", value)}
                />
              </div>
            </div>
          </Card>

          <Card
            title="Tài liệu minh chứng"
            description="Tải lên ảnh và tài liệu minh chứng phù hợp."
            actions={<FileUp size={20} className="text-[var(--hicas-orange)]" />}
          >
            <div className="grid gap-4">
              <FileInput
                label="Ảnh đại diện"
                accept=".jpg,.jpeg,.png"
                onChange={(event) => handleFileChange(event, "avatarFile", true)}
              />
              <div className="grid gap-4 md:grid-cols-2">
                <FileInput
                  label="CCCD mặt trước"
                  accept=".jpg,.jpeg,.png,.pdf"
                  onChange={(event) => handleFileChange(event, "identityFrontFile")}
                />
                <FileInput
                  label="CCCD mặt sau"
                  accept=".jpg,.jpeg,.png,.pdf"
                  onChange={(event) => handleFileChange(event, "identityBackFile")}
                />
              </div>
              <FileInput
                label="Chứng chỉ / bằng cấp"
                accept=".jpg,.jpeg,.png,.pdf"
                onChange={(event) => handleFileChange(event, "certificateFile")}
              />
            </div>
          </Card>
        </div>

        <Card className="p-4" padded={false}>
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex items-center gap-3 text-sm text-[var(--hicas-text-secondary)]">
              <Building2 size={18} className="text-[var(--hicas-orange)]" />
              Dữ liệu chỉ được cập nhật chính thức sau khi HR phê duyệt.
            </div>
            <div className="flex flex-col gap-3 sm:flex-row">
              <Button type="button" variant="secondary" onClick={resetForm} disabled={submitting}>
                Làm mới
              </Button>
              <Button type="submit" iconLeft={<Save size={16} />} isLoading={submitting}>
                Gửi yêu cầu duyệt
              </Button>
            </div>
          </div>
        </Card>
      </form>

      <DependentManagement
        dependents={dependents}
        loading={loadingDependents}
        onRefresh={refreshDependents}
      />
    </div>
  );
};
