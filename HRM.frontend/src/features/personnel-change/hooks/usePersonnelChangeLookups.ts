import { useEffect, useMemo, useState } from "react";
import { personnelChangeApi } from "../api/personnelChangeApi";
import type {
  PersonnelChangeContractOption,
  PersonnelChangeDepartmentOption,
  PersonnelChangeEmployeeOption,
  PersonnelChangeJobLevelOption,
  PersonnelChangePenaltyOption,
  PersonnelChangePerformanceReviewOption,
  PersonnelChangePositionOption,
} from "../types/personnelChange";

type LookupState = {
  employees: PersonnelChangeEmployeeOption[];
  departments: PersonnelChangeDepartmentOption[];
  positions: PersonnelChangePositionOption[];
  managers: PersonnelChangeEmployeeOption[];
  jobLevels: PersonnelChangeJobLevelOption[];
};

const emptyLookupState: LookupState = {
  employees: [],
  departments: [],
  positions: [],
  managers: [],
  jobLevels: [],
};

export const usePersonnelChangeLookups = () => {
  const [state, setState] = useState<LookupState>(emptyLookupState);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;

    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        const [employees, departments, positions, managers, jobLevels] =
          await Promise.all([
            personnelChangeApi.getEmployeeOptions(),
            personnelChangeApi.getDepartmentOptions(),
            personnelChangeApi.getPositionOptions(),
            personnelChangeApi.getManagerOptions(),
            personnelChangeApi.getJobLevelOptions(),
          ]);

        if (!mounted) return;
        setState({
          employees: employees.data ?? [],
          departments: departments.data ?? [],
          positions: positions.data ?? [],
          managers: managers.data ?? [],
          jobLevels: jobLevels.data ?? [],
        });
      } catch (err) {
        if (!mounted) return;
        setError(err instanceof Error ? err.message : "Không thể tải dữ liệu lựa chọn.");
      } finally {
        if (mounted) setLoading(false);
      }
    };

    void load();
    return () => {
      mounted = false;
    };
  }, []);

  return useMemo(
    () => ({
      ...state,
      loading,
      error,
    }),
    [error, loading, state],
  );
};

export const useEmployeePersonnelChangeLookups = (employeeId?: number | null) => {
  const [penalties, setPenalties] = useState<PersonnelChangePenaltyOption[]>([]);
  const [performanceReviews, setPerformanceReviews] = useState<PersonnelChangePerformanceReviewOption[]>([]);
  const [contracts, setContracts] = useState<PersonnelChangeContractOption[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;

    if (!employeeId) {
      setPenalties([]);
      setPerformanceReviews([]);
      setContracts([]);
      setError(null);
      return () => {
        mounted = false;
      };
    }

    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        const [penaltyRes, reviewRes, contractRes] = await Promise.all([
          personnelChangeApi.getEmployeePenaltyOptions(employeeId),
          personnelChangeApi.getEmployeePerformanceReviewOptions(employeeId),
          personnelChangeApi.getEmployeeContractOptions(employeeId),
        ]);

        if (!mounted) return;
        setPenalties(penaltyRes.data ?? []);
        setPerformanceReviews(reviewRes.data ?? []);
        setContracts(contractRes.data ?? []);
      } catch (err) {
        if (!mounted) return;
        setError(err instanceof Error ? err.message : "Không thể tải dữ liệu liên quan của nhân sự.");
      } finally {
        if (mounted) setLoading(false);
      }
    };

    void load();
    return () => {
      mounted = false;
    };
  }, [employeeId]);

  return useMemo(
    () => ({
      penalties,
      performanceReviews,
      contracts,
      loading,
      error,
    }),
    [contracts, error, loading, penalties, performanceReviews],
  );
};
