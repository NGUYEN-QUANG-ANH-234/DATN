import { useNavigate } from "react-router-dom";
import { jwtDecode } from "jwt-decode";
import axiosClient from "../../core/api/axiosClient";

// 1. Định nghĩa Interface khớp với Token từ Backend
interface MyTokenPayload {
  id: string;
  role: string;
  email: string;
  exp: number;
  // Các key mặc định của Microsoft Identity nếu bạn chưa sửa Backend
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"?: string;
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"?: string;
}

const DashboardPage = () => {
  const navigate = useNavigate();
  const token = localStorage.getItem("accessToken");

  // --- LOGIC GIẢI MÃ TOKEN (NGÀY 4) ---
  const getDecodedData = () => {
    if (!token) return { role: "", email: "" };
    try {
      const decoded = jwtDecode<MyTokenPayload>(token);

      // Ưu tiên lấy key ngắn nếu bạn đã sửa Backend, nếu chưa thì dùng key dài mặc định
      const role =
        decoded.role ||
        decoded[
          "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        ] ||
        "";
      const email =
        decoded.email ||
        decoded[
          "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
        ] ||
        "";

      return { role, email };
    } catch (error) {
      console.error("Token không hợp lệ:", error);
      return { role: "", email: "" };
    }
  };

  // Lấy dữ liệu một lần duy nhất
  const { role: userRole, email: userEmail } = getDecodedData();

  const handleLogout = async () => {
    try {
      const refreshToken = localStorage.getItem("refreshToken");
      if (refreshToken) {
        await axiosClient.post("/auth/logout", { refreshToken });
      }
    } catch (error) {
      console.error("Lỗi khi gọi API đăng xuất:", error);
    } finally {
      localStorage.clear();
      navigate("/", { replace: true });
    }
  };

  return (
    <div style={{ padding: "20px", fontFamily: "Arial, sans-serif" }}>
      <h1>Hệ thống Quản lý Nhân sự (HRM)</h1>
      <p style={{ color: "green", fontWeight: "bold" }}>
        Chào mừng {userEmail || "Người dùng"} đã đăng nhập thành công!
      </p>
      <p>
        Vai trò hiện tại: <strong>Role ID {userRole || "N/A"}</strong>
      </p>

      <div
        style={{
          border: "1px solid #ddd",
          padding: "20px",
          marginTop: "20px",
          borderRadius: "8px",
          backgroundColor: "#fafafa",
        }}
      >
        <h3 style={{ marginTop: 0 }}>Menu Chức Năng (Phân quyền thực tế)</h3>
        <ul style={{ lineHeight: "2", listStyleType: "none", padding: 0 }}>
          <li>👤 Xem thông tin cá nhân</li>

          {/* Logic hiển thị dựa trên Role ID */}
          {userRole === "7" && (
            <li style={{ color: "#007bff" }}>
              📂 Gửi hồ sơ ứng tuyển (Dành cho Ứng viên)
            </li>
          )}

          {(userRole === "2" || userRole === "3") && (
            <li style={{ color: "#28a745" }}>
              👥 Quản lý hồ sơ nhân viên (Dành cho Admin/HR)
            </li>
          )}

          {userRole === "2" && (
            <li style={{ color: "#dc3545", fontWeight: "bold" }}>
              💰 QUẢN LÝ LƯƠNG & PHÚC LỢI (Chỉ dành cho Admin)
            </li>
          )}
        </ul>
      </div>

      <button
        onClick={handleLogout}
        style={{
          marginTop: "20px",
          backgroundColor: "#dc3545",
          color: "white",
          border: "none",
          padding: "10px 25px",
          borderRadius: "5px",
          cursor: "pointer",
          fontSize: "16px",
          fontWeight: "bold",
        }}
      >
        Đăng xuất
      </button>
    </div>
  );
};

export default DashboardPage;
