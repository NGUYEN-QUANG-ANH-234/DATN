using System.Text.Json;
using HRM.backend.src.HRM.Application.DTOs.PersonnelChanges;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Entities.PersonnelChanges;
using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PersonnelChanges;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;
using HRM.backend.src.HRM.Core.Models.History;

namespace HRM.backend.src.HRM.Application.UseCases.PersonnelChanges
{
    public class PersonnelChangeRiskSummaryBuilder
    {
        private readonly IPersonnelChangeRepository _personnelChangeRepo;
        private readonly IContractRepository _contractRepo;
        private readonly IContractAddendumRepository _contractAddendumRepo;
        private readonly IPerformanceReviewRepository _performanceReviewRepo;
        private readonly IPenaltyRecordRepository _penaltyRecordRepo;
        private readonly IAttendanceSummaryRepository _attendanceSummaryRepo;
        private readonly IHistoryTrackingRepository _historyTrackingRepo;
        private readonly IBaseRepository<Payroll> _payrollRepo;

        public PersonnelChangeRiskSummaryBuilder(
            IPersonnelChangeRepository personnelChangeRepo,
            IContractRepository contractRepo,
            IContractAddendumRepository contractAddendumRepo,
            IPerformanceReviewRepository performanceReviewRepo,
            IPenaltyRecordRepository penaltyRecordRepo,
            IAttendanceSummaryRepository attendanceSummaryRepo,
            IHistoryTrackingRepository historyTrackingRepo,
            IBaseRepository<Payroll> payrollRepo)
        {
            _personnelChangeRepo = personnelChangeRepo;
            _contractRepo = contractRepo;
            _contractAddendumRepo = contractAddendumRepo;
            _performanceReviewRepo = performanceReviewRepo;
            _penaltyRecordRepo = penaltyRecordRepo;
            _attendanceSummaryRepo = attendanceSummaryRepo;
            _historyTrackingRepo = historyTrackingRepo;
            _payrollRepo = payrollRepo;
        }

        public async Task<PersonnelChangeRiskSummaryDto> BuildAsync(int requestId, CancellationToken ct = default)
        {
            var request = await _personnelChangeRepo.GetDetailAsync(requestId, ct)
                ?? throw new KeyNotFoundException("Personnel change request was not found.");

            if (!request.EmployeeId.HasValue || request.Employee == null)
            {
                return new PersonnelChangeRiskSummaryDto
                {
                    RequestId = request.Id,
                    GeneratedAt = DateTime.UtcNow
                };
            }

            var employeeId = request.EmployeeId.Value;
            var contracts = await _contractRepo.GetByEmployeeIdAsync(employeeId, ct);
            var currentContract = contracts
                .Where(c => c.Status == ContractStatus.Active)
                .OrderByDescending(c => c.StartDate)
                .FirstOrDefault()
                ?? contracts.OrderByDescending(c => c.StartDate).FirstOrDefault();

            var relatedContract = request.RelatedContractId.HasValue
                ? await _contractRepo.GetByIdAsync(request.RelatedContractId.Value, ct)
                : currentContract;

            var relatedAddendum = request.RelatedContractAddendumId.HasValue
                ? await _contractAddendumRepo.GetByIdWithContractAsync(request.RelatedContractAddendumId.Value, ct)
                : null;

            var latestPerformance = (await _performanceReviewRepo.GetByEmployeeAsync(employeeId, ct))
                .OrderByDescending(r => r.FinalizedAt ?? r.CreatedAt)
                .ThenByDescending(r => r.Id)
                .FirstOrDefault();

            var penaltyRecords = await _penaltyRecordRepo.GetPersonnelHistoryByEmployeeAsync(employeeId, ct);
            var latestPayroll = await GetLatestPayrollAsync(employeeId, ct);
            var latestAttendance = await GetLatestAttendanceAsync(employeeId, ct);
            var history = await _historyTrackingRepo.GetPagedConsolidatedHistoryAsync(
                employeeId,
                new HistoryFilterCriteria { Type = "ALL", Page = 1, Size = 10 },
                ct);

            return new PersonnelChangeRiskSummaryDto
            {
                RequestId = request.Id,
                Employee = MapEmployee(request),
                CurrentContract = MapContract(currentContract),
                RelatedContract = MapContract(relatedContract),
                RelatedAddendum = MapAddendum(relatedAddendum),
                LatestPerformance = MapPerformance(latestPerformance),
                PenaltySummary = MapPenaltySummary(penaltyRecords),
                Seniority = MapSeniority(request.Employee.JoinedDate),
                LatestPayroll = MapPayroll(latestPayroll),
                LatestAttendance = MapAttendance(latestAttendance),
                History = history.Items.Select(MapHistory).ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<string> BuildSnapshotJsonAsync(int requestId, CancellationToken ct = default)
        {
            var summary = await BuildAsync(requestId, ct);
            return JsonSerializer.Serialize(summary, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }

        private async Task<Payroll?> GetLatestPayrollAsync(int employeeId, CancellationToken ct)
        {
            var payrolls = await _payrollRepo.FindAsync(p => p.EmployeeId == employeeId, ct);

            return payrolls
                .OrderByDescending(p => p.Year ?? 0)
                .ThenByDescending(p => p.Month ?? 0)
                .ThenByDescending(p => p.CalculatedAt ?? p.CreatedAt)
                .FirstOrDefault();
        }

        private async Task<AttendanceSummary?> GetLatestAttendanceAsync(int employeeId, CancellationToken ct)
        {
            var summaries = await _attendanceSummaryRepo.FindAsync(s => s.EmployeeId == employeeId, ct);

            return summaries
                .OrderByDescending(s => s.Year)
                .ThenByDescending(s => s.Month)
                .FirstOrDefault();
        }

        private static PersonnelChangeEmployeeSnapshotDto? MapEmployee(PersonnelChangeRequest request)
        {
            if (!request.EmployeeId.HasValue || request.Employee == null)
                return null;

            return new PersonnelChangeEmployeeSnapshotDto
            {
                Id = request.EmployeeId.Value,
                EmployeeCode = request.Employee.EmployeeCode,
                FullName = request.Employee.FullName,
                DepartmentName = request.CurrentDepartment?.DeptName ?? request.Employee.Department?.DeptName,
                PositionName = request.CurrentPosition?.Title ?? request.Employee.Position?.Title,
                JobLevelName = request.CurrentJobLevel?.Name ?? request.Employee.JobLevel?.Name,
                EmployeeType = request.CurrentEmployeeType?.ToString() ?? request.Employee.Type.ToString(),
                Status = request.Employee.Status.ToString(),
                JoinedDate = request.Employee.JoinedDate
            };
        }

        private static PersonnelChangeContractSnapshotDto? MapContract(Contract? contract)
        {
            if (contract == null)
                return null;

            return new PersonnelChangeContractSnapshotDto
            {
                Id = contract.Id,
                ContractNumber = contract.ContractNumber,
                ContractType = contract.ContractType.ToString(),
                Status = contract.Status.ToString(),
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                BasicSalary = contract.BasicSalary,
                InsuranceSalary = contract.InsuranceSalary
            };
        }

        private static PersonnelChangeContractAddendumSnapshotDto? MapAddendum(ContractAddendum? addendum)
        {
            if (addendum == null)
                return null;

            return new PersonnelChangeContractAddendumSnapshotDto
            {
                Id = addendum.Id,
                ContractId = addendum.ContractId,
                AddendumNumber = addendum.AddendumNumber,
                Status = addendum.Status.ToString(),
                EffectiveDate = addendum.EffectiveDate,
                NewBasicSalary = addendum.NewBasicSalary,
                NewInsuranceSalary = addendum.NewInsuranceSalary
            };
        }

        private static PersonnelChangePerformanceSnapshotDto? MapPerformance(PerformanceReview? review)
        {
            if (review == null)
                return null;

            return new PersonnelChangePerformanceSnapshotDto
            {
                Id = review.Id,
                Period = review.Period,
                TotalScore = review.TotalScore,
                FinalRating = review.FinalRating,
                Status = review.Status.ToString(),
                FinalizedAt = review.FinalizedAt,
                Kpis = review.Details
                    .OrderByDescending(d => d.WeightPercent)
                    .ThenBy(d => d.KpiCode)
                    .Select(d => new PersonnelChangeKpiSnapshotDto
                    {
                        Id = d.Id,
                        KpiCode = d.KpiCode,
                        KpiName = d.KpiName,
                        WeightPercent = d.WeightPercent,
                        TargetValue = d.TargetValue,
                        ActualValue = d.ActualValue,
                        Unit = d.Unit,
                        AchievedPercent = d.AchievedPercent,
                        ManagerScore = d.ManagerScore,
                        FinalPoint = d.FinalPoint,
                        PenaltyPoint = d.PenaltyPoint,
                        PenaltyReason = d.PenaltyReason
                    })
                    .ToList()
            };
        }

        private static PersonnelChangePenaltySummaryDto MapPenaltySummary(List<PenaltyRecord> records)
        {
            var ordered = records
                .OrderByDescending(r => r.OccurredAt ?? r.CreatedAt)
                .ThenByDescending(r => r.Id)
                .ToList();

            return new PersonnelChangePenaltySummaryDto
            {
                TotalRecords = ordered.Count,
                PersonnelImpactRecords = ordered.Count(r => r.AffectsPersonnelDecision),
                TotalPenaltyPoint = ordered.Sum(r => r.PenaltyPoint),
                LatestRecords = ordered.Take(5).Select(r => new PersonnelChangePenaltyItemDto
                {
                    Id = r.Id,
                    Period = r.Period,
                    SourceType = r.SourceType.ToString(),
                    RuleCode = r.RuleCode,
                    PenaltyPoint = r.PenaltyPoint,
                    Reason = r.Reason,
                    Status = r.Status.ToString(),
                    Severity = r.Severity.ToString(),
                    OccurredAt = r.OccurredAt
                }).ToList()
            };
        }

        private static PersonnelChangeSeniorityDto MapSeniority(DateTime? joinedDate)
        {
            if (!joinedDate.HasValue)
                return new PersonnelChangeSeniorityDto();

            var today = DateTime.UtcNow.Date;
            var start = joinedDate.Value.Date;
            var totalMonths = Math.Max(0, ((today.Year - start.Year) * 12) + today.Month - start.Month - (today.Day < start.Day ? 1 : 0));

            return new PersonnelChangeSeniorityDto
            {
                JoinedDate = joinedDate,
                TotalMonths = totalMonths,
                TotalYears = Math.Round(totalMonths / 12m, 2)
            };
        }

        private static PersonnelChangePayrollSnapshotDto? MapPayroll(Payroll? payroll)
        {
            if (payroll == null)
                return null;

            return new PersonnelChangePayrollSnapshotDto
            {
                Id = payroll.Id,
                Month = payroll.Month,
                Year = payroll.Year,
                Period = payroll.Period,
                Status = payroll.Status.ToString(),
                GrossIncome = payroll.GrossIncome,
                NetSalary = payroll.NetSalary,
                CalculatedAt = payroll.CalculatedAt
            };
        }

        private static PersonnelChangeAttendanceSnapshotDto? MapAttendance(AttendanceSummary? summary)
        {
            if (summary == null)
                return null;

            return new PersonnelChangeAttendanceSnapshotDto
            {
                Id = summary.Id,
                Month = summary.Month,
                Year = summary.Year,
                WorkDays = summary.WorkDays,
                WorkedMinutes = summary.WorkedMinutes,
                LateMinutes = summary.LateMinutes,
                EarlyLeaveMinutes = summary.EarlyLeaveMinutes,
                ActualOtMinutes = summary.ActualOtMinutes,
                IsPayrollLocked = summary.IsPayrollLocked
            };
        }

        private static PersonnelChangeHistorySummaryItemDto MapHistory(ConsolidatedHistoryRecord record)
        {
            return new PersonnelChangeHistorySummaryItemDto
            {
                Date = record.Date,
                EventType = record.EventType,
                Title = record.Title,
                Description = record.Description,
                RefId = record.RefId
            };
        }
    }
}
