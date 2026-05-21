import { useState, useEffect, useCallback } from "react";
import { myProfileApi } from "../api/myProfileApi";
import type { MyProfileDto, MyContractDto } from "../types/myProfile";

export const useMyProfileData = () => {
  const [profile, setProfile] = useState<MyProfileDto | null>(null);
  const [contracts, setContracts] = useState<MyContractDto[]>([]);
  const [loadingProfile, setLoadingProfile] = useState(false);
  const [loadingContracts, setLoadingContracts] = useState(false);

  const fetchProfile = useCallback(async () => {
    setLoadingProfile(true);
    try {
      const res = await myProfileApi.getProfile();
      setProfile(res.data);
    } catch (error) {
      console.error("Lỗi tải thông tin hồ sơ:", error);
    } finally {
      setLoadingProfile(false);
    }
  }, []);

  const fetchContracts = useCallback(async () => {
    setLoadingContracts(true);
    try {
      const res = await myProfileApi.getContracts();
      setContracts(res.data || []);
    } catch (error) {
      console.error("Lỗi tải danh sách hợp đồng:", error);
    } finally {
      setLoadingContracts(false);
    }
  }, []);

  useEffect(() => {
    fetchProfile();
    fetchContracts();
  }, [fetchProfile, fetchContracts]);

  return {
    profile,
    contracts,
    loadingProfile,
    loadingContracts,
    refreshProfile: fetchProfile,
  };
};
