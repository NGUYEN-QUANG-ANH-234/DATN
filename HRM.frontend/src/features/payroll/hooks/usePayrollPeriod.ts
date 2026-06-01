import { useMemo, useState } from "react";

const currentDate = new Date();

export const usePayrollPeriod = () => {
  const [month, setMonth] = useState(currentDate.getMonth() + 1);
  const [year, setYear] = useState(currentDate.getFullYear());

  const period = useMemo(() => `${String(month).padStart(2, "0")}-${year}`, [month, year]);

  return {
    month,
    year,
    period,
    setMonth,
    setYear,
  };
};
