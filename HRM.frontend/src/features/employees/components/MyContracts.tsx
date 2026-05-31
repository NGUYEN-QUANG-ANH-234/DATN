import React from "react";
import { useMyProfileData } from "../hooks/useMyProfileData";

export const MyContracts: React.FC = () => {
  const { contracts, loadingContracts } = useMyProfileData({
    includeProfile: false,
    includeDependents: false,
  });

  if (loadingContracts)
    return (
      <div className="p-8 text-center text-gray-500 animate-pulse">
        Đang tải thông tin hợp đồng...
      </div>
    );

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat("vi-VN", {
      style: "currency",
      currency: "VND",
    }).format(value);
  };

  const getStatusStyle = (status: string) => {
    switch (status) {
      case "Active":
        return "bg-green-50 text-green-700 border-green-200";
      case "Draft":
        return "bg-yellow-50 text-yellow-700 border-yellow-200";
      case "Expired":
        return "bg-gray-50 text-gray-600 border-gray-200";
      default:
        return "bg-red-50 text-red-700 border-red-200";
    }
  };

  return (
    <div className="p-6 bg-gray-50 min-h-screen flex justify-center">
      <div className="w-full max-w-4xl bg-white p-8 rounded-xl shadow-sm border border-gray-100">
        <h2 className="text-2xl font-bold text-gray-800 mb-2">
          Danh sách hợp đồng cá nhân
        </h2>
        <p className="text-gray-500 text-sm mb-6">
          Theo dõi thời hạn, lịch sử điều khoản lương thưởng và trạng thái pháp
          lý hợp đồng của bạn.
        </p>

        <div className="space-y-4">
          {contracts.length === 0 ? (
            <p className="text-center text-gray-400 py-8">
              Bạn hiện chưa có bản ghi hợp đồng nào trên hệ thống.
            </p>
          ) : (
            contracts.map((contract) => (
              <div
                key={contract.id}
                className="border border-gray-200 rounded-lg p-5 bg-white hover:shadow-md transition-shadow"
              >
                <div className="flex justify-between items-start flex-wrap gap-2 border-b pb-3 mb-3">
                  <div>
                    <h3 className="font-bold text-gray-800 text-base">
                      Số HĐ: {contract.contractNumber}
                    </h3>
                    <p className="text-xs text-gray-400 mt-0.5">
                      Loại: {contract.contractType} (Phiên bản v
                      {contract.version})
                    </p>
                  </div>
                  <span
                    className={`text-xs px-2.5 py-1 font-semibold rounded-md border ${getStatusStyle(contract.status)}`}
                  >
                    {contract.status}
                  </span>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 text-sm text-gray-600">
                  <div>
                    <span className="block text-xs text-gray-400">
                      Lương cơ bản (Tỷ lệ):
                    </span>
                    <span className="font-bold text-gray-800">
                      {formatCurrency(contract.basicSalary)}
                    </span>{" "}
                    ({contract.salaryPercentage}%)
                  </div>
                  <div>
                    <span className="block text-xs text-gray-400">
                      Lương đóng BHXH:
                    </span>
                    <span className="font-semibold text-gray-700">
                      {formatCurrency(contract.insuranceSalary)}
                    </span>
                  </div>
                  <div>
                    <span className="block text-xs text-gray-400">
                      Thời hạn hợp đồng:
                    </span>
                    <span className="font-medium text-gray-700">
                      {new Date(contract.startDate).toLocaleDateString("vi-VN")}{" "}
                      -{" "}
                      {contract.endDate
                        ? new Date(contract.endDate).toLocaleDateString("vi-VN")
                        : "Vô thời hạn"}
                    </span>
                  </div>
                </div>

                {contract.negotiationNote && (
                  <div className="mt-4 p-2.5 bg-gray-50 rounded border border-dashed border-gray-200 text-xs text-gray-500">
                    <span className="font-semibold block text-gray-600 mb-0.5">
                      Ghi chú ý kiến thương thảo:
                    </span>
                    {contract.negotiationNote}
                  </div>
                )}
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
};
