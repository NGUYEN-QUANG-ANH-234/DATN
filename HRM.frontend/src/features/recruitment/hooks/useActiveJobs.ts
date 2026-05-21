import { useState, useEffect } from "react";
import { candidateApi } from "../api/candidateApi";
import type { ActiveJob } from "../types/candidate";

export const useActiveJobs = () => {
  const [jobs, setJobs] = useState<ActiveJob[]>([]);
  const [loadingJobs, setLoadingJobs] = useState(true);

  useEffect(() => {
    const fetchJobs = async () => {
      try {
        const res = await candidateApi.getActiveJobs();
        setJobs(res.data || []);
      } catch (error) {
        console.error("Lỗi khi tải danh sách việc làm:", error);
      } finally {
        setLoadingJobs(false);
      }
    };
    fetchJobs();
  }, []);

  return { jobs, loadingJobs };
};
