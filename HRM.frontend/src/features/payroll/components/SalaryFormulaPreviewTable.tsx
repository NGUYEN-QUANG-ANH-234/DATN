import { Badge, DataTable } from "../../../components/ui";
import type { DataTableColumn } from "../../../components/ui";
import type { PayrollFormulaPreviewLine, SalaryFormulaPreviewTableProps } from "../types/payroll";

const yesNo = (value: boolean) => (
  <Badge variant={value ? "success" : "neutral"}>{value ? "Có" : "Không"}</Badge>
);

export const SalaryFormulaPreviewTable = ({ lines }: SalaryFormulaPreviewTableProps) => {
  const columns: Array<DataTableColumn<PayrollFormulaPreviewLine>> = [
    { key: "order", header: "Thứ tự", render: (line) => line.calculationOrder },
    {
      key: "code",
      header: "ComponentCode",
      render: (line) => <span className="font-mono text-xs">{line.componentCode}</span>,
    },
    {
      key: "name",
      header: "Tên khoản",
      render: (line) => (
        <span className="font-semibold text-[var(--hicas-text-main)]">{line.componentName}</span>
      ),
    },
    {
      key: "expression",
      header: "Expression",
      render: (line) => <span className="font-mono text-xs">{line.expression}</span>,
    },
    { key: "gross", header: "Gross", render: (line) => yesNo(line.isGrossComponent) },
    { key: "tax", header: "Thuế", render: (line) => yesNo(line.isTaxable) },
    { key: "insurance", header: "BH", render: (line) => yesNo(line.isInsuranceBased) },
    { key: "deduction", header: "Khoản giảm", render: (line) => yesNo(line.isDeduction) },
  ];

  return (
    <DataTable
      columns={columns}
      data={lines}
      rowKey={(row) => row.componentCode}
      emptyTitle="Chưa có dòng công thức"
    />
  );
};
