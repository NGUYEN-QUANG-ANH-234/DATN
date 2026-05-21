import React, { createContext, useContext, useState, useEffect } from "react";

// Định nghĩa kiểu dữ liệu cho thông báo
interface AlertConfig {
  type: "success" | "error" | "warning" | "confirm";
  title: string;
  message: string;
  onConfirm?: () => void;
}

interface NotificationContextType {
  triggerAlert: (
    type: AlertConfig["type"],
    title: string,
    message: string,
    onConfirm?: () => void,
  ) => void;
  closeAlert: () => void;
}

const NotificationContext = createContext<NotificationContextType | undefined>(
  undefined,
);

export const NotificationProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [config, setConfig] = useState<AlertConfig | null>(null);

  const triggerAlert = (
    type: AlertConfig["type"],
    title: string,
    message: string,
    onConfirm?: () => void,
  ) => {
    setConfig({ type, title, message, onConfirm });
  };

  const closeAlert = () => setConfig(null);

  // Tự động đóng Toast sau 3 giây (trừ Modal xác nhận)
  useEffect(() => {
    if (config && config.type !== "confirm") {
      const timer = setTimeout(() => closeAlert(), 3000);
      return () => clearTimeout(timer);
    }
  }, [config]);

  return (
    <NotificationContext.Provider value={{ triggerAlert, closeAlert }}>
      {children}

      {/* RENDER POPUP ĐỒNG NHẤT Ở ĐÂY - LUÔN NẰM TRÊN CÙNG HỆ THỐNG */}
      {config &&
        (config.type === "confirm" ? (
          // 1. Giao diện Modal xác nhận giải thể/xóa
          <div className="fixed inset-0 bg-black/40 backdrop-blur-sm flex items-center justify-center z-50 p-4">
            <div className="bg-white rounded-xl shadow-xl max-w-md w-full border border-gray-100 p-6">
              <div className="flex items-start gap-4">
                <span className="text-3xl">❓</span>
                <div className="flex-1">
                  <h3 className="text-lg font-bold text-gray-900 mb-1">
                    {config.title}
                  </h3>
                  <p className="text-sm text-gray-600 leading-relaxed">
                    {config.message}
                  </p>
                </div>
              </div>
              <div className="flex justify-end gap-3 mt-6">
                <button
                  onClick={closeAlert}
                  className="px-4 py-2 text-sm font-medium text-gray-600 bg-gray-100 hover:bg-gray-200 rounded-lg"
                >
                  Hủy bỏ
                </button>
                <button
                  onClick={() => {
                    if (config.onConfirm) config.onConfirm();
                    closeAlert();
                  }}
                  className="px-4 py-2 text-sm font-medium text-white bg-red-600 hover:bg-red-700 rounded-lg shadow-sm"
                >
                  Xác nhận
                </button>
              </div>
            </div>
          </div>
        ) : (
          // 2. Giao diện Toast nhanh (Thành công/Thất bại/Cảnh báo)
          <div
            className={`fixed top-5 right-5 z-50 min-w-[320px] max-w-md p-4 rounded-xl shadow-lg border flex items-start gap-3 bg-white text-gray-800 border-gray-200`}
          >
            <span className="text-xl">
              {config.type === "success"
                ? "✅"
                : config.type === "error"
                  ? "❌"
                  : "⚠️"}
            </span>
            <div className="flex-1">
              <h4 className="font-bold text-sm">{config.title}</h4>
              <p className="text-xs opacity-90 mt-0.5">{config.message}</p>
            </div>
            <button
              onClick={closeAlert}
              className="text-gray-400 hover:text-gray-600 text-sm font-bold ml-2"
            >
              ✕
            </button>
          </div>
        ))}
    </NotificationContext.Provider>
  );
};

// Hook tùy biến để gọi thông báo ở bất cứ đâu cực nhanh
// eslint-disable-next-line react-refresh/only-export-components
export const useNotification = () => {
  const context = useContext(NotificationContext);
  if (!context)
    throw new Error("useNotification phải được đặt trong NotificationProvider");
  return context;
};
