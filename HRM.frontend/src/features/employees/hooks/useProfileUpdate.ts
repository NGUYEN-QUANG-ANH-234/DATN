import { useState } from "react";
import { profileApi } from "../api/profileApi";

export const useProfileUpdate = () => {
  const [submitting, setSubmitting] = useState(false);

  const handleRequestUpdate = async (formData: FormData) => {
    setSubmitting(true);
    try {
      const res = await profileApi.requestUpdate(formData);
      alert(res.message);
      return true;
    } catch (error: unknown) {
      // Hứng lỗi 409 Conflict (Trùng CCCD) hoặc 400 (Sai định dạng)
      alert(
        (error as { response?: { data?: { message?: string } } }).response?.data
          ?.message || "Đã xảy ra lỗi khi cập nhật hồ sơ.",
      );
      return false;
    } finally {
      setSubmitting(false);
    }
  };

  return { submitting, handleRequestUpdate };
};
