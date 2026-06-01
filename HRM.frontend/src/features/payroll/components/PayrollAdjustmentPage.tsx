import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { Card } from "../../../components/ui";
import { FeaturePage } from "../../../core/components/FeatureShell";
import { usePayrollAdjustments } from "../hooks/usePayrollAdjustments";
import { usePayrollPeriod } from "../hooks/usePayrollPeriod";
import type { CreatePayrollAdjustmentRequest } from "../types/payroll";
import { createEmptyPayrollAdjustmentForm } from "../utils";
import { PayrollAdjustmentForm } from "./PayrollAdjustmentForm";
import { PayrollAdjustmentTable } from "./PayrollAdjustmentTable";
import { PayrollPeriodFilter } from "./PayrollPeriodFilter";

export const PayrollAdjustmentPage = () => {
  const { month, year, period, setMonth, setYear } = usePayrollPeriod();
  const { loading, saving, adjustments, loadAdjustments, createAdjustment } = usePayrollAdjustments(month, year);
  const [form, setForm] = useState<CreatePayrollAdjustmentRequest>(() =>
    createEmptyPayrollAdjustmentForm(month, year),
  );

  useEffect(() => {
    setForm((current) => ({
      ...current,
      recognizedMonth: month,
      recognizedYear: year,
    }));
  }, [month, year]);

  const updateForm = (patch: Partial<CreatePayrollAdjustmentRequest>) => {
    setForm((current) => ({ ...current, ...patch }));
  };

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const saved = await createAdjustment({
      ...form,
      recognizedMonth: month,
      recognizedYear: year,
      effectiveFromMonth: form.effectiveFromMonth || null,
      effectiveToMonth: form.effectiveToMonth || null,
    });

    if (saved) {
      setForm(createEmptyPayrollAdjustmentForm(month, year));
    }
  };

  return (
    <FeaturePage
      title="Điều chỉnh nghiệp vụ lương"
      description="Ghi nhận truy lĩnh, truy thu, điều chỉnh thuế, bảo hiểm, bồi hoàn hoặc sai sót kỳ trước. Lỗi hiện diện đi qua bảng công và biên bản vi phạm, không tạo khoản tiền trực tiếp tại payroll."
      width="wide"
    >
      <Card title="Kỳ ghi nhận">
        <PayrollPeriodFilter
          month={month}
          year={year}
          loading={loading}
          showExport={false}
          onMonthChange={setMonth}
          onYearChange={setYear}
          onRefresh={loadAdjustments}
        />
      </Card>

      <Card title="Tạo khoản điều chỉnh nghiệp vụ lương">
        <PayrollAdjustmentForm
          form={form}
          period={period}
          saving={saving}
          onChange={updateForm}
          onSubmit={submit}
        />
      </Card>

      <Card title="Danh sách khoản điều chỉnh hợp lệ">
        <PayrollAdjustmentTable adjustments={adjustments} loading={loading} period={period} />
      </Card>
    </FeaturePage>
  );
};
