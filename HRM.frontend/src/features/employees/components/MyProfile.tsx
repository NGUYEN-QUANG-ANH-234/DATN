import React from "react";
import { useMyProfileData } from "../hooks/useMyProfileData";
import { BACKEND_URL } from "../../../core/api/config";

export const MyProfile: React.FC = () => {
  const { profile, dependents, loadingDependents, loadingProfile } = useMyProfileData({
    includeContracts: false,
  });

  if (loadingProfile)
    return (
      <div className="p-8 text-center text-gray-500 animate-pulse">
        Đang tải hồ sơ...
      </div>
    );
  if (!profile)
    return (
      <div className="p-8 text-center text-gray-400">
        Không tìm thấy thông tin hồ sơ của bạn.
      </div>
    );

  const getGenderText = (g: number | null) =>
    g === 0 ? "Nam" : g === 1 ? "Nữ" : "Khác";

  const getDependentRelationText = (value: number) =>
    ["Con", "Cha/Mẹ", "Vợ/Chồng", "Khác"][value] ?? "Khác";

  return (
    <div className="min-h-full bg-gray-50 px-4 py-6 sm:px-6">
      <div className="mx-auto w-full max-w-6xl space-y-6 rounded-lg border border-gray-200 bg-white p-5 shadow-sm sm:p-6">
        {/* Header Profile */}
        <div className="flex items-center gap-5 pb-6 border-b">
          <img
            src={
              profile.avatarUrl
                ? `${BACKEND_URL}${profile.avatarUrl}`
                : "https://via.placeholder.com/150"
            }
            alt="Avatar"
            className="w-24 h-24 rounded-full object-cover border-2 border-blue-500 shadow-sm"
          />
          <div>
            <h2 className="text-2xl font-bold text-gray-800">
              {profile.fullName}
            </h2>
            <p className="text-sm font-semibold text-blue-600 mt-1">
              Mã NV: {profile.employeeCode}
            </p>
            <span className="inline-block bg-green-50 text-green-700 text-xs px-2.5 py-1 rounded-full font-medium mt-2">
              Trạng thái: {profile.status}
            </span>
          </div>
        </div>

        {/* Nội dung chi tiết - Dùng Grid 3 cột cho gọn */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {/* Cột 1: Cơ bản & Liên hệ */}
          <div className="space-y-4">
            <div>
              <h3 className="font-bold text-gray-700 text-sm border-l-4 border-blue-500 pl-2 uppercase mb-2">
                Cá nhân & Liên hệ
              </h3>
              <p className="text-sm text-gray-600 mb-1">
                Giới tính:{" "}
                <span className="font-semibold text-gray-800">
                  {getGenderText(profile.gender)}
                </span>
              </p>
              <p className="text-sm text-gray-600 mb-1">
                Ngày sinh:{" "}
                <span className="font-semibold text-gray-800">
                  {profile.birthDate
                    ? new Date(profile.birthDate).toLocaleDateString("vi-VN")
                    : "Chưa cập nhật"}
                </span>
              </p>
              <p className="text-sm text-gray-600 mb-1">
                SĐT:{" "}
                <span className="font-semibold text-gray-800">
                  {profile.phoneNumber || "Chưa cập nhật"}
                </span>
              </p>
              <p className="text-sm text-gray-600 mb-1">
                Email cá nhân:{" "}
                <span className="font-semibold text-gray-800">
                  {profile.personalEmail || "Chưa cập nhật"}
                </span>
              </p>
              <p className="text-sm text-gray-600 mb-1">
                Chỗ ở hiện tại:{" "}
                <span className="font-semibold text-gray-800">
                  {profile.currentAddress || "Chưa cập nhật"}
                </span>
              </p>
              <p className="text-sm text-gray-600 mb-1">
                Thường trú:{" "}
                <span className="font-semibold text-gray-800">
                  {profile.permanentAddress || "Chưa cập nhật"}
                </span>
              </p>
            </div>

            <div>
              <h3 className="font-bold text-gray-700 text-sm border-l-4 border-red-500 pl-2 uppercase mb-2">
                Liên hệ khẩn cấp
              </h3>
              <p className="text-sm text-gray-600 mb-1">
                Người liên hệ:{" "}
                <span className="font-semibold text-gray-800">
                  {profile.emergencyContactName || "Chưa cập nhật"}
                </span>
              </p>
              <p className="text-sm text-gray-600 mb-1">
                SĐT khẩn cấp:{" "}
                <span className="font-semibold text-gray-800">
                  {profile.emergencyPhone || "Chưa cập nhật"}
                </span>
              </p>
              <p className="text-sm text-gray-600 mb-1">
                Quan hệ:{" "}
                <span className="font-semibold text-gray-800">
                  {profile.emergencyRelation || "Chưa cập nhật"}
                </span>
              </p>
            </div>
          </div>

          {/* Cột 2: Định danh & Thuế & BHXH */}
          <div className="space-y-4">
            <div>
              <h3 className="font-bold text-gray-700 text-sm border-l-4 border-indigo-500 pl-2 uppercase mb-2">
                Hồ sơ pháp lý
              </h3>
              <p className="text-sm text-gray-600 mb-1">
                Số CCCD:{" "}
                <span className="font-semibold text-gray-800">
                  {profile.identityNumber || "Chưa cập nhật"}
                </span>
              </p>
              <p className="text-sm text-gray-600 mb-1">
                Mã số thuế:{" "}
                <span className="font-semibold text-gray-800">
                  {profile.taxCode || "Chưa có"}
                </span>
              </p>
            </div>

            <div>
              <h3 className="font-bold text-gray-700 text-sm border-l-4 border-green-500 pl-2 uppercase mb-2">
                Bảo hiểm Y tế / Xã hội
              </h3>
              <p className="text-sm text-gray-600 mb-1">
                Số BHXH:{" "}
                <span className="font-semibold text-gray-800">
                  {profile.socialInsCode || "Chưa có"}
                </span>
              </p>
              <p className="text-sm text-gray-600 mb-1">
                Ngày tham gia:{" "}
                <span className="font-semibold text-gray-800">
                  {profile.socialInsJoinDate
                    ? new Date(profile.socialInsJoinDate).toLocaleDateString(
                        "vi-VN",
                      )
                    : "Chưa cập nhật"}
                </span>
              </p>
              <p className="text-sm text-gray-600 mb-1">
                Nơi khám chữa bệnh:{" "}
                <span className="font-semibold text-gray-800">
                  {profile.insuranceHospital || "Chưa cập nhật"}
                </span>
              </p>
            </div>
          </div>

          {/* Cột 3: Công việc & Ngân hàng */}
          <div className="space-y-4">
            <div>
              <h3 className="font-bold text-gray-700 text-sm border-l-4 border-yellow-500 pl-2 uppercase mb-2">
                Thanh toán lương
              </h3>
              <p className="text-sm text-gray-600 mb-1">
                Ngân hàng:{" "}
                <span className="font-semibold text-gray-800">
                  {profile.bankName || "Chưa có"}
                </span>
              </p>
              <p className="text-sm text-gray-600 mb-1">
                Số tài khoản:{" "}
                <span className="font-semibold text-gray-800">
                  {profile.bankAccount || "Chưa có"}
                </span>
              </p>
            </div>
            <div>
              <h3 className="font-bold text-gray-700 text-sm border-l-4 border-purple-500 pl-2 uppercase mb-2">
                Công ty
              </h3>
              <p className="text-sm text-gray-600 mb-1">
                Ngày gia nhập:{" "}
                <span className="font-semibold text-gray-800">
                  {profile.joinedDate
                    ? new Date(profile.joinedDate).toLocaleDateString("vi-VN")
                    : "-"}
                </span>
              </p>
            </div>
          </div>
        </div>

        {/* Khối tài liệu minh chứng đính kèm */}
        <div className="pt-6 border-t">
          <h3 className="font-bold text-gray-700 text-sm mb-3 uppercase">
            Tài liệu minh chứng
          </h3>
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            {profile.identityFrontUrl && (
              <a
                href={`${BACKEND_URL}${profile.identityFrontUrl}`}
                target="_blank"
                rel="noreferrer"
                className="p-3 border rounded text-center text-sm font-medium text-blue-600 hover:bg-blue-50 bg-gray-50/50"
              >
                📄 CCCD Mặt trước
              </a>
            )}
            {profile.identityBackUrl && (
              <a
                href={`${BACKEND_URL}${profile.identityBackUrl}`}
                target="_blank"
                rel="noreferrer"
                className="p-3 border rounded text-center text-sm font-medium text-blue-600 hover:bg-blue-50 bg-gray-50/50"
              >
                📄 CCCD Mặt sau
              </a>
            )}
            {profile.certificateUrl && (
              <a
                href={`${BACKEND_URL}${profile.certificateUrl}`}
                target="_blank"
                rel="noreferrer"
                className="p-3 border rounded text-center text-sm font-medium text-blue-600 hover:bg-blue-50 bg-gray-50/50"
              >
                🎓 Bằng cấp/Chứng chỉ
              </a>
            )}
          </div>
        </div>

        <div className="pt-6 border-t">
          <h3 className="font-bold text-gray-700 text-sm mb-3 uppercase">
            Người phụ thuộc
          </h3>
          {loadingDependents ? (
            <p className="text-sm text-gray-500">Đang tải người phụ thuộc...</p>
          ) : dependents.length === 0 ? (
            <p className="rounded border border-dashed bg-gray-50 p-4 text-sm text-gray-500">
              Chưa có người phụ thuộc đang hiệu lực.
            </p>
          ) : (
            <div className="overflow-hidden rounded border border-gray-200">
              <table className="min-w-full divide-y divide-gray-200 text-sm">
                <thead className="bg-gray-50 text-left text-xs font-semibold uppercase text-gray-500">
                  <tr>
                    <th className="px-4 py-3">Họ tên</th>
                    <th className="px-4 py-3">Quan hệ</th>
                    <th className="px-4 py-3">MST phụ thuộc</th>
                    <th className="px-4 py-3">Hiệu lực</th>
                    <th className="px-4 py-3">Trạng thái</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 bg-white">
                  {dependents.map((item) => (
                    <tr key={item.id}>
                      <td className="px-4 py-3">
                        <div className="font-medium text-gray-800">
                          {item.fullName}
                        </div>
                        <div className="text-xs text-gray-500">
                          {item.idNumber || "Chưa có CCCD"}
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        {getDependentRelationText(item.relationship)}
                      </td>
                      <td className="px-4 py-3">
                        {item.taxDependentCode || "-"}
                      </td>
                      <td className="px-4 py-3">
                        {item.validFrom
                          ? new Date(item.validFrom).toLocaleDateString("vi-VN")
                          : "-"}
                        {item.validTo
                          ? ` - ${new Date(item.validTo).toLocaleDateString("vi-VN")}`
                          : ""}
                      </td>
                      <td className="px-4 py-3">
                        <span
                          className={`rounded-full px-2 py-1 text-xs font-medium ${
                            item.isActive
                              ? "bg-green-50 text-green-700"
                              : "bg-gray-100 text-gray-500"
                          }`}
                        >
                          {item.isActive ? "Đang hiệu lực" : "Ngừng hiệu lực"}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

      </div>
    </div>
  );
};
