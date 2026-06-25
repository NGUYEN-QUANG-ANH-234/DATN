using System.Text.Json;
using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Services;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PayrollAllowances;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;

namespace HRM.backend.src.HRM.Application.Services.TimeAttendance
{
    public class OvertimeReconciliationService : IOvertimeReconciliationService
    {
        private const string WeekdayPolicyCode = "OT_WEEKDAY";
        private const string WeekendPolicyCode = "OT_WEEKEND";
        private const string HolidayPolicyCode = "OT_HOLIDAY";

        private readonly IPayrollPolicyRepository _policyRepo;
        private readonly IPayrollRepository _payrollRepo;
        private readonly IBaseRepository<OvertimeSegment> _segmentRepo;
        private readonly ICompanyCalendarRepository _companyCalendarRepo;
        private readonly IWorkCalendarConfigRepository _workCalendarConfigRepo;

        public OvertimeReconciliationService(
            IPayrollPolicyRepository policyRepo,
            IPayrollRepository payrollRepo,
            IBaseRepository<OvertimeSegment> segmentRepo,
            ICompanyCalendarRepository companyCalendarRepo,
            IWorkCalendarConfigRepository workCalendarConfigRepo)
        {
            _policyRepo = policyRepo;
            _payrollRepo = payrollRepo;
            _segmentRepo = segmentRepo;
            _companyCalendarRepo = companyCalendarRepo;
            _workCalendarConfigRepo = workCalendarConfigRepo;
        }

        public async Task ReconcileAsync(OvertimeRequest request, AttendanceLog? attendanceLog, CancellationToken ct = default)
        {
            if (request.IsPayrollLocked)
                return;

            if (request.Segments.Any())
            {
                _segmentRepo.RemoveRange(request.Segments.ToList());
                request.Segments.Clear();
            }

            request.ActualOtMinutes = 0;
            request.ReconciledAt = DateTime.UtcNow;

            if (attendanceLog?.CheckIn == null || attendanceLog.CheckOut == null)
            {
                request.Status = OvertimeRequestStatus.Approved;
                return;
            }

            var actualStart = Max(request.StartAt, attendanceLog.CheckIn.Value);
            var actualEnd = Min(request.EndAt, attendanceLog.CheckOut.Value);
            if (actualEnd <= actualStart)
            {
                request.Status = OvertimeRequestStatus.Reconciled;
                return;
            }

            var policies = await _policyRepo.GetByFilterAsync(PayrollPolicyType.Overtime, false, ct);
            var rateConfigs = await _payrollRepo.GetActiveOvertimeRateConfigsAsync(actualEnd, ct);
            var workCalendarConfigs = await LoadWorkCalendarConfigsAsync(request, actualStart, actualEnd, ct);
            var companyCalendars = await LoadCompanyCalendarsAsync(actualStart, actualEnd, workCalendarConfigs, ct);
            var segments = BuildSegments(request, actualStart, actualEnd, policies, rateConfigs, companyCalendars, workCalendarConfigs);
            await _segmentRepo.AddRangeAsync(segments, ct);
            foreach (var segment in segments)
                request.Segments.Add(segment);

            request.ActualOtMinutes = segments.Sum(s => s.Minutes);
            request.Status = OvertimeRequestStatus.Reconciled;
        }

        private static List<OvertimeSegment> BuildSegments(
            OvertimeRequest request,
            DateTime actualStart,
            DateTime actualEnd,
            IReadOnlyCollection<PayrollPolicy> policies,
            IReadOnlyCollection<Core.Entities.PayrollAllowances.OvertimeRateConfig> rateConfigs,
            IReadOnlyCollection<CompanyCalendar> companyCalendars,
            IReadOnlyCollection<WorkCalendarConfig> workCalendarConfigs)
        {
            var segments = new List<OvertimeSegment>();
            var cursor = actualStart;

            while (cursor < actualEnd)
            {
                var dayContext = ResolveDayContext(cursor, request.Employee?.DeptId, companyCalendars, workCalendarConfigs);
                var nextBoundary = NextOvertimeBoundary(cursor, dayContext);
                var segmentEnd = Min(nextBoundary, actualEnd);
                var overtimeType = ResolveOvertimeType(cursor, dayContext);
                var policyCode = ResolvePolicyCode(dayContext);
                var config = ResolveRateConfig(rateConfigs, overtimeType, cursor);
                var policy = ResolvePolicy(policies, policyCode, cursor);
                var rate = ResolveRateMultiplier(config, policy, policyCode);

                segments.Add(new OvertimeSegment
                {
                    OvertimeRequestId = request.Id,
                    SegmentStartAt = cursor,
                    SegmentEndAt = segmentEnd,
                    Minutes = (int)Math.Floor((segmentEnd - cursor).TotalMinutes),
                    OvertimeType = overtimeType,
                    PolicyCode = config?.Code ?? policyCode,
                    RateMultiplierSnapshot = rate,
                    PolicySnapshotJson = BuildPolicySnapshot(config, policy, policyCode, rate, dayContext)
                });

                cursor = segmentEnd;
            }

            return segments.Where(s => s.Minutes > 0).ToList();
        }

        private static PayrollPolicy? ResolvePolicy(
            IEnumerable<PayrollPolicy> policies,
            string policyCode,
            DateTime effectiveAt)
        {
            return policies
                .Where(p => string.Equals(p.Code, policyCode, StringComparison.OrdinalIgnoreCase) &&
                            p.EffectiveFrom <= effectiveAt &&
                            (!p.EffectiveTo.HasValue || p.EffectiveTo.Value >= effectiveAt))
                .OrderByDescending(p => p.EffectiveFrom)
                .ThenByDescending(p => p.Version)
                .FirstOrDefault();
        }

        private static Core.Entities.PayrollAllowances.OvertimeRateConfig? ResolveRateConfig(
            IEnumerable<Core.Entities.PayrollAllowances.OvertimeRateConfig> configs,
            OvertimeType overtimeType,
            DateTime effectiveAt)
        {
            return configs
                .Where(c => c.OvertimeType == overtimeType &&
                            c.EffectiveFrom <= effectiveAt &&
                            (!c.EffectiveTo.HasValue || c.EffectiveTo.Value >= effectiveAt))
                .OrderByDescending(c => c.EffectiveFrom)
                .ThenByDescending(c => c.Version)
                .FirstOrDefault();
        }

        private static decimal ResolveRateMultiplier(
            Core.Entities.PayrollAllowances.OvertimeRateConfig? config,
            PayrollPolicy? policy,
            string policyCode)
        {
            if (config != null)
                return CalculateRateMultiplier(config);

            if (policy?.RatePercent > 0)
                return decimal.Round(policy.RatePercent.Value / 100m, 4);

            throw new InvalidOperationException($"Thiếu cấu hình hệ số làm thêm cho chính sách {policyCode}.");
        }

        private static decimal CalculateRateMultiplier(Core.Entities.PayrollAllowances.OvertimeRateConfig config)
        {
            var nightOvertimeExtra = config.BaseMultiplier * config.NightOvertimeExtraRate;
            return decimal.Round(config.BaseMultiplier + config.NightAllowanceRate + nightOvertimeExtra, 4);
        }

        private static string BuildPolicySnapshot(
            Core.Entities.PayrollAllowances.OvertimeRateConfig? config,
            PayrollPolicy? policy,
            string policyCode,
            decimal rateMultiplier,
            OvertimeDayContext dayContext)
        {
            return JsonSerializer.Serialize(new
            {
                Code = config?.Code ?? policy?.Code ?? policyCode,
                Name = policy?.Name ?? config?.Code ?? policyCode,
                ConfigVersion = config?.Version,
                config?.OvertimeType,
                config?.BaseMultiplier,
                config?.NightAllowanceRate,
                config?.NightOvertimeExtraRate,
                NightOvertimeExtraAmount = config == null ? (decimal?)null : decimal.Round(config.BaseMultiplier * config.NightOvertimeExtraRate, 4),
                Formula = config == null ? "RatePercent / 100" : "BaseMultiplier + NightAllowanceRate + (BaseMultiplier * NightOvertimeExtraRate)",
                PolicyVersion = policy?.Version,
                policy?.RatePercent,
                RateMultiplier = rateMultiplier,
                DayClassification = dayContext.Classification,
                DaySource = dayContext.Source,
                CompanyCalendarId = dayContext.CompanyCalendar?.Id,
                CompanyCalendarVersion = dayContext.CompanyCalendar?.VersionCode,
                CompanyDayType = dayContext.CompanyDay?.DayType,
                CompanyDayName = dayContext.CompanyDay?.Name,
                WorkCalendarConfigId = dayContext.WorkCalendarConfig?.Id,
                WorkingDaysOfWeek = dayContext.WorkCalendarConfig?.WorkingDaysOfWeek,
                HolidayWorkingStartTime = dayContext.WorkCalendarConfig?.HolidayWorkingStartTime,
                HolidayWorkingEndTime = dayContext.WorkCalendarConfig?.HolidayWorkingEndTime
            });
        }

        private static DateTime NextOvertimeBoundary(DateTime cursor, OvertimeDayContext dayContext)
        {
            var boundaries = new List<DateTime>
            {
                cursor.Date.AddHours(6),
                cursor.Date.AddHours(22),
                cursor.Date.AddDays(1)
            };

            if (dayContext.WorkCalendarConfig?.HolidayWorkingStartTime.HasValue == true)
                boundaries.Add(cursor.Date.Add(dayContext.WorkCalendarConfig.HolidayWorkingStartTime.Value));
            if (dayContext.WorkCalendarConfig?.HolidayWorkingEndTime.HasValue == true)
                boundaries.Add(cursor.Date.Add(dayContext.WorkCalendarConfig.HolidayWorkingEndTime.Value));

            return boundaries
                .Where(boundary => boundary > cursor)
                .OrderBy(boundary => boundary)
                .FirstOrDefault(cursor.Date.AddDays(1));
        }

        private async Task<List<WorkCalendarConfig>> LoadWorkCalendarConfigsAsync(OvertimeRequest request, DateTime start, DateTime end, CancellationToken ct)
        {
            var deptId = request.Employee?.DeptId;
            if (!deptId.HasValue)
                return new List<WorkCalendarConfig>();

            var configs = new List<WorkCalendarConfig>();
            for (var month = new DateTime(start.Year, start.Month, 1); month <= end; month = month.AddMonths(1))
            {
                var config = await _workCalendarConfigRepo.GetByDeptPeriodAsync(deptId.Value, (byte)month.Month, (short)month.Year, ct);
                if (config != null)
                    configs.Add(config);
            }

            return configs;
        }

        private async Task<List<CompanyCalendar>> LoadCompanyCalendarsAsync(
            DateTime start,
            DateTime end,
            IReadOnlyCollection<WorkCalendarConfig> workCalendarConfigs,
            CancellationToken ct)
        {
            var calendars = new List<CompanyCalendar>();
            var configuredCalendarIds = workCalendarConfigs
                .Where(config => config.CompanyCalendarId.HasValue)
                .Select(config => config.CompanyCalendarId!.Value)
                .Distinct()
                .ToList();

            foreach (var calendarId in configuredCalendarIds)
            {
                var calendar = await _companyCalendarRepo.GetByIdWithDaysAsync(calendarId, ct);
                if (calendar != null)
                    calendars.Add(calendar);
            }

            for (var year = start.Year; year <= end.Year; year++)
            {
                var calendar = await _companyCalendarRepo.GetActiveByYearAsync((short)year, ct);
                if (calendar != null && calendars.All(item => item.Id != calendar.Id))
                    calendars.Add(calendar);
            }

            return calendars;
        }

        private static OvertimeType ResolveOvertimeType(DateTime value, OvertimeDayContext dayContext)
        {
            var night = value.TimeOfDay < TimeSpan.FromHours(6) || value.TimeOfDay >= TimeSpan.FromHours(22);
            if (dayContext.Classification == OvertimeDayClassification.Holiday)
                return night ? OvertimeType.HolidayNight : OvertimeType.Holiday;

            return (dayContext.Classification == OvertimeDayClassification.WeeklyRestDay, night) switch
            {
                (true, true) => OvertimeType.WeekendNight,
                (true, false) => OvertimeType.Weekend,
                (false, true) => OvertimeType.WeekdayNight,
                _ => OvertimeType.Weekday
            };
        }

        private static string ResolvePolicyCode(OvertimeDayContext dayContext)
        {
            return dayContext.Classification switch
            {
                OvertimeDayClassification.Holiday => HolidayPolicyCode,
                OvertimeDayClassification.WeeklyRestDay => WeekendPolicyCode,
                _ => WeekdayPolicyCode
            };
        }

        private static OvertimeDayContext ResolveDayContext(
            DateTime value,
            int? deptId,
            IReadOnlyCollection<CompanyCalendar> companyCalendars,
            IReadOnlyCollection<WorkCalendarConfig> workCalendarConfigs)
        {
            var date = value.Date;
            var workCalendar = workCalendarConfigs.FirstOrDefault(config =>
                config.DeptId == deptId &&
                config.Month == date.Month &&
                config.Year == date.Year);
            var companyCalendar = workCalendar?.CompanyCalendarId.HasValue == true
                ? companyCalendars.FirstOrDefault(calendar => calendar.Id == workCalendar.CompanyCalendarId.Value)
                : companyCalendars.FirstOrDefault(calendar => calendar.Year == date.Year);
            var companyDay = companyCalendar?.Days.FirstOrDefault(day => day.Date.Date == date);
            var departmentHolidayDates = ParseHolidayDates(workCalendar?.HolidayDatesJson);
            var workingDays = ParseWorkingDays(workCalendar?.WorkingDaysOfWeek);

            var isWorkingOverride = companyDay?.IsWorkingDayOverride == true ||
                                    companyDay?.DayType == CompanyCalendarDayType.CompensatoryWorkingDay;
            var isDepartmentHoliday = departmentHolidayDates.Contains(date);
            var isCompanyHoliday = companyDay != null &&
                                   !isWorkingOverride &&
                                   ((companyDay.DayType is CompanyCalendarDayType.PublicHoliday
                                       or CompanyCalendarDayType.CompanyHoliday) ||
                                    companyDay.IsOvertimeHoliday);

            if (isWorkingOverride)
            {
                return new OvertimeDayContext(
                    date,
                    OvertimeDayClassification.NormalWorkingDay,
                    "CompanyCalendarWorkingOverride",
                    companyCalendar,
                    companyDay,
                    workCalendar);
            }

            if (isCompanyHoliday)
            {
                return new OvertimeDayContext(
                    date,
                    OvertimeDayClassification.Holiday,
                    "CompanyCalendarHoliday",
                    companyCalendar,
                    companyDay,
                    workCalendar);
            }

            if (isDepartmentHoliday ||
                companyDay?.DayType is CompanyCalendarDayType.CompensatoryDayOff
                    or CompanyCalendarDayType.SpecialPaidLeave
                    or CompanyCalendarDayType.UnpaidCompanyClosure)
            {
                return new OvertimeDayContext(
                    date,
                    OvertimeDayClassification.WeeklyRestDay,
                    isDepartmentHoliday ? "DepartmentHolidayOverride" : "CompanyCalendarDayOff",
                    companyCalendar,
                    companyDay,
                    workCalendar);
            }

            if (!workingDays.Contains(date.DayOfWeek))
            {
                return new OvertimeDayContext(
                    date,
                    OvertimeDayClassification.WeeklyRestDay,
                    "DepartmentWeeklyRestDay",
                    companyCalendar,
                    companyDay,
                    workCalendar);
            }

            return new OvertimeDayContext(
                date,
                OvertimeDayClassification.NormalWorkingDay,
                workCalendar == null ? "DefaultWorkingWeek" : "DepartmentWorkingCalendar",
                companyCalendar,
                companyDay,
                workCalendar);
        }

        private static HashSet<DayOfWeek> ParseWorkingDays(string? workingDaysOfWeek)
        {
            var defaultWorkingDays = new HashSet<DayOfWeek>
            {
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday
            };

            if (string.IsNullOrWhiteSpace(workingDaysOfWeek))
                return defaultWorkingDays;

            var parsed = new HashSet<DayOfWeek>();
            foreach (var token in workingDaysOfWeek.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(token.Trim(), out var dayNumber))
                {
                    if (dayNumber == 7)
                        parsed.Add(DayOfWeek.Sunday);
                    else if (dayNumber is >= 0 and <= 6)
                        parsed.Add((DayOfWeek)dayNumber);
                    continue;
                }

                if (Enum.TryParse<DayOfWeek>(token.Trim(), true, out var dayOfWeek))
                    parsed.Add(dayOfWeek);
            }

            return parsed.Count > 0 ? parsed : defaultWorkingDays;
        }

        private static HashSet<DateTime> ParseHolidayDates(string? holidayDatesJson)
        {
            if (string.IsNullOrWhiteSpace(holidayDatesJson))
                return new HashSet<DateTime>();

            try
            {
                var dateStrings = JsonSerializer.Deserialize<List<string>>(holidayDatesJson) ?? new List<string>();
                return dateStrings
                    .Where(value => DateTime.TryParse(value, out _))
                    .Select(value => DateTime.Parse(value).Date)
                    .ToHashSet();
            }
            catch (JsonException)
            {
                return holidayDatesJson
                    .Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(value => DateTime.TryParse(value, out _))
                    .Select(value => DateTime.Parse(value).Date)
                    .ToHashSet();
            }
        }

        private static DateTime Max(DateTime left, DateTime right) => left > right ? left : right;
        private static DateTime Min(DateTime left, DateTime right) => left < right ? left : right;

        private enum OvertimeDayClassification
        {
            NormalWorkingDay,
            WeeklyRestDay,
            Holiday
        }

        private sealed record OvertimeDayContext(
            DateTime Date,
            OvertimeDayClassification Classification,
            string Source,
            CompanyCalendar? CompanyCalendar,
            CompanyCalendarDay? CompanyDay,
            WorkCalendarConfig? WorkCalendarConfig);
    }
}
