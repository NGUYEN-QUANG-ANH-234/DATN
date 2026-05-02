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
  baseURL: "https://localhost:7003/api",
  withCredentials: true,
});

axiosClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem("accessToken");
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
    // Ép kiểu config về CustomAxiosRequestConfig
    const originalRequest = error.config as CustomAxiosRequestConfig;

    if (
      error.response?.status === 401 &&
      originalRequest &&
      !originalRequest._retry
    ) {
      originalRequest._retry = true;

      try {
        const refreshToken = localStorage.getItem("refreshToken");
        const accessToken = localStorage.getItem("accessToken");

        // Gọi API refresh token
        const res = await axios.post<TokenResponse>(
          "https://localhost:7003/api/auth/refresh",
          {
            accessToken,
            refreshToken,
          },
        );

        const data = res.data;
        const newAccessToken = data.accessToken || data.AccessToken;

        if (newAccessToken) {
          localStorage.setItem("accessToken", newAccessToken);
          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
          }
          return axiosClient(originalRequest);
        }
      } catch (refreshError) {
        // Nếu refresh token cũng hết hạn hoặc lỗi -> Xóa token và bắt đăng nhập lại
        localStorage.clear();
        window.location.href = "/";
        return Promise.reject(refreshError);
      }
    }

    if (error.response?.status === 403) {
      alert("Bạn không có quyền thực hiện hành động này!");
    }

    return Promise.reject(error);
  },
);

export default axiosClient;
