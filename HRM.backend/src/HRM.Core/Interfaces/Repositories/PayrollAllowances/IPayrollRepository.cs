using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.PayrollAllowances
{
    public interface IPayrollRepository
    {
        Task<List<AttendanceSummary>> GetAttendanceInputsAsync(byte month, short year, CancellationToken ct = default);
        Task<List<AttendanceDailySummary>> GetApprovedDailySummariesAsync(IEnumerable<int> employeeIds, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default);
        Task<List<Contract>> GetActiveContractsAsync(IEnumerable<int> employeeIds, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default);
        Task<List<EmployeeAllowance>> GetEmployeeAllowancesAsync(IEnumerable<int> employeeIds, CancellationToken ct = default);
        Task<List<EmployeeSalaryComponent>> GetEmployeeSalaryComponentsAsync(IEnumerable<int> employeeIds, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default);
        Task<List<SalaryComponentType>> GetActiveSalaryComponentTypesAsync(DateTime effectiveDate, CancellationToken ct = default);
        Task<List<PerformanceReview>> GetPerformanceReviewsAsync(IEnumerable<int> employeeIds, string period, CancellationToken ct = default);
        Task<Dictionary<int, int>> GetActiveDependentCountsAsync(IEnumerable<int> employeeIds, DateTime periodEnd, CancellationToken ct = default);
        Task<List<OvertimeSegment>> GetOvertimeSegmentsAsync(IEnumerable<int> employeeIds, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default);
        Task<List<ExternalTimesheetLine>> GetApprovedExternalTimesheetLinesAsync(DateTime periodStart, DateTime periodEnd, CancellationToken ct = default);
        Task<List<ExternalTimesheetLine>> GetApprovedExternalTimesheetLinesAsync(IEnumerable<int> employeeIds, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default);
        Task<List<ProjectBonusImportLine>> GetApprovedProjectBonusLinesAsync(byte month, short year, CancellationToken ct = default);
        Task<List<ProjectBonusImportLine>> GetApprovedProjectBonusLinesAsync(IEnumerable<int> employeeIds, byte month, short year, CancellationToken ct = default);
        Task<List<PayrollFormula>> GetApprovedPayrollFormulasAsync(DateTime effectiveDate, CancellationToken ct = default);
        Task<TaxConfig?> GetActiveTaxConfigAsync(DateTime effectiveDate, CancellationToken ct = default);
        Task<List<PITTaxBracket>> GetActivePitTaxBracketsAsync(DateTime effectiveDate, CancellationToken ct = default);
        Task<InsuranceConfig?> GetActiveInsuranceConfigAsync(DateTime effectiveDate, CancellationToken ct = default);
        Task<List<MonthlyInsuranceStatus>> GetMonthlyInsuranceStatusesAsync(IEnumerable<int> employeeIds, byte month, short year, CancellationToken ct = default);
        Task<List<PayrollAdjustment>> GetApprovedPayrollAdjustmentsAsync(IEnumerable<int> employeeIds, byte month, short year, CancellationToken ct = default);
        Task<List<PayrollAdjustment>> GetPayrollAdjustmentsAsync(byte month, short year, CancellationToken ct = default);
        Task AddPayrollAdjustmentAsync(PayrollAdjustment adjustment, CancellationToken ct = default);
        Task<List<OvertimeRateConfig>> GetActiveOvertimeRateConfigsAsync(DateTime effectiveDate, CancellationToken ct = default);
        Task<List<OvertimeRateConfig>> GetOvertimeRateConfigsAsync(bool includeInactive = true, CancellationToken ct = default);
        Task<OvertimeRateConfig?> GetOvertimeRateConfigForUpdateAsync(int id, CancellationToken ct = default);
        Task AddOvertimeRateConfigAsync(OvertimeRateConfig config, CancellationToken ct = default);
        void UpdateOvertimeRateConfig(OvertimeRateConfig config);
        Task<List<PayrollPolicy>> GetActivePayrollPoliciesAsync(PayrollPolicyType policyType, DateTime effectiveDate, CancellationToken ct = default);
        Task<List<WorkCalendarConfig>> GetWorkCalendarConfigsAsync(byte month, short year, CancellationToken ct = default);
        Task<List<EmploymentServicePeriod>> GetEmploymentServicePeriodsAsync(IEnumerable<int> employeeIds, DateTime periodEnd, CancellationToken ct = default);
        Task<List<PositionJobLevelPolicy>> GetPositionJobLevelPoliciesAsync(IEnumerable<int> positionIds, IEnumerable<int> jobLevelIds, DateTime effectiveDate, CancellationToken ct = default);
        Task<bool> HasLockedPayrollAsync(byte month, short year, CancellationToken ct = default);
        Task ReplaceDraftsAsync(byte month, short year, IEnumerable<Payroll> payrolls, CancellationToken ct = default);
        Task<List<Payroll>> GetByPeriodAsync(byte month, short year, CancellationToken ct = default);
        Task<List<Payroll>> GetTrackedByPeriodAsync(byte month, short year, CancellationToken ct = default);
        Task<List<Payroll>> GetByStatusAsync(PayrollStatus status, CancellationToken ct = default);
        Task<List<Payroll>> GetByDepartmentPeriodAsync(int deptId, byte month, short year, CancellationToken ct = default);
        Task<List<Payroll>> GetByEmployeePeriodAsync(int employeeId, byte month, short year, CancellationToken ct = default);
        Task<Payroll?> GetDetailAsync(int id, CancellationToken ct = default);
        Task<List<Payroll>> GetDetailsByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);
        void UpdateRange(IEnumerable<Payroll> payrolls);
    }
}
