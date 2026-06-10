import { useMemo, useState } from "react";
import { useNotification } from "../../../core/context/NotificationContext";
import type { ExternalTimesheetImportState, ExternalTimesheetLinePreview } from "../types/payroll";

const splitCsvLine = (line: string) => line.split(",").map((cell) => cell.trim().replace(/^"|"$/g, ""));

const parseNumber = (value: string) => {
  const numberValue = Number(value.replace(/\s/g, "").replace(",", "."));
  return Number.isFinite(numberValue) ? numberValue : 0;
};

export const useExternalTimesheetImport = (month: number, year: number) => {
  const { triggerAlert } = useNotification();
  const [sourceSystem, setSourceSystem] = useState("Timesheet ngoài");
  const [fileName, setFileName] = useState("");
  const [lines, setLines] = useState<ExternalTimesheetLinePreview[]>([]);

  const totals = useMemo(
    () =>
      lines.reduce(
        (sum, line) => ({
          totalHours: sum.totalHours + line.approvedHours,
          totalAmount: sum.totalAmount + line.amount,
        }),
        { totalHours: 0, totalAmount: 0 },
      ),
    [lines],
  );

  const importState: ExternalTimesheetImportState = {
    fileName,
    sourceSystem,
    importMonth: month,
    importYear: year,
    lines,
    totalHours: totals.totalHours,
    totalAmount: totals.totalAmount,
  };

  const parseFile = async (file: File) => {
    setFileName(file.name);
    const content = await file.text();
    const rows = content
      .split(/\r?\n/)
      .map((line) => line.trim())
      .filter(Boolean);

    if (rows.length <= 1) {
      setLines([]);
      triggerAlert(
        "warning",
        "File chưa có dữ liệu",
        "Vui lòng dùng file CSV có dòng tiêu đề và dữ liệu giờ công.",
      );
      return;
    }

    const parsedLines = rows.slice(1).map((row, index) => {
      const cells = splitCsvLine(row);
      const approvedHours = parseNumber(cells[5] ?? "0");
      const hourlyRate = parseNumber(cells[6] ?? "0");

      return {
        rowNumber: index + 2,
        collaboratorCode: cells[0] ?? "",
        collaboratorName: cells[1] ?? "",
        workDate: cells[2] ?? "",
        projectCode: cells[3] ?? "",
        taskCode: cells[4] ?? "",
        approvedHours,
        hourlyRate,
        amount: approvedHours * hourlyRate,
        note: cells[7] ?? "",
      };
    });

    setLines(parsedLines);
    triggerAlert(
      "success",
      "Đã đọc file giờ công CTV",
      "Dữ liệu đang ở trạng thái xem trước.",
    );
  };

  const reset = () => {
    setFileName("");
    setLines([]);
  };

  return {
    sourceSystem,
    setSourceSystem,
    importState,
    parseFile,
    reset,
  };
};
