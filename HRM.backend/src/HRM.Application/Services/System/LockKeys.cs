namespace HRM.backend.src.HRM.Application.Services.System
{
    public static class LockKeys
    {
        public static string PayrollRun(byte month, short year) =>
            $"payroll:run:{year}:{month:00}";

        public static string AttendancePeriod(byte month, short year) =>
            $"attendance:period:{year}:{month:00}";

        public static string Approval(string workflowKey, int referenceId) =>
            $"approval:{Normalize(workflowKey)}:{referenceId}";

        public static string ProjectBonusBatch(int batchId) =>
            $"project-bonus:batch:{batchId}";

        public static string ProjectBonusPeriod(byte month, short year) =>
            $"project-bonus:period:{year}:{month:00}";

        public static string ExternalTimesheetBatch(int batchId) =>
            $"external-timesheet:batch:{batchId}";

        public static string ExternalTimesheetPeriod(byte month, short year) =>
            $"external-timesheet:period:{year}:{month:00}";

        public static string PayrollFormula(int formulaId) =>
            $"payroll-formula:{formulaId}";

        public static string PayrollFormulaCode(string formulaCode) =>
            $"payroll-formula:code:{Normalize(formulaCode)}";

        public static string PersonnelChange(int requestId) =>
            $"personnel-change:{requestId}";

        public static string Contract(int contractId) =>
            $"contract:{contractId}";

        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? "unknown"
                : value.Trim().ToLowerInvariant().Replace(' ', '-');
    }
}
