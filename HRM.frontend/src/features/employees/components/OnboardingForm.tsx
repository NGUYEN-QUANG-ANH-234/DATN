import React, { useState } from "react";
import { useParams, useSearchParams } from "react-router-dom";
import { onboardingApi } from "../api/onboardingApi";
import type { SubmitOnboardingFormState } from "../types/onboarding";
import {
  FeatureCard,
  FeaturePage,
  fieldClass,
  primaryButtonClass,
} from "../../../core/components/FeatureShell";
import { useNotification } from "../../../core/context/NotificationContext";

const MAX_FILE_SIZE = 5 * 1024 * 1024;
const ALLOWED_FILE_TYPES = ["image/jpeg", "image/png", "application/pdf"];

const initialState = (candidateId: number): SubmitOnboardingFormState => ({
  candidateId,
  fullName: "",
  email: "",
  phoneNumber: "",
  personalEmail: "",
  currentAddress: "",
  permanentAddress: "",
  identityNumber: "",
  emergencyContactName: "",
  emergencyPhone: "",
  emergencyRelation: "",
  gender: "",
  birthDate: "",
  taxCode: "",
  socialInsCode: "",
  bankAccount: "",
  bankName: "",
  identityFrontFile: null,
  identityBackFile: null,
  certificateFile: null,
});

export const OnboardingForm: React.FC<{ candidateId?: number }> = ({
  candidateId: candidateIdProp,
}) => {
  const params = useParams();
  const [searchParams] = useSearchParams();
  const candidateId =
    candidateIdProp ??
    Number(params.candidateId || searchParams.get("candidateId") || 0);

  const [submitting, setSubmitting] = useState(false);
  const { triggerAlert } = useNotification();
  const [formState, setFormState] = useState<SubmitOnboardingFormState>(
    initialState(candidateId),
  );

  const handleInput = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>,
    field: keyof SubmitOnboardingFormState,
  ) => {
    setFormState((prev) => ({ ...prev, [field]: e.target.value }));
  };

  const handleFileChange = (
    e: React.ChangeEvent<HTMLInputElement>,
    field: keyof SubmitOnboardingFormState,
  ) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (file.size > MAX_FILE_SIZE) {
      triggerAlert("warning", "File quá lớn", "Dung lượng file vượt quá 5MB.");
      e.target.value = "";
      return;
    }

    if (!ALLOWED_FILE_TYPES.includes(file.type)) {
      triggerAlert("warning", "File không hợp lệ", "Chỉ chấp nhận JPG, PNG hoặc PDF.");
      e.target.value = "";
      return;
    }

    setFormState((prev) => ({ ...prev, [field]: file }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!candidateId) {
      triggerAlert(
        "warning",
        "Thiếu mã ứng viên",
        "Vui lòng mở onboarding từ liên kết hợp lệ.",
      );
      return;
    }

    if (!formState.identityFrontFile || !formState.identityBackFile) {
      triggerAlert(
        "warning",
        "Thiếu giấy tờ",
        "Vui lòng tải lên CCCD mặt trước và mặt sau.",
      );
      return;
    }

    const formData = new FormData();
    Object.entries({ ...formState, candidateId }).forEach(([key, value]) => {
      if (value !== null && value !== "") {
        const formattedKey = key.charAt(0).toUpperCase() + key.slice(1);
        formData.append(
          formattedKey,
          value instanceof Blob ? value : String(value),
        );
      }
    });

    setSubmitting(true);
    try {
      const res = await onboardingApi.submitProfile(formData);
      triggerAlert(
        "success",
        "Đã gửi hồ sơ",
        res.message || "Đã gửi hồ sơ thành công!",
      );
      setFormState(initialState(candidateId));
    } catch (error: unknown) {
      const err = error as { message?: string };
      triggerAlert("error", "Lỗi", err.message || "Lỗi khi gửi hồ sơ.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <FeaturePage
      title="Onboarding nhân sự mới"
      description="Hoàn thiện hồ sơ điện tử để HR khởi tạo tài khoản nội bộ."
      width="normal"
    >
      <FeatureCard>
        <form onSubmit={handleSubmit} className="space-y-6">
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">
                Họ và tên *
              </label>
              <input
                required
                className={fieldClass}
                value={formState.fullName}
                onChange={(e) => handleInput(e, "fullName")}
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">
                Email liên hệ *
              </label>
              <input
                type="email"
                required
                className={fieldClass}
                value={formState.email}
                onChange={(e) => handleInput(e, "email")}
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">
                Giới tính
              </label>
              <select
                className={fieldClass}
                value={formState.gender}
                onChange={(e) => handleInput(e, "gender")}
              >
                <option value="">Chọn giới tính</option>
                <option value="0">Nam</option>
                <option value="1">Nữ</option>
                <option value="2">Khác</option>
              </select>
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">
                Ngày sinh
              </label>
              <input
                type="date"
                className={fieldClass}
                value={formState.birthDate}
                onChange={(e) => handleInput(e, "birthDate")}
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">
                Số điện thoại *
              </label>
              <input
                required
                className={fieldClass}
                value={formState.phoneNumber}
                onChange={(e) => handleInput(e, "phoneNumber")}
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">
                Số CCCD *
              </label>
              <input
                required
                className={fieldClass}
                value={formState.identityNumber}
                onChange={(e) => handleInput(e, "identityNumber")}
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">
                Địa chỉ hiện tại
              </label>
              <input
                className={fieldClass}
                value={formState.currentAddress}
                onChange={(e) => handleInput(e, "currentAddress")}
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">
                Địa chỉ thường trú *
              </label>
              <input
                required
                className={fieldClass}
                value={formState.permanentAddress}
                onChange={(e) => handleInput(e, "permanentAddress")}
              />
            </div>
          </div>

          <div className="grid grid-cols-1 gap-4 rounded-lg border border-gray-200 bg-gray-50 p-4 md:grid-cols-2">
            <input
              className={fieldClass}
              placeholder="Tên ngân hàng"
              value={formState.bankName}
              onChange={(e) => handleInput(e, "bankName")}
            />
            <input
              className={fieldClass}
              placeholder="Số tài khoản"
              value={formState.bankAccount}
              onChange={(e) => handleInput(e, "bankAccount")}
            />
            <input
              className={fieldClass}
              placeholder="Mã số thuế"
              value={formState.taxCode}
              onChange={(e) => handleInput(e, "taxCode")}
            />
            <input
              className={fieldClass}
              placeholder="Mã số BHXH"
              value={formState.socialInsCode}
              onChange={(e) => handleInput(e, "socialInsCode")}
            />
          </div>

          <div className="rounded-lg border border-red-100 bg-red-50 p-4">
            <h3 className="mb-3 font-semibold text-red-700">
              Liên hệ khẩn cấp
            </h3>
            <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
              <input
                required
                className={fieldClass}
                placeholder="Họ tên người thân"
                value={formState.emergencyContactName}
                onChange={(e) => handleInput(e, "emergencyContactName")}
              />
              <input
                required
                className={fieldClass}
                placeholder="Số điện thoại"
                value={formState.emergencyPhone}
                onChange={(e) => handleInput(e, "emergencyPhone")}
              />
              <input
                required
                className={fieldClass}
                placeholder="Mối quan hệ"
                value={formState.emergencyRelation}
                onChange={(e) => handleInput(e, "emergencyRelation")}
              />
            </div>
          </div>

          <div className="grid grid-cols-1 gap-4 rounded-lg border border-blue-100 bg-blue-50 p-4 md:grid-cols-2">
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">
                CCCD mặt trước *
              </label>
              <input
                type="file"
                required
                accept=".jpg,.jpeg,.png,.pdf"
                className={fieldClass}
                onChange={(e) => handleFileChange(e, "identityFrontFile")}
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">
                CCCD mặt sau *
              </label>
              <input
                type="file"
                required
                accept=".jpg,.jpeg,.png,.pdf"
                className={fieldClass}
                onChange={(e) => handleFileChange(e, "identityBackFile")}
              />
            </div>
          </div>

          <button
            type="submit"
            disabled={submitting}
            className={`w-full ${primaryButtonClass}`}
          >
            {submitting ? "Đang xử lý..." : "Gửi hồ sơ onboarding"}
          </button>
        </form>
      </FeatureCard>
    </FeaturePage>
  );
};
