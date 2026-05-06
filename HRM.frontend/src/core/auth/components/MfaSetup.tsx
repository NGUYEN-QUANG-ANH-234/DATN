import React, { useState } from "react";
import { QRCodeSVG } from "qrcode.react";
import { authApi } from "../api/authApi";
import type { MfaSetupResponse } from "../types";
import { AxiosError } from "axios";
import { useNavigate } from "react-router-dom";
import { useCurrentUser } from "../hooks/useCurrentUser";

export const MfaSetup: React.FC = () => {
  const navigate = useNavigate();

  // 1. Lấy thông tin user từ JWT (Đã chứa sẵn cờ isMfaEnabled)
  const { user } = useCurrentUser();

  // 2. Các State quản lý luồng UI
  const [step, setStep] = useState<1 | 2 | 3>(1);
  const [qrData, setQrData] = useState<MfaSetupResponse | null>(null);
  const [otpCode, setOtpCode] = useState<string>("");
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);
  const [error, setError] = useState<string>("");

  // NẾU USER ĐÃ BẬT MFA TRONG TOKEN -> TỰ ĐỘNG HIỂN THỊ GIAO DIỆN "ĐÃ KÍCH HOẠT"
  // Khai báo State, lấy giá trị ban đầu từ Token
  const [isAlreadySetup, setIsAlreadySetup] = useState<boolean>(
    (user as { isMfaEnabled?: boolean })?.isMfaEnabled || false,
  );

  // Thêm useEffect để đồng bộ nếu thông tin user thay đổi
  React.useEffect(() => {
    if ((user as { isMfaEnabled?: boolean })?.isMfaEnabled) {
      setIsAlreadySetup(true);
    }
  }, [user]);

  // Bước 1: Gọi API lấy QR Code
  const handleStartSetup = async () => {
    try {
      setError("");
      const response = await authApi.initiateMfaSetup();

      const data = (
        "data" in response ? response.data : response
      ) as MfaSetupResponse;

      if (data.qrCodeUri && data.secretKey) {
        setQrData({
          qrCodeUri: data.qrCodeUri,
          secretKey: data.secretKey,
        });
        setStep(2);
      } else {
        setError("Không nhận được dữ liệu cấu hình MFA từ máy chủ.");
      }
    } catch (err) {
      if (err instanceof AxiosError) {
        // "Quét" mọi định dạng lỗi mà C# có thể trả về (message, Message, detail, hoặc cả cục data)
        const errorData = err.response?.data;
        const errorMessage = String(
          errorData?.message ||
            errorData?.Message ||
            errorData?.detail ||
            errorData ||
            "",
        ).toLowerCase();

        // BẮT LỖI TỪ BACKEND
        if (
          errorMessage.includes("mfa đã được bật") ||
          errorMessage.includes("already enabled")
        ) {
          setIsAlreadySetup(true);
        } else {
          setError(
            errorData?.message ||
              errorData?.detail ||
              "Lỗi khi khởi tạo cấu hình MFA (Mã 500).",
          );
        }
      } else {
        setError("Đã xảy ra lỗi không xác định.");
      }
    }
  };

  // Bước 2: Xác nhận OTP và nhận mã khôi phục
  const handleConfirm = async () => {
    if (otpCode.length < 6) {
      setError("Vui lòng nhập đủ 6 số OTP.");
      return;
    }

    try {
      setError("");
      const response = await authApi.confirmMfaSetup(otpCode);

      const data = ("data" in response ? response.data : response) as {
        recoveryCodes: string[];
      };

      setRecoveryCodes(data.recoveryCodes);
      setStep(3);
    } catch (err) {
      if (err instanceof AxiosError) {
        setError(
          err.response?.data?.message || "Mã OTP không hợp lệ hoặc đã hết hạn.",
        );
      } else {
        setError("Đã xảy ra lỗi khi xác nhận OTP.");
      }
    }
  };

  return (
    <div className="mx-auto max-w-md rounded-xl bg-white p-6 shadow-md border border-gray-100">
      <h2 className="mb-6 text-2xl font-bold text-gray-800 text-center">
        Bảo mật 2 Lớp (MFA)
      </h2>

      {/* RẼ NHÁNH 1: GIAO DIỆN KHI ĐÃ THIẾT LẬP TỪ TRƯỚC */}
      {isAlreadySetup ? (
        <div className="text-center py-4">
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-green-100">
            <svg
              className="h-8 w-8 text-green-600"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth="2"
                d="M5 13l4 4L19 7"
              />
            </svg>
          </div>
          <h3 className="mb-2 text-xl font-bold text-gray-800">Đã kích hoạt</h3>
          <p className="mb-6 text-gray-600">
            Tài khoản của bạn đang được bảo vệ an toàn bởi hệ thống xác thực 2
            lớp. Bạn không cần thiết lập lại.
          </p>
          <button
            onClick={() => navigate("/dashboard")}
            className="w-full rounded-lg bg-gray-100 px-4 py-2.5 font-semibold text-gray-700 transition hover:bg-gray-200"
          >
            Quay về bảng điều khiển
          </button>
        </div>
      ) : (
        /* RẼ NHÁNH 2: LUỒNG CÀI ĐẶT BÌNH THƯỜNG */
        <>
          {error && (
            <div className="mb-4 rounded bg-red-50 p-3 text-sm text-red-600 border border-red-200">
              {error}
            </div>
          )}

          {/* BƯỚC 1: GIỚI THIỆU */}
          {step === 1 && (
            <div className="text-center">
              <p className="mb-6 text-gray-600">
                Tăng cường bảo mật cho tài khoản nội bộ bằng ứng dụng Google
                Authenticator.
              </p>
              <button
                onClick={handleStartSetup}
                className="w-full rounded-lg bg-blue-600 px-4 py-2.5 font-semibold text-white transition hover:bg-blue-700"
              >
                Bắt đầu thiết lập
              </button>
            </div>
          )}

          {/* BƯỚC 2: QUÉT MÃ VÀ NHẬP OTP */}
          {step === 2 && qrData && (
            <div className="flex flex-col items-center">
              <div className="mb-6 w-full text-center">
                <p className="mb-3 font-medium text-gray-700">
                  1. Quét mã QR bằng ứng dụng Authenticator
                </p>
                <div className="mx-auto inline-block rounded-xl border-4 border-gray-100 bg-white p-3 shadow-sm">
                  <QRCodeSVG value={qrData.qrCodeUri} size={180} />
                </div>
                <p className="mt-3 text-sm text-gray-500">
                  Hoặc nhập thủ công:{" "}
                  <span className="font-mono font-bold text-gray-800">
                    {qrData.secretKey}
                  </span>
                </p>
              </div>

              <div className="w-full border-t border-gray-100 pt-5">
                <p className="mb-2 font-medium text-gray-700">
                  2. Nhập mã OTP 6 số để xác nhận:
                </p>
                <input
                  type="text"
                  maxLength={6}
                  value={otpCode}
                  onChange={(e) =>
                    setOtpCode(e.target.value.replace(/\D/g, ""))
                  }
                  className="mb-4 w-full rounded-lg border border-gray-300 p-3 text-center font-mono text-xl tracking-[0.5em] text-gray-800 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                  placeholder="••••••"
                />
                <button
                  onClick={handleConfirm}
                  disabled={otpCode.length !== 6}
                  className="w-full rounded-lg bg-green-600 px-4 py-2.5 font-semibold text-white transition hover:bg-green-700 disabled:bg-gray-400"
                >
                  Xác nhận thiết lập
                </button>
              </div>
            </div>
          )}

          {/* BƯỚC 3: HIỂN THỊ MÃ KHÔI PHỤC */}
          {step === 3 && (
            <div>
              <div className="mb-5 rounded-lg bg-green-50 p-4 text-green-800 border border-green-200 text-center">
                <span className="block font-bold text-lg mb-1">
                  🎉 Thành công!
                </span>
                MFA đã được kích hoạt cho tài khoản của bạn.
              </div>

              <div className="mb-6">
                <p className="mb-2 font-bold text-red-600">
                  LƯU TRỮ MÃ KHÔI PHỤC
                </p>
                <p className="mb-4 text-sm text-gray-600 leading-relaxed">
                  Hãy sao chép và cất giữ an toàn các mã dưới đây. Bạn sẽ cần
                  dùng chúng để đăng nhập nếu bị mất thiết bị. <br />
                  <b>
                    Lưu ý: Mỗi mã chỉ dùng được 1 lần và sẽ không hiển thị lại.
                  </b>
                </p>
                <div className="grid grid-cols-2 gap-3">
                  {recoveryCodes.map((code, index) => (
                    <div
                      key={index}
                      className="rounded bg-gray-50 p-2 text-center font-mono text-sm font-semibold text-gray-800 border border-gray-200"
                    >
                      {code}
                    </div>
                  ))}
                </div>
              </div>

              <button
                onClick={() => navigate("/dashboard")}
                className="w-full rounded-lg bg-blue-600 px-4 py-2.5 font-semibold text-white transition hover:bg-blue-700"
              >
                Quay về bảng điều khiển
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
};
