import React, { useState } from "react";
import axiosClient from "../../../core/api/axiosClient";
import type { ProfileUpdateFormState } from "../types/profile";
import { useNotification } from "../../../core/context/NotificationContext";

const MAX_FILE_SIZE = 5 * 1024 * 1024;
const ALLOWED_TYPES = ["image/jpeg", "image/png", "application/pdf"];
const ALLOWED_IMAGE_TYPES = ["image/jpeg", "image/png"];

export const ProfileUpdateForm: React.FC = () => {
  const [submitting, setSubmitting] = useState(false);
  const { triggerAlert } = useNotification();

  const [formState, setFormState] = useState<ProfileUpdateFormState>({
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
  });

  const handleFileChange = (
    e: React.ChangeEvent<HTMLInputElement>,
    fieldName: keyof ProfileUpdateFormState,
    onlyImage = false,
  ) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (file.size > MAX_FILE_SIZE) {
      triggerAlert("warning", "File quá lớn", "Dung lượng file vượt quá 5MB.");
      e.target.value = "";
      return;
    }
    const typesToCheck = onlyImage ? ALLOWED_IMAGE_TYPES : ALLOWED_TYPES;
    if (!typesToCheck.includes(file.type)) {
      triggerAlert(
        "warning",
        "File không hợp lệ",
        onlyImage
          ? "Chỉ chấp nhận ảnh JPG, PNG."
          : "Chỉ chấp nhận định dạng JPG, PNG hoặc PDF.",
      );
      e.target.value = "";
      return;
    }

    setFormState((prev) => ({ ...prev, [fieldName]: file }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const formData = new FormData();

    // Text Data mapping to C# DTO EXACTLY
    if (formState.fullName) formData.append("FullName", formState.fullName);
    if (formState.gender) formData.append("Gender", formState.gender);
    if (formState.birthDate) formData.append("BirthDate", formState.birthDate);

    if (formState.phoneNumber)
      formData.append("PhoneNumber", formState.phoneNumber);
    if (formState.personalEmail)
      formData.append("PersonalEmail", formState.personalEmail);
    if (formState.currentAddress)
      formData.append("CurrentAddress", formState.currentAddress);
    if (formState.permanentAddress)
      formData.append("PermanentAddress", formState.permanentAddress);

    if (formState.identityNumber)
      formData.append("IdentityNumber", formState.identityNumber);
    if (formState.taxCode) formData.append("TaxCode", formState.taxCode);
    if (formState.socialInsCode)
      formData.append("SocialInsCode", formState.socialInsCode);
    if (formState.socialInsJoinDate)
      formData.append("SocialInsJoinDate", formState.socialInsJoinDate);
    if (formState.insuranceHospital)
      formData.append("InsuranceHospital", formState.insuranceHospital);

    if (formState.bankAccount)
      formData.append("BankAccount", formState.bankAccount);
    if (formState.bankName) formData.append("BankName", formState.bankName);

    if (formState.emergencyContactName)
      formData.append("EmergencyContactName", formState.emergencyContactName);
    if (formState.emergencyPhone)
      formData.append("EmergencyPhone", formState.emergencyPhone);
    if (formState.emergencyRelation)
      formData.append("EmergencyRelation", formState.emergencyRelation);

    // File Data
    if (formState.avatarFile)
      formData.append("AvatarFile", formState.avatarFile);
    if (formState.identityFrontFile)
      formData.append("IdentityFrontFile", formState.identityFrontFile);
    if (formState.identityBackFile)
      formData.append("IdentityBackFile", formState.identityBackFile);
    if (formState.certificateFile)
      formData.append("CertificateFile", formState.certificateFile);

    let hasData = false;
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
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
      const response: unknown = await axiosClient.patch(
        "/employees/profile",
        formData,
        {
          headers: {
            "Content-Type": "multipart/form-data",
          },
        },
      );

      const resObj = response as {
        data?: { message?: string; Message?: string };
        message?: string;
        Message?: string;
      };

      const responseData = resObj?.data !== undefined ? resObj.data : resObj;
      const msg =
        responseData?.message ||
        responseData?.Message ||
        "Đã gửi yêu cầu cập nhật thành công!";

      triggerAlert("success", "Đã gửi yêu cầu", msg);
      // Reset form
      setFormState({
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
      });
      document.querySelectorAll("input[type=file]").forEach((el: Element) => {
        (el as HTMLInputElement).value = "";
      });
    } catch (error: unknown) {
      // Bắt lỗi với unknown
      console.error("Lỗi giao diện/API:", error);

      // Ép kiểu an toàn cho error
      const errObj = error as {
        response?: {
          data?: { message?: string; Message?: string };
          message?: string;
          Message?: string;
        };
      };

      const errData = errObj.response?.data || errObj.response || {};
      const errMsg =
        errData?.message ||
        errData?.Message ||
        "Đã xảy ra lỗi khi cập nhật hồ sơ.";

      triggerAlert("error", "Lỗi", errMsg);
    } finally {
      setSubmitting(false);
    }
  };

  const handleInput = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>,
    field: keyof ProfileUpdateFormState,
  ) => {
    setFormState({ ...formState, [field]: e.target.value });
  };

  return (
    <div className="min-h-full bg-gray-50 px-4 py-6 sm:px-6">
      <div className="mx-auto w-full max-w-6xl rounded-lg border border-gray-200 bg-white p-5 shadow-sm sm:p-6">
        <h2 className="text-2xl font-bold text-gray-800 mb-2">
          Cập nhật Hồ sơ cá nhân
        </h2>
        <p className="text-gray-500 text-sm mb-6">
          Chỉ nhập những thông tin bạn muốn thay đổi. Bỏ trống nếu không đổi.
        </p>

        <form onSubmit={handleSubmit} className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* Nhóm 1: Cơ bản & Liên hệ */}
            <div className="bg-white p-4 rounded border border-gray-200 shadow-sm space-y-3">
              <h3 className="font-semibold text-gray-700 border-b pb-2 mb-2">
                👤 Cá nhân & Liên hệ
              </h3>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs mb-1">Họ tên mới</label>
                  <input
                    className="w-full border p-2 text-sm rounded"
                    value={formState.fullName}
                    onChange={(e) => handleInput(e, "fullName")}
                  />
                </div>
                <div>
                  <label className="block text-xs mb-1">Giới tính</label>
                  <select
                    className="w-full border p-2 text-sm rounded bg-white"
                    value={formState.gender}
                    onChange={(e) => handleInput(e, "gender")}
                  >
                    <option value="">- Bỏ qua -</option>
                    <option value="0">Nam</option>
                    <option value="1">Nữ</option>
                    <option value="2">Khác</option>
                  </select>
                </div>
                <div>
                  <label className="block text-xs mb-1">Ngày sinh</label>
                  <input
                    type="date"
                    className="w-full border p-2 text-sm rounded"
                    value={formState.birthDate}
                    onChange={(e) => handleInput(e, "birthDate")}
                  />
                </div>
                <div>
                  <label className="block text-xs mb-1">Số điện thoại</label>
                  <input
                    type="text"
                    className="w-full border p-2 text-sm rounded"
                    value={formState.phoneNumber}
                    onChange={(e) => handleInput(e, "phoneNumber")}
                  />
                </div>
                <div className="col-span-2">
                  <label className="block text-xs mb-1">Email cá nhân</label>
                  <input
                    type="email"
                    className="w-full border p-2 text-sm rounded"
                    value={formState.personalEmail}
                    onChange={(e) => handleInput(e, "personalEmail")}
                  />
                </div>
                <div className="col-span-2">
                  <label className="block text-xs mb-1">Chỗ ở hiện tại</label>
                  <input
                    type="text"
                    className="w-full border p-2 text-sm rounded"
                    value={formState.currentAddress}
                    onChange={(e) => handleInput(e, "currentAddress")}
                  />
                </div>
                <div className="col-span-2">
                  <label className="block text-xs mb-1">
                    Địa chỉ thường trú
                  </label>
                  <input
                    type="text"
                    className="w-full border p-2 text-sm rounded"
                    value={formState.permanentAddress}
                    onChange={(e) => handleInput(e, "permanentAddress")}
                  />
                </div>
              </div>
            </div>

            {/* Nhóm 2: Khẩn cấp & Ngân hàng */}
            <div className="space-y-6">
              <div className="bg-white p-4 rounded border border-gray-200 shadow-sm space-y-3">
                <h3 className="font-semibold text-gray-700 border-b pb-2 mb-2">
                  🚨 Liên hệ khẩn cấp
                </h3>
                <div className="grid grid-cols-2 gap-3">
                  <div className="col-span-2">
                    <label className="block text-xs mb-1">
                      Họ tên người liên hệ
                    </label>
                    <input
                      className="w-full border p-2 text-sm rounded"
                      value={formState.emergencyContactName}
                      onChange={(e) => handleInput(e, "emergencyContactName")}
                    />
                  </div>
                  <div>
                    <label className="block text-xs mb-1">
                      SĐT người liên hệ
                    </label>
                    <input
                      className="w-full border p-2 text-sm rounded"
                      value={formState.emergencyPhone}
                      onChange={(e) => handleInput(e, "emergencyPhone")}
                    />
                  </div>
                  <div>
                    <label className="block text-xs mb-1">
                      Mối quan hệ (Cha, Mẹ...)
                    </label>
                    <input
                      className="w-full border p-2 text-sm rounded"
                      value={formState.emergencyRelation}
                      onChange={(e) => handleInput(e, "emergencyRelation")}
                    />
                  </div>
                </div>
              </div>

              <div className="bg-white p-4 rounded border border-gray-200 shadow-sm space-y-3">
                <h3 className="font-semibold text-gray-700 border-b pb-2 mb-2">
                  🏦 Thanh toán Lương
                </h3>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs mb-1">Tên ngân hàng</label>
                    <input
                      className="w-full border p-2 text-sm rounded"
                      placeholder="VD: MB Bank"
                      value={formState.bankName}
                      onChange={(e) => handleInput(e, "bankName")}
                    />
                  </div>
                  <div>
                    <label className="block text-xs mb-1">Số tài khoản</label>
                    <input
                      className="w-full border p-2 text-sm rounded"
                      value={formState.bankAccount}
                      onChange={(e) => handleInput(e, "bankAccount")}
                    />
                  </div>
                </div>
              </div>
            </div>

            {/* Nhóm 3: Pháp lý & BHXH */}
            <div className="bg-white p-4 rounded border border-gray-200 shadow-sm space-y-3">
              <h3 className="font-semibold text-gray-700 border-b pb-2 mb-2">
                🪪 Định danh & Bảo hiểm
              </h3>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs mb-1">Số CCCD mới</label>
                  <input
                    className="w-full border p-2 text-sm rounded"
                    value={formState.identityNumber}
                    onChange={(e) => handleInput(e, "identityNumber")}
                  />
                </div>
                <div>
                  <label className="block text-xs mb-1">Mã số thuế</label>
                  <input
                    className="w-full border p-2 text-sm rounded"
                    value={formState.taxCode}
                    onChange={(e) => handleInput(e, "taxCode")}
                  />
                </div>
                <div>
                  <label className="block text-xs mb-1">Mã số BHXH</label>
                  <input
                    className="w-full border p-2 text-sm rounded"
                    value={formState.socialInsCode}
                    onChange={(e) => handleInput(e, "socialInsCode")}
                  />
                </div>
                <div>
                  <label className="block text-xs mb-1">
                    Ngày tham gia BHXH
                  </label>
                  <input
                    type="date"
                    className="w-full border p-2 text-sm rounded"
                    value={formState.socialInsJoinDate}
                    onChange={(e) => handleInput(e, "socialInsJoinDate")}
                  />
                </div>
                <div className="col-span-2">
                  <label className="block text-xs mb-1">
                    Nơi khám chữa bệnh ban đầu (BHYT)
                  </label>
                  <input
                    className="w-full border p-2 text-sm rounded"
                    value={formState.insuranceHospital}
                    onChange={(e) => handleInput(e, "insuranceHospital")}
                  />
                </div>
              </div>
            </div>

            {/* Nhóm 4: Files */}
            <div className="bg-blue-50/50 p-4 rounded border border-blue-100 shadow-sm space-y-3">
              <h3 className="font-semibold text-gray-700 border-b border-blue-200 pb-2 mb-2">
                📎 Tải lên Minh chứng (Nếu có)
              </h3>
              <div className="grid grid-cols-1 gap-3">
                <div>
                  <label className="block text-xs mb-1">
                    Ảnh đại diện (Avatar)
                  </label>
                  <input
                    type="file"
                    accept=".jpg,.png"
                    className="text-xs bg-white p-1 border rounded w-full"
                    onChange={(e) => handleFileChange(e, "avatarFile", true)}
                  />
                </div>
                <div className="flex gap-2">
                  <div className="flex-1">
                    <label className="block text-xs mb-1">CCCD Mặt trước</label>
                    <input
                      type="file"
                      accept=".jpg,.png,.pdf"
                      className="text-xs bg-white p-1 border rounded w-full"
                      onChange={(e) => handleFileChange(e, "identityFrontFile")}
                    />
                  </div>
                  <div className="flex-1">
                    <label className="block text-xs mb-1">CCCD Mặt sau</label>
                    <input
                      type="file"
                      accept=".jpg,.png,.pdf"
                      className="text-xs bg-white p-1 border rounded w-full"
                      onChange={(e) => handleFileChange(e, "identityBackFile")}
                    />
                  </div>
                </div>
                <div>
                  <label className="block text-xs mb-1">
                    Chứng chỉ / Bằng cấp
                  </label>
                  <input
                    type="file"
                    accept=".jpg,.png,.pdf"
                    className="text-xs bg-white p-1 border rounded w-full"
                    onChange={(e) => handleFileChange(e, "certificateFile")}
                  />
                </div>
              </div>
            </div>
          </div>

          <div className="flex justify-end pt-4 border-t">
            <button
              type="submit"
              disabled={submitting}
              className="px-8 py-2.5 bg-blue-600 text-white rounded font-medium shadow hover:bg-blue-700 disabled:bg-blue-400 transition-colors"
            >
              {submitting ? "Đang gửi..." : "Gửi yêu cầu duyệt"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
