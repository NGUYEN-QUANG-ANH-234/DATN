import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import mkcert from "vite-plugin-mkcert"; // 1. Import mkcert

export default defineConfig({
  plugins: [
    react(),
    mkcert(), // Plugin này sẽ tự động lo tạo và cấu hình key/cert ngầm
  ],
  server: {
    headers: {
      // BẮT BUỘC dùng dòng này cho Google OAuth
      "Cross-Origin-Opener-Policy": "same-origin-allow-popups",
      "Cross-Origin-Embedder-Policy": "unsafe-none",
    },
    // Đã xóa https: true ở đây để hết lỗi TypeScript
    host: "localhost",
    port: 5173,
  },
});
