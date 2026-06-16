using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PayrollAllowances;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.PayrollAllowances
{
    public class PayrollRepository : IPayrollRepository
    {
        private readonly MyDbContext _context;

        public PayrollRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<List<AttendanceSummary>> GetAttendanceInputsAsync(byte month, short year, CancellationToken ct = default)
        {
            return await _context.AttendanceSummaries
                .Include(s => s.Employee)
                    .ThenInclude(e => e.Department)
                .Include(s => s.Employee)
                    .ThenInclude(e => e.Position)
                .Include(s => s.Employee)
                    .ThenInclude(e => e.JobLevel)
                .Where(s => s.Month == month &&
                            s.Year == year &&
                            s.IsPayrollLocked &&
                            s.ApprovalStatus == AttendancePayrollApprovalStatus.Locked)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<AttendanceDailySummary>> GetApprovedDailySummariesAsync(IEnumerable<int> employeeIds, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default)
        {
            var ids = employeeIds.Distinct().ToList();
            if (ids.Count == 0) return new List<AttendanceDailySummary>();

            return await _context.AttendanceDailySummaries
                .Where(s => ids.Contains(s.EmployeeId) &&
                            s.WorkDate.Date >= periodStart.Date &&
                            s.WorkDate.Date <= periodEnd.Date &&
                            (s.ApprovalStatus == AttendancePayrollApprovalStatus.Approved ||
                             s.ApprovalStatus == AttendancePayrollApprovalStatus.Locked))
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<Contract>> GetActiveContractsAsync(IEnumerable<int> employeeIds, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default)
        {
            var ids = employeeIds.Distinct().ToList();
            return await _context.Contracts
                .Include(c => c.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(c => c.Employee)
                    .ThenInclude(e => e!.Position)
                .Include(c => c.Employee)
                    .ThenInclude(e => e!.JobLevel)
                .Where(c => c.EmployeeId.HasValue &&
                            ids.Contains(c.EmployeeId.Value) &&
                            c.Status == ContractStatus.Active &&
                            c.StartDate.Date <= periodEnd.Date &&
                            (!c.EndDate.HasValue || c.EndDate.Value.Date >= periodStart.Date))
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<EmployeeAllowance>> GetEmployeeAllowancesAsync(IEnumerable<int> employeeIds, CancellationToken ct = default)
        {
            var ids = employeeIds.Distinct().ToList();
            return await _context.EmployeeAllowances
                .Include(a => a.AllowanceType)
                .Where(a => a.EmployeeId.HasValue && ids.Contains(a.EmployeeId.Value))
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<EmployeeSalaryComponent>> GetEmployeeSalaryComponentsAsync(IEnumerable<int> employeeIds, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default)
        {
            var ids = employeeIds.Distinct().ToList();
            return await _context.EmployeeSalaryComponents
                .Include(c => c.SalaryComponentType)
                .Where(c => ids.Contains(c.EmployeeId) &&
                            c.IsActive &&
                            c.EffectiveFrom.Date <= periodEnd.Date &&
                            (!c.EffectiveTo.HasValue || c.EffectiveTo.Value.Date >= periodStart.Date) &&
                            c.SalaryComponentType.IsActive &&
                            c.SalaryComponentType.EffectiveFrom.Date <= periodEnd.Date &&
                            (!c.SalaryComponentType.EffectiveTo.HasValue || c.SalaryComponentType.EffectiveTo.Value.Date >= periodStart.Date))
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<SalaryComponentType>> GetActiveSalaryComponentTypesAsync(DateTime effectiveDate, CancellationToken ct = default)
        {
            return await _context.SalaryComponentTypes
                .Where(type => type.IsActive &&
                               type.Status == PolicyVersionStatus.Active &&
                               type.EffectiveFrom.Date <= effectiveDate.Date &&
                               (!type.EffectiveTo.HasValue || type.EffectiveTo.Value.Date >= effectiveDate.Date))
                .OrderBy(type => type.ComponentGroup)
                .ThenBy(type => type.Name)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<PerformanceReview>> GetPerformanceReviewsAsync(IEnumerable<int> employeeIds, string period, CancellationToken ct = default)
        {
            var ids = employeeIds.Distinct().ToList();
            return await _context.PerformanceReviews
                .Include(r => r.Details)
                .Where(r => ids.Contains(r.EmployeeId) &&
                            r.Period == period &&
                            (r.Status == ReviewStatus.Evaluated ||
                             r.Status == ReviewStatus.AutoEvaluated ||
                             r.Status == ReviewStatus.Approved))
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<PayrollFormula>> GetApprovedPayrollFormulasAsync(DateTime effectiveDate, CancellationToken ct = default)
        {
            return await _context.PayrollFormulas
                .Include(f => f.Lines.OrderBy(l => l.CalculationOrder))
                    .ThenInclude(l => l.SalaryComponentType)
                .Where(f => f.IsActive &&
                            (f.Status == FormulaStatus.Approved || f.Status == FormulaStatus.Active) &&
                            f.EffectiveFrom.Date <= effectiveDate.Date &&
                            (!f.EffectiveTo.HasValue || f.EffectiveTo.Value.Date >= effectiveDate.Date))
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<TaxConfig?> GetActiveTaxConfigAsync(DateTime effectiveDate, CancellationToken ct = default)
        {
            return await _context.TaxConfigs
                .Where(t => t.IsActive &&
                            t.Status != PolicyVersionStatus.Draft &&
                            t.EffectiveFrom.Date <= effectiveDate.Date &&
                            (!t.EffectiveTo.HasValue || t.EffectiveTo.Value.Date >= effectiveDate.Date))
                .OrderByDescending(t => t.EffectiveFrom)
                .ThenByDescending(t => t.Version)
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);
        }

        public async Task<List<PITTaxBracket>> GetActivePitTaxBracketsAsync(DateTime effectiveDate, CancellationToken ct = default)
        {
            var selectedVersion = await _context.PITTaxBrackets
                .Where(t => t.IsActive &&
                            t.Status != PolicyVersionStatus.Draft &&
                            t.EffectiveFrom.Date <= effectiveDate.Date &&
                            (!t.EffectiveTo.HasValue || t.EffectiveTo.Value.Date >= effectiveDate.Date))
                .OrderByDescending(t => t.EffectiveFrom)
                .ThenByDescending(t => t.Version)
                .Select(t => new { t.Code, t.Version, t.VersionCode, t.EffectiveFrom })
                .FirstOrDefaultAsync(ct);

            if (selectedVersion == null) return new List<PITTaxBracket>();

            return await _context.PITTaxBrackets
                .Where(t => t.IsActive &&
                            t.Status != PolicyVersionStatus.Draft &&
                            t.Code == selectedVersion.Code &&
                            t.Version == selectedVersion.Version &&
                            t.EffectiveFrom == selectedVersion.EffectiveFrom &&
                            t.VersionCode == selectedVersion.VersionCode)
                .OrderBy(t => t.Level)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<InsuranceConfig?> GetActiveInsuranceConfigAsync(DateTime effectiveDate, CancellationToken ct = default)
        {
            return await _context.InsuranceConfigs
                .Where(i => i.IsActive &&
                            i.Status != PolicyVersionStatus.Draft &&
                            i.EffectiveFrom.Date <= effectiveDate.Date &&
                            (!i.EffectiveTo.HasValue || i.EffectiveTo.Value.Date >= effectiveDate.Date))
                .OrderByDescending(i => i.EffectiveFrom)
                .ThenByDescending(i => i.Version)
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);
        }

        public async Task<List<MonthlyInsuranceStatus>> GetMonthlyInsuranceStatusesAsync(IEnumerable<int> employeeIds, byte month, short year, CancellationToken ct = default)
        {
            var ids = employeeIds.Distinct().ToList();
            if (ids.Count == 0) return new List<MonthlyInsuranceStatus>();

            return await _context.MonthlyInsuranceStatuses
                .Where(s => ids.Contains(s.EmployeeId) && s.Month == month && s.Year == year)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<PayrollAdjustment>> GetApprovedPayrollAdjustmentsAsync(IEnumerable<int> employeeIds, byte month, short year, CancellationToken ct = default)
        {
            var ids = employeeIds.Distinct().ToList();
            if (ids.Count == 0) return new List<PayrollAdjustment>();

            return await _context.PayrollAdjustments
                .Where(a => ids.Contains(a.EmployeeId) &&
                            a.RecognizedMonth == month &&
                            a.RecognizedYear == year &&
                            a.Status == PayrollAdjustmentStatus.Approved)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<PayrollAdjustment>> GetPayrollAdjustmentsAsync(byte month, short year, CancellationToken ct = default)
        {
            return await _context.PayrollAdjustments
                .Include(a => a.Employee)
                .Where(a => a.RecognizedMonth == month && a.RecognizedYear == year)
                .OrderBy(a => a.Employee.FullName)
                .ThenBy(a => a.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task AddPayrollAdjustmentAsync(PayrollAdjustment adjustment, CancellationToken ct = default)
        {
            await _context.PayrollAdjustments.AddAsync(adjustment, ct);
        }

        public async Task<List<OvertimeRateConfig>> GetActiveOvertimeRateConfigsAsync(DateTime effectiveDate, CancellationToken ct = default)
        {
            var configs = await _context.OvertimeRateConfigs
                .Where(o => o.IsActive &&
                            o.Status != PolicyVersionStatus.Draft &&
                            o.EffectiveFrom.Date <= effectiveDate.Date &&
                            (!o.EffectiveTo.HasValue || o.EffectiveTo.Value.Date >= effectiveDate.Date))
                .OrderBy(o => o.OvertimeType)
                .ThenByDescending(o => o.EffectiveFrom)
                .ThenByDescending(o => o.Version)
                .AsNoTracking()
                .ToListAsync(ct);

            return configs
                .GroupBy(o => o.OvertimeType)
                .Select(g => g
                    .OrderByDescending(o => o.EffectiveFrom)
                    .ThenByDescending(o => o.Version)
                    .First())
                .OrderBy(o => o.OvertimeType)
                .ToList();
        }

        public async Task<List<PayrollPolicy>> GetActivePayrollPoliciesAsync(PayrollPolicyType policyType, DateTime effectiveDate, CancellationToken ct = default)
        {
            return await _context.PayrollPolicies
                .Where(p => p.PolicyType == policyType &&
                            p.IsActive &&
                            p.Status != PolicyVersionStatus.Draft &&
                            p.EffectiveFrom.Date <= effectiveDate.Date &&
                            (!p.EffectiveTo.HasValue || p.EffectiveTo.Value.Date >= effectiveDate.Date))
                .OrderByDescending(p => p.EffectiveFrom)
                .ThenByDescending(p => p.Version)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<WorkCalendarConfig>> GetWorkCalendarConfigsAsync(byte month, short year, CancellationToken ct = default)
        {
            return await _context.WorkCalendarConfigs
                .Include(c => c.Department)
                .Include(c => c.CompanyCalendar)
                .Where(c => c.Month == month &&
                            c.Year == year &&
                            c.Status != PolicyVersionStatus.Draft)
                .OrderBy(c => c.DeptId)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<EmploymentServicePeriod>> GetEmploymentServicePeriodsAsync(IEnumerable<int> employeeIds, DateTime periodEnd, CancellationToken ct = default)
        {
            var ids = employeeIds.Distinct().ToList();
            if (ids.Count == 0) return new List<EmploymentServicePeriod>();

            return await _context.EmploymentServicePeriods
                .Where(p => ids.Contains(p.EmployeeId) &&
                            p.PeriodStart.Date <= periodEnd.Date)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<PositionJobLevelPolicy>> GetPositionJobLevelPoliciesAsync(IEnumerable<int> positionIds, IEnumerable<int> jobLevelIds, DateTime effectiveDate, CancellationToken ct = default)
        {
            var positions = positionIds.Where(id => id > 0).Distinct().ToList();
            var levels = jobLevelIds.Where(id => id > 0).Distinct().ToList();
            if (positions.Count == 0 || levels.Count == 0) return new List<PositionJobLevelPolicy>();

            return await _context.PositionJobLevelPolicies
                .Include(p => p.Position)
                .Include(p => p.JobLevel)
                .Where(p => positions.Contains(p.PositionId) &&
                            levels.Contains(p.JobLevelId) &&
                            p.IsActive &&
                            p.EffectiveFrom.Date <= effectiveDate.Date &&
                            (!p.EffectiveTo.HasValue || p.EffectiveTo.Value.Date >= effectiveDate.Date))
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<Dictionary<int, int>> GetActiveDependentCountsAsync(IEnumerable<int> employeeIds, DateTime periodEnd, CancellationToken ct = default)
        {
            var ids = employeeIds.Distinct().ToList();
            return await _context.Dependents
                .Where(d => d.EmployeeId.HasValue &&
                            ids.Contains(d.EmployeeId.Value) &&
                            d.IsActive &&
                            d.ValidFrom.Date <= periodEnd.Date &&
                            (!d.ValidTo.HasValue || d.ValidTo.Value.Date >= periodEnd.Date))
                .GroupBy(d => d.EmployeeId!.Value)
                .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EmployeeId, x => x.Count, ct);
        }

        public async Task<List<OvertimeSegment>> GetOvertimeSegmentsAsync(IEnumerable<int> employeeIds, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default)
        {
            var ids = employeeIds.Distinct().ToList();
            return await _context.OvertimeSegments
                .Include(s => s.OvertimeRequest)
                .Where(s => ids.Contains(s.OvertimeRequest.EmployeeId) &&
                            s.SegmentStartAt < periodEnd &&
                            s.SegmentEndAt >= periodStart &&
                            (s.OvertimeRequest.Status == OvertimeRequestStatus.Reconciled ||
                             s.OvertimeRequest.Status == OvertimeRequestStatus.PayrollLocked ||
                             s.OvertimeRequest.Status == OvertimeRequestStatus.Approved))
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<ExternalTimesheetLine>> GetApprovedExternalTimesheetLinesAsync(IEnumerable<int> employeeIds, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default)
        {
            var ids = employeeIds.Distinct().ToList();
            if (ids.Count == 0) return new List<ExternalTimesheetLine>();

            return await _context.ExternalTimesheetLines
                .Include(l => l.Import)
                .Where(l => l.CollaboratorEmployeeId.HasValue &&
                            ids.Contains(l.CollaboratorEmployeeId.Value) &&
                            l.WorkDate.Date >= periodStart.Date &&
                            l.WorkDate.Date <= periodEnd.Date &&
                            l.Import.Status == ExternalTimesheetImportStatus.Approved)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<ExternalTimesheetLine>> GetApprovedExternalTimesheetLinesAsync(DateTime periodStart, DateTime periodEnd, CancellationToken ct = default)
        {
            return await _context.ExternalTimesheetLines
                .Include(l => l.Import)
                .Include(l => l.CollaboratorEmployee)
                    .ThenInclude(e => e!.Department)
                .Include(l => l.CollaboratorEmployee)
                    .ThenInclude(e => e!.Position)
                .Include(l => l.CollaboratorEmployee)
                    .ThenInclude(e => e!.JobLevel)
                .Where(l => l.CollaboratorEmployeeId.HasValue &&
                            l.WorkDate.Date >= periodStart.Date &&
                            l.WorkDate.Date <= periodEnd.Date &&
                            l.Import.Status == ExternalTimesheetImportStatus.Approved)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<ProjectBonusImportLine>> GetApprovedProjectBonusLinesAsync(byte month, short year, CancellationToken ct = default)
        {
            return await _context.ProjectBonusImportLines
                .Include(l => l.Batch)
                .Include(l => l.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(l => l.Employee)
                    .ThenInclude(e => e!.Position)
                .Include(l => l.Employee)
                    .ThenInclude(e => e!.JobLevel)
                .Where(l => l.EmployeeId.HasValue &&
                            l.Batch.PeriodMonth == month &&
                            l.Batch.PeriodYear == year &&
                            l.Batch.Status == ProjectBonusImportStatus.Approved &&
                            l.ValidationStatus == ProjectBonusLineValidationStatus.Valid)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<ProjectBonusImportLine>> GetApprovedProjectBonusLinesAsync(IEnumerable<int> employeeIds, byte month, short year, CancellationToken ct = default)
        {
            var ids = employeeIds.Distinct().ToList();
            if (ids.Count == 0) return new List<ProjectBonusImportLine>();

            return await _context.ProjectBonusImportLines
                .Include(l => l.Batch)
                .Where(l => l.EmployeeId.HasValue &&
                            ids.Contains(l.EmployeeId.Value) &&
                            l.Batch.PeriodMonth == month &&
                            l.Batch.PeriodYear == year &&
                            l.Batch.Status == ProjectBonusImportStatus.Approved &&
                            l.ValidationStatus == ProjectBonusLineValidationStatus.Valid)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<bool> HasLockedPayrollAsync(byte month, short year, CancellationToken ct = default)
        {
            return await _context.Payrolls.AnyAsync(p =>
                p.Month == month &&
                p.Year == year &&
                (p.Status == PayrollStatus.Locked || p.Status == PayrollStatus.Finalized || p.Status == PayrollStatus.Paid), ct);
        }

        public async Task ReplaceDraftsAsync(byte month, short year, IEnumerable<Payroll> payrolls, CancellationToken ct = default)
        {
            var replaceable = await _context.Payrolls
                .Include(p => p.Details)
                .Include(p => p.ContractSegments)
                .Where(p => p.Month == month &&
                            p.Year == year &&
                            p.Status != PayrollStatus.Locked &&
                            p.Status != PayrollStatus.Finalized &&
                            p.Status != PayrollStatus.Paid)
                .ToListAsync(ct);

            if (replaceable.Count > 0)
                _context.Payrolls.RemoveRange(replaceable);

            await _context.Payrolls.AddRangeAsync(payrolls, ct);
        }

        public async Task<List<Payroll>> GetByPeriodAsync(byte month, short year, CancellationToken ct = default)
        {
            return await BuildSlipQuery()
                .Where(p => p.Month == month && p.Year == year)
                .OrderBy(p => p.Employee!.FullName)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<Payroll>> GetTrackedByPeriodAsync(byte month, short year, CancellationToken ct = default)
        {
            return await BuildSlipQuery()
                .Where(p => p.Month == month && p.Year == year)
                .OrderBy(p => p.Employee!.FullName)
                .ToListAsync(ct);
        }

        public async Task<List<Payroll>> GetByStatusAsync(PayrollStatus status, CancellationToken ct = default)
        {
            return await BuildSlipQuery()
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.Year)
                .ThenByDescending(p => p.Month)
                .ThenBy(p => p.Employee!.FullName)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<Payroll>> GetByDepartmentPeriodAsync(int deptId, byte month, short year, CancellationToken ct = default)
        {
            return await BuildSlipQuery()
                .Where(p => p.Month == month && p.Year == year && p.Employee != null && p.Employee.DeptId == deptId)
                .OrderBy(p => p.Employee!.FullName)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<List<Payroll>> GetByEmployeePeriodAsync(int employeeId, byte month, short year, CancellationToken ct = default)
        {
            return await BuildSlipQuery()
                .Where(p => p.Month == month && p.Year == year && p.EmployeeId == employeeId)
                .OrderByDescending(p => p.Id)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<Payroll?> GetDetailAsync(int id, CancellationToken ct = default)
        {
            return await BuildSlipQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, ct);
        }

        public async Task<List<Payroll>> GetDetailsByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
        {
            var list = ids.Distinct().ToList();
            return await BuildSlipQuery()
                .Where(p => list.Contains(p.Id))
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public void UpdateRange(IEnumerable<Payroll> payrolls)
        {
            _context.Payrolls.UpdateRange(payrolls);
        }

        private IQueryable<Payroll> BuildSlipQuery()
        {
            return _context.Payrolls
                .Include(p => p.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(p => p.Employee)
                    .ThenInclude(e => e!.Position)
                .Include(p => p.Details)
                .Include(p => p.ContractSegments);
        }
    }
}
