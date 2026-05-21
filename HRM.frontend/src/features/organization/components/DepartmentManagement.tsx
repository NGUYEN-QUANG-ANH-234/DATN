import React, { useState, useEffect } from "react";
import { useDepartments } from "../hooks/useDepartments";
import type { DepartmentTree } from "../types/department";

// ==========================================
// 1. COMPONENT THÔNG BÁO ĐỒNG NHẤT (TOAST & MODAL)
// ==========================================
interface UnifiedAlertProps {
  type: "success" | "error" | "warning" | "confirm";
  title: string;
  message: string;
  onClose: () => void;
  onConfirm?: () => void;
}

const UnifiedAlert: React.FC<UnifiedAlertProps> = ({
  type,
  title,
  message,
  onClose,
  onConfirm,
}) => {
  // Tự động đóng sau 3 giây nếu chỉ là thông báo dạng Toast (không phải Modal xác nhận)
  useEffect(() => {
    if (type !== "confirm") {
      const timer = setTimeout(() => onClose(), 3000);
      return () => clearTimeout(timer);
    }
  }, [type, onClose]);

  const styleMap = {
    success: {
      bg: "bg-green-50 border-green-200",
      text: "text-green-800",
      icon: "✅",
    },
    error: { bg: "bg-red-50 border-red-200", text: "text-red-800", icon: "❌" },
    warning: {
      bg: "bg-amber-50 border-amber-200",
      text: "text-amber-800",
      icon: "⚠️",
    },
    confirm: {
      bg: "bg-white border-blue-200",
      text: "text-gray-800",
      icon: "❓",
    },
  };

  const currentStyle = styleMap[type];

  if (type === "confirm") {
    // Giao diện Modal phủ toàn màn hình dành cho việc Xác nhận hành động nguy hiểm (Giải thể)
    return (
      <div className="fixed inset-0 bg-black/40 backdrop-blur-sm flex items-center justify-center z-50 p-4 animate-fade-in">
        <div className="bg-white rounded-xl shadow-xl max-w-md w-full border border-gray-100 p-6 transform transition-all scale-100">
          <div className="flex items-start gap-4">
            <span className="text-3xl">{currentStyle.icon}</span>
            <div className="flex-1">
              <h3 className="text-lg font-bold text-gray-900 mb-1">{title}</h3>
              <p className="text-sm text-gray-600 leading-relaxed">{message}</p>
            </div>
          </div>
          <div className="flex justify-end gap-3 mt-6">
            <button
              onClick={onClose}
              className="px-4 py-2 text-sm font-medium text-gray-600 bg-gray-100 hover:bg-gray-200 rounded-lg transition-colors"
            >
              Hủy bỏ
            </button>
            <button
              onClick={() => {
                if (onConfirm) onConfirm();
                onClose();
              }}
              className="px-4 py-2 text-sm font-medium text-white bg-red-600 hover:bg-red-700 rounded-lg shadow-sm transition-colors"
            >
              Xác nhận giải thể
            </button>
          </div>
        </div>
      </div>
    );
  }

  // Giao diện Toast trượt ra ở góc phải màn hình cho các thông báo trạng thái nhanh
  return (
    <div
      className={`fixed top-5 right-5 z-50 min-w-[320px] max-w-md p-4 rounded-xl shadow-lg border ${currentStyle.bg} ${currentStyle.text} flex items-start gap-3 animate-slide-in`}
    >
      <span className="text-xl">{currentStyle.icon}</span>
      <div className="flex-1">
        <h4 className="font-bold text-sm">{title}</h4>
        <p className="text-xs opacity-90 mt-0.5">{message}</p>
      </div>
      <button
        onClick={onClose}
        className="text-gray-400 hover:text-gray-600 text-sm font-bold ml-2"
      >
        ✕
      </button>
    </div>
  );
};

// ==========================================
// 2. COMPONENT CON: RENDER NODE CÂY
// ==========================================
const DepartmentNode: React.FC<{
  node: DepartmentTree;
  flatList: { id: number; name: string }[];
  onUpdateParent: (id: number, newParentId: number | null) => void;
  onDeactivate: (id: number, name: string) => void;
}> = ({ node, flatList, onUpdateParent, onDeactivate }) => {
  return (
    <div className="ml-8 mt-3 border-l-2 border-gray-200 pl-4 relative">
      <div className="absolute w-4 h-0.5 bg-gray-300 left-0 top-6 -translate-x-full"></div>

      <div className="p-3 bg-white border border-gray-200 rounded shadow-sm flex flex-wrap justify-between items-center gap-4 hover:border-blue-300 transition-colors">
        <div>
          <span className="font-bold text-gray-800">{node.deptName}</span>
          <span className="text-xs text-gray-500 ml-2">({node.deptCode})</span>
        </div>

        <div className="flex items-center gap-2 text-sm">
          <label className="text-gray-500 text-xs hidden sm:block">
            Trực thuộc:
          </label>
          <select
            className="border border-gray-300 p-1.5 rounded focus:ring-2 focus:ring-blue-400 focus:outline-none bg-white text-gray-700"
            value={node.parentDeptId || ""}
            onChange={(e) =>
              onUpdateParent(
                node.id,
                e.target.value ? Number(e.target.value) : null,
              )
            }
          >
            <option value="">-- Cấp cao nhất (Root) --</option>
            {flatList.map((d) => (
              <option key={d.id} value={d.id} disabled={d.id === node.id}>
                {d.name}
              </option>
            ))}
          </select>

          <button
            onClick={() => onDeactivate(node.id, node.deptName)}
            className="bg-red-50 text-red-600 border border-red-200 px-3 py-1.5 rounded hover:bg-red-600 hover:text-white transition-colors"
          >
            Giải thể
          </button>
        </div>
      </div>

      {node.children && node.children.length > 0 && (
        <div className="mt-1">
          {node.children.map((child) => (
            <DepartmentNode
              key={child.id}
              node={child}
              flatList={flatList}
              onUpdateParent={onUpdateParent}
              onDeactivate={onDeactivate}
            />
          ))}
        </div>
      )}
    </div>
  );
};

// ==========================================
// 3. COMPONENT CHÍNH (QUẢN LÝ SƠ ĐỒ)
// ==========================================
export const DepartmentManagement: React.FC = () => {
  const {
    treeData,
    loading,
    handleUpdateParent,
    handleDeactivate,
    handleCreate,
  } = useDepartments();

  const [showForm, setShowForm] = useState(false);
  const [formData, setFormData] = useState({
    deptCode: "",
    deptName: "",
    parentDeptId: "",
  });

  // State Quản lý hệ thống Thông báo Đồng nhất
  const [alertConfig, setAlertConfig] = useState<Omit<
    UnifiedAlertProps,
    "onClose"
  > | null>(null);

  const triggerAlert = (
    type: UnifiedAlertProps["type"],
    title: string,
    message: string,
    onConfirm?: () => void,
  ) => {
    setAlertConfig({ type, title, message, onConfirm });
  };

  const flattenTree = (
    nodes: DepartmentTree[],
  ): { id: number; name: string }[] => {
    return nodes.reduce(
      (acc, curr) => {
        return [
          ...acc,
          { id: curr.id, name: curr.deptName },
          ...flattenTree(curr.children),
        ];
      },
      [] as { id: number; name: string }[],
    );
  };

  const flatList = flattenTree(treeData);

  // Điều phối: Xử lý Thêm phòng ban mới
  const onSubmitCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    const payload = {
      deptCode: formData.deptCode,
      deptName: formData.deptName,
      parentDeptId: formData.parentDeptId
        ? Number(formData.parentDeptId)
        : null,
    };

    try {
      const success = await handleCreate(payload);
      if (success) {
        setShowForm(false);
        setFormData({ deptCode: "", deptName: "", parentDeptId: "" });
        triggerAlert(
          "success",
          "Thành công",
          `Đã thiết lập phòng ban [${payload.deptName}] vào hệ thống.`,
        );
      } else {
        triggerAlert(
          "error",
          "Thất bại",
          "Mã phòng ban đã tồn tại hoặc dữ liệu không hợp lệ.",
        );
      }
    } catch {
      triggerAlert(
        "error",
        "Lỗi kết nối",
        "Hệ thống không thể xử lý yêu cầu lúc này.",
      );
    }
  };

  // Điều phối: Thay đổi trực thuộc (Phòng ban cha)
  const onInterceptUpdateParent = async (
    id: number,
    newParentId: number | null,
  ) => {
    try {
      await handleUpdateParent(id, newParentId);
      triggerAlert(
        "success",
        "Cập nhật thành công",
        "Đã điều chỉnh sơ đồ trực thuộc trên cây tổ chức.",
      );
    } catch {
      triggerAlert(
        "error",
        "Lỗi cấu trúc",
        "Không thể di chuyển! Phát hiện nguy cơ lặp vòng vô hạn (Circular Dependency).",
      );
    }
  };

  // Điều phối: Cảnh báo giải thể an toàn (Tích hợp luồng F0.5)
  const onInterceptDeactivate = (id: number, name: string) => {
    triggerAlert(
      "confirm",
      "Xác nhận giải thể",
      `Bạn có chắc chắn muốn ngừng hoạt động phòng [${name}]? Hệ thống sẽ quét kiểm tra nhân sự trước khi thực thi.`,
      async () => {
        try {
          const success = await handleDeactivate(id);
          if (success) {
            triggerAlert(
              "success",
              "Đã giải thể",
              `Phòng ban [${name}] đã được gỡ khỏi sơ đồ cây.`,
            );
          } else {
            triggerAlert(
              "warning",
              "Không thể thực thi",
              "Giải thể thất bại! Vui lòng thuyên chuyển toàn bộ nhân sự (F8.5) ra khỏi phòng ban này trước.",
            );
          }
        } catch {
          triggerAlert(
            "error",
            "Lỗi hệ thống",
            "Không thể kiểm tra hoặc tương tác với cơ sở dữ liệu.",
          );
        }
      },
    );
  };

  if (loading)
    return (
      <div className="p-8 text-center text-gray-500 animate-pulse font-medium">
        Đang đồng bộ sơ đồ tổ chức HICAS...
      </div>
    );

  return (
    <div className="min-h-full rounded-lg bg-gray-50 px-4 py-6 sm:px-6 relative">
      {/* KHÔNG GIAN HIỂN THỊ THÔNG BÁO ĐỒNG NHẤT */}
      {alertConfig && (
        <UnifiedAlert
          type={alertConfig.type}
          title={alertConfig.title}
          message={alertConfig.message}
          onConfirm={alertConfig.onConfirm}
          onClose={() => setAlertConfig(null)}
        />
      )}

      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold text-gray-800">Cấu trúc Tổ chức</h2>
        <button
          onClick={() => setShowForm(!showForm)}
          className="bg-blue-600 text-white px-4 py-2 rounded shadow hover:bg-blue-700 transition-colors font-medium"
        >
          {showForm ? "Đóng" : "+ Thêm phòng ban"}
        </button>
      </div>

      {showForm && (
        <form
          onSubmit={onSubmitCreate}
          className="mb-6 p-4 bg-white border border-blue-200 rounded-lg shadow-sm flex flex-wrap gap-4 items-end animate-fade-in"
        >
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Mã PB (*)
            </label>
            <input
              required
              type="text"
              placeholder="VD: IT"
              className="border p-2 rounded w-32 focus:ring-2 focus:ring-blue-400 focus:outline-none"
              value={formData.deptCode}
              onChange={(e) =>
                setFormData({ ...formData, deptCode: e.target.value })
              }
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Tên phòng ban (*)
            </label>
            <input
              required
              type="text"
              placeholder="VD: Phòng Công nghệ"
              className="border p-2 rounded w-64 focus:ring-2 focus:ring-blue-400 focus:outline-none"
              value={formData.deptName}
              onChange={(e) =>
                setFormData({ ...formData, deptName: e.target.value })
              }
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Trực thuộc
            </label>
            <select
              className="border p-2 rounded w-64 focus:ring-2 focus:ring-blue-400 focus:outline-none bg-white text-gray-700"
              value={formData.parentDeptId}
              onChange={(e) =>
                setFormData({ ...formData, parentDeptId: e.target.value })
              }
            >
              <option value="">-- Cấp cao nhất (Root) --</option>
              {flatList.map((d) => (
                <option key={d.id} value={d.id}>
                  {d.name}
                </option>
              ))}
            </select>
          </div>
          <button
            type="submit"
            className="bg-green-600 text-white px-5 py-2 rounded font-medium hover:bg-green-700 transition-colors"
          >
            Lưu mới
          </button>
        </form>
      )}

      <div className="overflow-x-auto rounded-lg border border-gray-200 bg-white p-5 shadow-sm sm:p-6">
        {treeData.length === 0 ? (
          <p className="text-center text-gray-400 py-8">
            Chưa có dữ liệu phòng ban.
          </p>
        ) : (
          treeData.map((rootNode) => (
            <DepartmentNode
              key={rootNode.id}
              node={rootNode}
              flatList={flatList}
              onUpdateParent={onInterceptUpdateParent}
              onDeactivate={onInterceptDeactivate}
            />
          ))
        )}
      </div>
    </div>
  );
};
