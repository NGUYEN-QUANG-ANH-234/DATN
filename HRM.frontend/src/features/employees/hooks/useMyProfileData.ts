import { useState, useEffect, useCallback } from "react";
import { myProfileApi } from "../api/myProfileApi";
import { dependentApi } from "../api/dependentApi";
import type { MyProfileDto, MyContractDto } from "../types/myProfile";
import type { DependentDto } from "../types/dependent";

interface UseMyProfileDataOptions {
  includeProfile?: boolean;
  includeContracts?: boolean;
  includeDependents?: boolean;
}

export const useMyProfileData = ({
  includeProfile = true,
  includeContracts = true,
  includeDependents = true,
}: UseMyProfileDataOptions = {}) => {
  const [profile, setProfile] = useState<MyProfileDto | null>(null);
  const [contracts, setContracts] = useState<MyContractDto[]>([]);
  const [dependents, setDependents] = useState<DependentDto[]>([]);
  const [loadingProfile, setLoadingProfile] = useState(false);
  const [loadingContracts, setLoadingContracts] = useState(false);
  const [loadingDependents, setLoadingDependents] = useState(false);

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

  const fetchDependents = useCallback(async () => {
    setLoadingDependents(true);
    try {
      const res = await dependentApi.getMyDependents();
      setDependents(res.data || []);
    } catch (error) {
      console.error("Lỗi tải danh sách người phụ thuộc:", error);
    } finally {
      setLoadingDependents(false);
    }
  }, []);

  useEffect(() => {
    if (includeProfile) fetchProfile();
    if (includeContracts) fetchContracts();
    if (includeDependents) fetchDependents();
  }, [
    fetchProfile,
    fetchContracts,
    fetchDependents,
    includeProfile,
    includeContracts,
    includeDependents,
  ]);

  return {
    profile,
    contracts,
    dependents,
    loadingProfile,
    loadingContracts,
    loadingDependents,
    refreshProfile: fetchProfile,
    refreshDependents: fetchDependents,
  };
};
