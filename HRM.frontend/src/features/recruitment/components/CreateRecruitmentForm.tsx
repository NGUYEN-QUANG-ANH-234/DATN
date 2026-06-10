import React, { useState } from "react";
import { useCreateRecruitment } from "../hooks/useCreateRecruitment";
import { useMyRequests } from "../hooks/useMyRequests";
import type { CreateRecruitmentPayload } from "../types/recruitment";
import { useNotification } from "../../../core/context/NotificationContext";

export const CreateRecruitmentForm: React.FC = () => {
  // Gọi Hooks
  const { loading, departments, positions, handleCreateRequest } =
    useCreateRecruitment();
  const { myRequests, fetchMyRequests } = useMyRequests();
  const { triggerAlert } = useNotification();

  // State Form
  const [deptId, setDeptId] = useState<number | "">("");
  const [positionId, setPositionId] = useState<number | "">("");
  const [quantity, setQuantity] = useState<number>(1);
  const [description, setDescription] = useState("");
  const [deadline, setDeadline] = useState("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!deptId || !positionId) {
      triggerAlert("warning", "Thiếu thông tin", "Vui lòng chọn Phòng ban và Vị trí cần tuyển.");
      return;
    }

    const payload: CreateRecruitmentPayload = {
      deptId: Number(deptId),
      positionId: Number(positionId),
      quantity,
      description,
      deadline: deadline ? new Date(deadline).toISOString() : undefined,
    };

    const isSuccess = await handleCreateRequest(payload);

    if (isSuccess) {
      // Reset form
      setDeptId("");
      setPositionId("");
      setQuantity(1);
      setDescription("");
      setDeadline("");

      // Load lại danh sách lịch sử
      fetchMyRequests();
    }
  };

  return (
    <div className="mx-auto w-full max-w-6xl space-y-6">
      {/* KHỐI 1: FORM TẠO ĐƠN */}
      <div className="rounded-lg border border-gray-200 bg-white p-5 shadow-sm sm:p-6">
        <div className="mb-6 border-b pb-3">
          <h2 className="text-xl font-bold text-gray-800">
            Tạo Đề Xuất Tuyển Dụng
          </h2>
        </div>

        <form onSubmit={handleSubmit} className="space-y-5">
          <div className="grid grid-cols-2 gap-5">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Phòng ban đề xuất *
              </label>
              <select
                required
                className="w-full border p-2.5 rounded"
                value={deptId}
                onChange={(e) =>
                  setDeptId(e.target.value ? Number(e.target.value) : "")
                }
              >
                <option value="">-- Chọn phòng ban --</option>
                {departments.map((d) => (
                  <option key={d.id} value={d.id}>
                    {d.deptName}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Vị trí chức danh *
              </label>
              <select
                required
                className="w-full border p-2.5 rounded"
                value={positionId}
                onChange={(e) =>
                  setPositionId(e.target.value ? Number(e.target.value) : "")
                }
              >
                <option value="">-- Chọn chức danh --</option>
                {positions.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.title}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-5">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Số lượng *
              </label>
              <input
                type="number"
                min="1"
                required
                className="w-full border p-2.5 rounded"
                value={quantity}
                onChange={(e) => setQuantity(Number(e.target.value))}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Hạn tuyển dụng
              </label>
              <input
                type="date"
                className="w-full border p-2.5 rounded"
                value={deadline}
                onChange={(e) => setDeadline(e.target.value)}
              />
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Mô tả chi tiết (JD)
            </label>
            <textarea
              rows={4}
              className="w-full border p-2.5 rounded"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full bg-blue-600 hover:bg-blue-700 text-white font-medium py-3 px-4 rounded-lg mt-4"
          >
            {loading ? "Đang xử lý..." : " Gửi Yêu Cầu Tuyển Dụng"}
          </button>
        </form>
      </div>

      {/* KHỐI 2: DANH SÁCH ĐƠN ĐÃ TẠO */}
      <div className="rounded-lg border border-gray-200 bg-white p-5 shadow-sm sm:p-6">
        <h3 className="text-lg font-bold text-gray-800 mb-4 border-b pb-2">
          Lịch sử Yêu cầu của tôi
        </h3>

        {myRequests.length === 0 ? (
          <p className="text-gray-500 text-center py-4">
            Bạn chưa tạo yêu cầu nào.
          </p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm text-left text-gray-500">
              <thead className="text-xs text-gray-700 uppercase bg-gray-50">
                <tr>
                  <th className="px-4 py-3">ID</th>
                  <th className="px-4 py-3">Phòng ban</th>
                  <th className="px-4 py-3">Vị trí</th>
                  <th className="px-4 py-3">Số lượng</th>
                  <th className="px-4 py-3">Ngày tạo</th>
                  <th className="px-4 py-3">Trạng thái</th>
                </tr>
              </thead>
              <tbody>
                {myRequests.map((req) => (
                  <tr key={req.id} className="border-b hover:bg-gray-50">
                    <td className="px-4 py-3 font-medium text-gray-900">
                      #{req.id}
                    </td>
                    <td className="px-4 py-3">{req.departmentName}</td>
                    <td className="px-4 py-3">{req.positionName}</td>
                    <td className="px-4 py-3">{req.quantity}</td>
                    <td className="px-4 py-3">
                      {new Date(req.createdAt).toLocaleDateString("vi-VN")}
                    </td>
                    <td className="px-4 py-3">
                      <span
                        className={`px-2 py-1 rounded text-xs font-medium 
                        ${
                          req.status.includes("Pending")
                            ? "bg-yellow-100 text-yellow-800"
                            : req.status === "Approved"
                              ? "bg-green-100 text-green-800"
                              : "bg-red-100 text-red-800"
                        }`}
                      >
                        {req.status}
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
  );
};
