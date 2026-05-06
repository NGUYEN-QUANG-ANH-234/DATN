import { Link } from "react-router-dom";

export const Header = () => {
  return (
    <header className="flex justify-between p-4 bg-white shadow">
      <div>HRM HICAS</div>

      <div className="flex items-center gap-4">
        {/* Nút bấm chuyển hướng sang trang MFA */}
        <Link
          to="/mfa-setup"
          className="bg-blue-100 text-blue-700 px-3 py-1.5 rounded text-sm font-medium hover:bg-blue-200 transition"
        >
          Thiết lập MFA
        </Link>

        <div className="avatar">👤 Avatar</div>
      </div>
    </header>
  );
};
