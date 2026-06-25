import React, { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { jwtDecode } from "jwt-decode";
import { onboardingApi } from "../api/onboardingApi";
import type {
  OnboardingCandidateLookup,
  SubmitOnboardingFormState,
} from "../types/onboarding";
import {
  FeatureCard,
  FeaturePage,
  fieldClass,
  primaryButtonClass,
} from "../../../core/components/FeatureShell";
import { useNotification } from "../../../core/context/NotificationContext";
import type { JwtPayload } from "../../../core/auth/types";

const MAX_FILE_SIZE = 5 * 1024 * 1024;
const ALLOWED_FILE_TYPES = ["image/jpeg", "image/png", "application/pdf"];
const EMAIL_CLAIM = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";

const getDefaultAccountEmail = () => {
  const token = localStorage.getItem("accessToken");
  if (token) {
    try {
      const decoded = jwtDecode<JwtPayload>(token);
      const tokenEmail = decoded.email || (decoded[EMAIL_CLAIM] as string | undefined);
      if (tokenEmail?.trim()) return tokenEmail.trim();
    } catch {
      // Fallback to the email stored after candidate lookup/application.
    }
  }

  return localStorage.getItem("candidate_email")?.trim() || "";
};

const initialState = (
  candidate?: OnboardingCandidateLookup | null,
): SubmitOnboardingFormState => ({
  candidateId: candidate?.candidateId ?? 0,
  trackingCode: candidate?.trackingCode ?? "",
  fullName: candidate?.fullName ?? "",
  email: candidate?.email ?? "",
  phoneNumber: "",
  personalEmail: candidate?.email ?? "",
  currentAddress: "",
  permanentAddress: "",
  identityNumber: "",
  nationality: "Việt Nam",
  ethnicity: "Kinh",
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

const getErrorMessage = (error: unknown, fallback: string) => {
  if (typeof error === "object" && error !== null && "message" in error) {
    const message = (error as { message?: string }).message;
    if (message) return message;
  }
  return fallback;
};

export const OnboardingForm: React.FC<{ candidateId?: number }> = () => {
  const [searchParams] = useSearchParams();
  const { triggerAlert } = useNotification();

  const defaultEmail =
    searchParams.get("email")?.trim() ||
    getDefaultAccountEmail();
  const defaultTrackingCode =
    searchParams.get("trackingCode")?.trim() ||
    localStorage.getItem("candidate_trackingCode")?.trim() ||
    "";

  const [lookupEmail, setLookupEmail] = useState(defaultEmail);
  const [lookupTrackingCode, setLookupTrackingCode] = useState(defaultTrackingCode);
  const [resolvedCandidate, setResolvedCandidate] =
    useState<OnboardingCandidateLookup | null>(null);
  const [formState, setFormState] = useState<SubmitOnboardingFormState>(
    initialState(null),
  );
  const [resolving, setResolving] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const resolveCandidate = async (showSuccess = true) => {
    const email = lookupEmail.trim();
    const trackingCode = lookupTrackingCode.trim();

    if (!email || !trackingCode) {
      triggerAlert(
        "warning",
        "Thiếu thông tin tra cứu",
        "Vui lòng nhập email ứng tuyển và mã hồ sơ được gửi qua email.",
      );
      return;
    }

    setResolving(true);
    try {
      const res = await onboardingApi.resolveCandidate({ email, trackingCode });
      const candidate = res.data;
      setResolvedCandidate(candidate);
      setFormState(initialState(candidate));
      localStorage.setItem("candidate_email", candidate.email);
      localStorage.setItem("candidate_trackingCode", candidate.trackingCode);
      localStorage.removeItem("candidate_id");

      if (showSuccess) {
        triggerAlert(
          "success",
          "Đã mở hồ sơ",
          "Bạn có thể hoàn thiện thông tin tiếp nhận.",
        );
      }
    } catch (error: unknown) {
      setResolvedCandidate(null);
      setFormState(initialState(null));
      triggerAlert(
        "error",
        "Không mở được hồ sơ",
        getErrorMessage(error, "Không tìm thấy hồ sơ phù hợp với email và mã hồ sơ."),
      );
    } finally {
      setResolving(false);
    }
  };

  useEffect(() => {
    if (defaultEmail && defaultTrackingCode) {
      void resolveCandidate(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

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

    if (!resolvedCandidate) {
      triggerAlert(
        "warning",
        "Chưa mở hồ sơ",
        "Vui lòng tra cứu hồ sơ bằng email và mã hồ sơ trước khi gửi.",
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
    Object.entries({
      ...formState,
      candidateId: resolvedCandidate.candidateId,
      trackingCode: resolvedCandidate.trackingCode,
      email: resolvedCandidate.email,
      personalEmail: resolvedCandidate.email,
    }).forEach(([key, value]) => {
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
        res.message || "Hồ sơ đã được gửi đến HR để kiểm tra.",
      );
      setFormState(initialState(resolvedCandidate));
    } catch (error: unknown) {
      triggerAlert(
        "error",
        "Không gửi được hồ sơ",
        getErrorMessage(error, "Vui lòng kiểm tra lại thông tin và thử lại."),
      );
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <FeaturePage
      title="Thiết lập hồ sơ"
      description="Hoàn thiện thông tin tiếp nhận sau khi hồ sơ ứng tuyển được duyệt."
      width="normal"
    >
      {!resolvedCandidate ? (
        <FeatureCard title="Mở hồ sơ tiếp nhận">
          <div className="space-y-5 text-sm leading-6 text-[var(--hicas-text-secondary)]">
            <p>
              Nhập email ứng tuyển và mã hồ sơ trong email thông báo để mở form hoàn thiện hồ sơ.
              Nếu HR gửi link trực tiếp, hệ thống sẽ tự điền hai thông tin này.
            </p>

            <div className="grid grid-cols-1 gap-4 rounded-[var(--radius-md)] border border-[var(--hicas-border-soft)] bg-[var(--hicas-bg-soft)] p-4 md:grid-cols-2">
              <label className="block">
                <span className="mb-2 block text-sm font-semibold text-[var(--hicas-text-main)]">
                  Email ứng tuyển
                </span>
                <input
                  type="email"
                  value={lookupEmail}
                  onChange={(event) => setLookupEmail(event.target.value)}
                  className={fieldClass}
                  placeholder="vidu@email.com"
                />
              </label>

              <label className="block">
                <span className="mb-2 block text-sm font-semibold text-[var(--hicas-text-main)]">
                  Mã hồ sơ
                </span>
                <input
                  value={lookupTrackingCode}
                  onChange={(event) => setLookupTrackingCode(event.target.value.toUpperCase())}
                  className={fieldClass}
                  placeholder="VD: HICAS-2026-0001"
                />
              </label>
            </div>

            <div className="flex flex-wrap gap-3">
              <button
                type="button"
                onClick={() => void resolveCandidate()}
                disabled={resolving}
                className={primaryButtonClass}
              >
                {resolving ? "Đang kiểm tra..." : "Mở hồ sơ"}
              </button>
              <Link
                to="/recruitment/history"
                className="hicas-btn-secondary inline-flex min-h-[42px] items-center justify-center gap-2 px-[18px] text-sm"
              >
                Tra cứu hồ sơ ứng tuyển
              </Link>
              <Link
                to="/recruitment/jobs"
                className="hicas-btn-secondary inline-flex min-h-[42px] items-center justify-center gap-2 px-[18px] text-sm"
              >
                Xem vị trí tuyển dụng
              </Link>
            </div>
          </div>
        </FeatureCard>
      ) : (
        <FeatureCard>
          <div className="mb-6 rounded-[var(--radius-md)] border border-[var(--hicas-border-soft)] bg-[var(--hicas-bg-soft)] p-4">
            <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
              <div>
                <p className="text-xs font-semibold uppercase tracking-wide text-[var(--hicas-text-muted)]">
                  Ứng viên
                </p>
                <p className="mt-1 font-semibold text-[var(--hicas-text-main)]">
                  {resolvedCandidate.fullName}
                </p>
              </div>
              <div>
                <p className="text-xs font-semibold uppercase tracking-wide text-[var(--hicas-text-muted)]">
                  Email
                </p>
                <p className="mt-1 font-semibold text-[var(--hicas-text-main)]">
                  {resolvedCandidate.email}
                </p>
              </div>
              <div>
                <p className="text-xs font-semibold uppercase tracking-wide text-[var(--hicas-text-muted)]">
                  Mã hồ sơ
                </p>
                <p className="mt-1 font-semibold text-[var(--hicas-text-main)]">
                  {resolvedCandidate.trackingCode}
                </p>
              </div>
              {(resolvedCandidate.departmentName || resolvedCandidate.positionName) && (
                <div className="md:col-span-3">
                  <p className="text-xs font-semibold uppercase tracking-wide text-[var(--hicas-text-muted)]">
                    Dự kiến tiếp nhận
                  </p>
                  <p className="mt-1 font-semibold text-[var(--hicas-text-main)]">
                    {[resolvedCandidate.departmentName, resolvedCandidate.positionName]
                      .filter(Boolean)
                      .join(" - ")}
                  </p>
                </div>
              )}
            </div>
          </div>

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
                  Email hồ sơ
                </label>
                <input
                  type="email"
                  readOnly
                  className={`${fieldClass} bg-[var(--hicas-bg-soft)] font-semibold`}
                  value={resolvedCandidate.email}
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
                  Quốc tịch
                </label>
                <input
                  className={fieldClass}
                  value={formState.nationality}
                  onChange={(e) => handleInput(e, "nationality")}
                />
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium text-gray-700">
                  Dân tộc
                </label>
                <input
                  className={fieldClass}
                  value={formState.ethnicity}
                  onChange={(e) => handleInput(e, "ethnicity")}
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
                  Địa chỉ hiện tại *
                </label>
                <input
                  required
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
              {submitting ? "Đang gửi..." : "Gửi hồ sơ"}
            </button>
          </form>
        </FeatureCard>
      )}
    </FeaturePage>
  );
};
