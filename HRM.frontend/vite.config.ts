// vite.config.ts
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  // server: {
  //   headers: {
  //     // Cho phép cửa sổ popup gửi tin nhắn (postMessage) về trang chính
  //     "Cross-Origin-Opener-Policy": "same-origin-allow-popups",
  //     "Cross-Origin-Embedder-Policy": "unsafe-none",
  //   },

  // },
  server: {
    // Đảm bảo host và port được định nghĩa rõ ràng
    host: "localhost",
    port: 5173,
    // https: true, // Bật HTTPS
  },
});
