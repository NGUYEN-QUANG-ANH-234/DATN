import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App.tsx";
import { GoogleOAuthProvider } from "@react-oauth/google";
import "./index.css";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    {/* Dán Client ID bạn lấy từ Google Cloud Console vào đây */}
    <GoogleOAuthProvider clientId="838887932315-qeot1elujhg3tci16priaf4scql22nuo.apps.googleusercontent.com">
      <App />
    </GoogleOAuthProvider>
  </React.StrictMode>,
);
