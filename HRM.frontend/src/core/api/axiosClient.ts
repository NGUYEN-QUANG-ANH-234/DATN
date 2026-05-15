import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios";

// 1. Định nghĩa Interface cho dữ liệu Token
interface TokenResponse {
  accessToken: string;
  AccessToken?: string; // Hỗ trợ cả PascalCase
  refreshToken: string;
}

// 2. Mở rộng Type của Axios để thêm thuộc tính _retry mà không dùng any
interface CustomAxiosRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
}

const axiosClient = axios.create({
  baseURL: "https://localhost:7003/api/v1",
  withCredentials: true,
});

axiosClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem("accessToken");
    console.log(
      `[Request] Gọi API: ${config.url}`,
      "Token đang gửi:",
      token ? "CÓ" : "KHÔNG CÓ",
    );
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  },
);

axiosClient.interceptors.response.use(
  (response) => response.data,
  async (error: AxiosError) => {
    console.error(
      `[Lỗi API] ${error.config?.url} - Status:`,
      error.response?.status,
    );
    console.error(`[Chi tiết lỗi từ Backend]:`, error.response?.data); // QUAN TRỌNG NHẤT

    // Ép kiểu config về CustomAxiosRequestConfig
    const originalRequest = error.config as CustomAxiosRequestConfig;

    if (
      error.response?.status === 401 &&
      originalRequest &&
      !originalRequest._retry
    ) {
      originalRequest._retry = true;

      try {
        console.log("[Interceptor] Bắt đầu gọi Refresh Token...");

        const accessToken = localStorage.getItem("accessToken");

        // Gọi API refresh token
        const res = await axios.post<TokenResponse>(
          "https://localhost:7003/api/v1/auth/refresh",
          {
            accessToken,
          },
          {
            withCredentials: true, // BẮT BUỘC ĐỂ TRÌNH DUYỆT GỬI KÈM COOKIE
          },
        );

        const data = res.data;
        console.log("[Interceptor] Kết quả Refresh:", data);

        const newAccessToken = data.accessToken || data.AccessToken;

        if (newAccessToken) {
          localStorage.setItem("accessToken", newAccessToken);
          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
          }
          return axiosClient(originalRequest);
        }
      } catch (refreshError: unknown) {
        const errorMessage =
          refreshError instanceof AxiosError
            ? refreshError.response?.data || refreshError.message
            : refreshError instanceof Error
              ? refreshError.message
              : String(refreshError);
        console.error(
          "[Interceptor] Refresh THẤT BẠI. Bị đẩy ra ngoài vì:",
          errorMessage,
        );
        // Nếu refresh token cũng hết hạn hoặc lỗi -> Xóa token và bắt đăng nhập lại
        localStorage.clear();
        window.location.href = "/";
        return Promise.reject(refreshError);
      }
    }

    if (error.response?.status === 403) {
      alert("Bạn không có quyền thực hiện hành động này!");
    }

    const responseData = error.response?.data as unknown;

    // Quét tìm thông báo lỗi từ Backend (Hỗ trợ Message hoa, message thường, hoặc title mặc định của .NET)
    const customErrorMessage =
      (responseData as { message?: string })?.message ||
      (responseData as { Message?: string })?.Message ||
      (responseData as { title?: string })?.title ||
      error.message ||
      "Đã có lỗi xảy ra từ máy chủ!";

    // Ghi đè thẳng vào message của Axios
    error.message = customErrorMessage;

    return Promise.reject(error);
  },
);

export default axiosClient;
