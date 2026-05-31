using System.Text.Json;
using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Services;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PayrollAllowances;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;

namespace HRM.backend.src.HRM.Application.Services.TimeAttendance
{
    public class OvertimeReconciliationService : IOvertimeReconciliationService
    {
        private const string WeekdayPolicyCode = "OT_WEEKDAY";
        private const string WeekendPolicyCode = "OT_WEEKEND";

        private readonly IPayrollPolicyRepository _policyRepo;
        private readonly IPayrollRepository _payrollRepo;
        private readonly IBaseRepository<OvertimeSegment> _segmentRepo;

        public OvertimeReconciliationService(
            IPayrollPolicyRepository policyRepo,
            IPayrollRepository payrollRepo,
            IBaseRepository<OvertimeSegment> segmentRepo)
        {
            _policyRepo = policyRepo;
            _payrollRepo = payrollRepo;
            _segmentRepo = segmentRepo;
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
            var segments = BuildSegments(request, actualStart, actualEnd, policies, rateConfigs);
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
            IReadOnlyCollection<Core.Entities.PayrollAllowances.OvertimeRateConfig> rateConfigs)
        {
            var segments = new List<OvertimeSegment>();
            var cursor = actualStart;

            while (cursor < actualEnd)
            {
                var nextBoundary = NextOvertimeBoundary(cursor);
                var segmentEnd = Min(nextBoundary, actualEnd);
                var overtimeType = ResolveOvertimeType(cursor);
                var policyCode = IsWeekend(cursor) ? WeekendPolicyCode : WeekdayPolicyCode;
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
                    PolicySnapshotJson = BuildPolicySnapshot(config, policy, policyCode, rate)
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
                return decimal.Round(config.BaseMultiplier + config.NightAllowanceRate + config.NightOvertimeExtraRate, 4);

            if (policy?.RatePercent > 0)
                return decimal.Round(policy.RatePercent.Value / 100m, 4);

            return policyCode == WeekendPolicyCode ? 1.5m : 1.2m;
        }

        private static string BuildPolicySnapshot(
            Core.Entities.PayrollAllowances.OvertimeRateConfig? config,
            PayrollPolicy? policy,
            string policyCode,
            decimal rateMultiplier)
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
                PolicyVersion = policy?.Version,
                policy?.RatePercent,
                RateMultiplier = rateMultiplier
            });
        }

        private static DateTime NextOvertimeBoundary(DateTime cursor)
        {
            var six = cursor.Date.AddHours(6);
            var twentyTwo = cursor.Date.AddHours(22);

            if (cursor < six) return six;
            if (cursor < twentyTwo) return twentyTwo;
            return cursor.Date.AddDays(1);
        }

        private static OvertimeType ResolveOvertimeType(DateTime value)
        {
            var night = value.TimeOfDay < TimeSpan.FromHours(6) || value.TimeOfDay >= TimeSpan.FromHours(22);
            var weekend = IsWeekend(value);
            return (weekend, night) switch
            {
                (true, true) => OvertimeType.WeekendNight,
                (true, false) => OvertimeType.Weekend,
                (false, true) => OvertimeType.WeekdayNight,
                _ => OvertimeType.Weekday
            };
        }

        private static bool IsWeekend(DateTime value) =>
            value.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        private static DateTime Max(DateTime left, DateTime right) => left > right ? left : right;
        private static DateTime Min(DateTime left, DateTime right) => left < right ? left : right;
    }
}
