using HRM.backend.src.HRM.Application.DTOs.TasksTraining;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.TasksTraining.Usecases;
using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;

namespace HRM.backend.src.HRM.Application.UseCases.TasksTraining
{
    public class PerformanceEvaluationUseCase : IPerformanceEvaluationUseCase
    {
        private const decimal ManagerScoreCommentThreshold = 15m;
        private const string WeightedScoringVersion = "WeightedV2";

        private readonly IPerformanceReviewRepository _reviewRepo;
        private readonly IPenaltyRecordRepository _penaltyRecordRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;

        public PerformanceEvaluationUseCase(
            IPerformanceReviewRepository reviewRepo,
            IPenaltyRecordRepository penaltyRecordRepo,
            IEmployeeRepository employeeRepo,
            IUnitOfWork unitOfWork,
            ILockService lockService)
        {
            _reviewRepo = reviewRepo;
            _penaltyRecordRepo = penaltyRecordRepo;
            _employeeRepo = employeeRepo;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
        }

        public async Task<List<PerformanceEvaluationDto>> GetMyReviewsAsync(int actorAccountId, CancellationToken ct = default)
        {
            var employee = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct)
                ?? throw new UnauthorizedAccessException("Account is not linked to an employee profile.");
            var reviews = await _reviewRepo.GetByEmployeeAsync(employee.Id, ct);
            return reviews.Select(r => MapReview(r, 0)).ToList();
        }

        public async Task<List<PerformanceEvaluationDto>> GetPendingEvaluationsAsync(int actorAccountId, string role, CancellationToken ct = default)
        {
            if (IsAdmin(role))
            {
                var allReviews = await _reviewRepo.GetByStatusAsync(ReviewStatus.PendingEvaluation, ct);
                return allReviews.Select(r => MapReview(r, 0)).ToList();
            }

            if (!IsManager(role))
                return new List<PerformanceEvaluationDto>();

            var managedDeptIds = (await _employeeRepo.GetManagedDepartmentIdsByAccountIdAsync(actorAccountId, ct)).ToHashSet();
            if (managedDeptIds.Count == 0)
                return new List<PerformanceEvaluationDto>();

            var reviews = (await _reviewRepo.GetByStatusAsync(ReviewStatus.PendingEvaluation, ct))
                .Where(r => r.DeptId.HasValue && managedDeptIds.Contains(r.DeptId.Value))
                .ToList();
            return reviews.Select(r => MapReview(r, 0)).ToList();
        }

        public async Task<PerformanceEvaluationDto> GetDetailAsync(int id, int actorAccountId, string role, CancellationToken ct = default)
        {
            var review = await _reviewRepo.GetDetailAsync(id, ct)
                ?? throw new InvalidOperationException("Performance review not found.");
            await EnsureReviewerAsync(review, actorAccountId, role, ct);
            var systemPenalty = await GetSystemPenaltyAsync(review, ct);
            return MapReview(review, systemPenalty.TotalPoint);
        }

        public async Task UpdateMyProgressAsync(int id, PerformanceProgressUpdateDto dto, int actorAccountId, CancellationToken ct = default)
        {
            var employee = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct)
                ?? throw new UnauthorizedAccessException("Account is not linked to an employee profile.");

            await _lockService.GetWithLockAsync($"performance_progress_{id}", async innerCt =>
            {
                var review = await _reviewRepo.GetDetailTrackedAsync(id, innerCt)
                    ?? throw new InvalidOperationException("Performance review not found.");

                if (review.EmployeeId != employee.Id)
                    throw new UnauthorizedAccessException("Only the assigned employee can update this KPI review.");

                if (review.Status != ReviewStatus.PendingEmployeeUpdate &&
                    review.Status != ReviewStatus.ReworkRequired)
                    throw new InvalidOperationException("This KPI review is not open for employee progress update.");

                var detailMap = dto.Details.ToDictionary(d => d.DetailId);
                foreach (var detail in review.Details)
                {
                    if (!detailMap.TryGetValue(detail.Id, out var update))
                        continue;

                    if (update.ActualValue.HasValue && update.ActualValue.Value < 0)
                        throw new InvalidOperationException("Actual KPI value cannot be negative.");

                    detail.EmployeeSelfPercent = Math.Clamp(update.EmployeeSelfPercent, 0, 100);
                    detail.ActualValue = update.ActualValue;
                    detail.EmployeeComment = update.EmployeeComment?.Trim();

                    detail.AchievedPercent = detail.TargetValue.HasValue && detail.TargetValue.Value > 0 && update.ActualValue.HasValue
                        ? Math.Clamp(update.ActualValue.Value / detail.TargetValue.Value * 100, 0, 999.99m)
                        : detail.EmployeeSelfPercent;
                }

                review.Status = ReviewStatus.PendingEvaluation;
                review.ReviewDeadline = DateTime.UtcNow.AddDays(2);
                _reviewRepo.Update(review);
                await _unitOfWork.CommitAsync(innerCt);
                return true;
            }, cancellationToken: ct);
        }

        public async Task FinalizeScoreAsync(int id, FinalizePerformanceDto dto, int actorAccountId, string role, CancellationToken ct = default)
        {
            await _lockService.GetWithLockAsync($"performance_finalize_{id}", async innerCt =>
            {
                var review = await _reviewRepo.GetDetailTrackedAsync(id, innerCt)
                    ?? throw new InvalidOperationException("Performance review not found.");
                await EnsureReviewerAsync(review, actorAccountId, role, innerCt);

                if (!dto.IsApproved)
                {
                    review.Status = ReviewStatus.ReworkRequired;
                    review.FinalComment = dto.FinalComment;
                    _reviewRepo.Update(review);
                    await _unitOfWork.CommitAsync(innerCt);
                    return true;
                }

                var systemPenalty = await GetSystemPenaltyAsync(review, innerCt);
                EnsureManagerCommentForLargeDeviation(review, dto);
                ApplyScores(review, dto, systemPenalty);
                review.ReviewerAccountId = actorAccountId;
                review.FinalComment = dto.FinalComment;
                review.FinalRating = string.IsNullOrWhiteSpace(dto.FinalRating)
                    ? ResolveRating(review.TotalScore)
                    : dto.FinalRating.Trim();
                review.ScoringVersion = WeightedScoringVersion;
                review.Status = ReviewStatus.Evaluated;
                review.FinalizedAt = DateTime.UtcNow;
                review.IsPayrollSynced = false;
                review.PayrollSyncedAt = null;

                _reviewRepo.Update(review);
                await _unitOfWork.CommitAsync(innerCt);
                return true;
            }, cancellationToken: ct);
        }

        private static void ApplyScores(PerformanceReview review, FinalizePerformanceDto dto, (decimal TotalPoint, string Reason) systemPenalty)
        {
            var detailMap = dto.Details.ToDictionary(d => d.DetailId);
            var orderedDetails = review.Details.OrderBy(d => d.Id).ToList();
            var systemPenaltyAllocations = AllocateSystemPenaltyByWeight(orderedDetails, systemPenalty.TotalPoint);

            foreach (var detail in orderedDetails)
            {
                detailMap.TryGetValue(detail.Id, out var score);
                detail.ManagerScore = score == null ? detail.ManagerScore : Math.Clamp(score.ManagerScore, 0, 100);
                detail.ManualPenaltyPoint = score == null ? 0 : Math.Max(0, score.ManualPenaltyPoint);
                detail.ManualPenaltyReason = score?.ManualPenaltyReason;
                detail.ManagerComment = score?.ManagerComment;

                detail.SystemPenaltyPoint = systemPenaltyAllocations.GetValueOrDefault(detail.Id);
                detail.SystemPenaltyReason = detail.SystemPenaltyPoint > 0 ? systemPenalty.Reason : null;

                detail.PenaltyPoint = detail.SystemPenaltyPoint + detail.ManualPenaltyPoint;
                detail.PenaltyReason = string.Join("; ", new[] { detail.SystemPenaltyReason, detail.ManualPenaltyReason }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

                var weightedOfficialPoint = detail.ManagerScore * detail.WeightPercent / 100m;
                detail.FinalPoint = Math.Round(
                    Math.Max(0, weightedOfficialPoint - detail.PenaltyPoint),
                    2,
                    MidpointRounding.AwayFromZero);
            }

            review.TotalWeight = orderedDetails.Sum(d => d.WeightPercent);
            review.TotalScore = Math.Round(
                Math.Clamp(orderedDetails.Sum(d => d.FinalPoint), 0, 100),
                2,
                MidpointRounding.AwayFromZero);
        }

        private static void EnsureManagerCommentForLargeDeviation(PerformanceReview review, FinalizePerformanceDto dto)
        {
            var detailMap = dto.Details.ToDictionary(d => d.DetailId);
            foreach (var detail in review.Details)
            {
                if (!detailMap.TryGetValue(detail.Id, out var score))
                    continue;

                var managerScore = Math.Clamp(score.ManagerScore, 0, 100);
                var referenceScore = ReferenceManagerScore(detail);
                var isLargeDeviation = Math.Abs(managerScore - referenceScore) >= ManagerScoreCommentThreshold;

                if (isLargeDeviation && string.IsNullOrWhiteSpace(score.ManagerComment))
                    throw new InvalidOperationException($"KPI {detail.KpiCode} có điểm trưởng phòng lệch nhiều so với điểm gợi ý, vui lòng nhập nhận xét.");
            }
        }

        private static decimal ReferenceManagerScore(PerformanceDetail detail)
        {
            if (detail.AchievedPercent > 0)
                return Math.Min(100, detail.AchievedPercent);
            return Math.Clamp(detail.EmployeeSelfPercent, 0, 100);
        }

        private static Dictionary<int, decimal> AllocateSystemPenaltyByWeight(
            IReadOnlyList<PerformanceDetail> details,
            decimal totalPenalty)
        {
            var result = details.ToDictionary(d => d.Id, _ => 0m);
            if (details.Count == 0 || totalPenalty <= 0)
                return result;

            var totalWeight = details.Sum(d => Math.Max(0, d.WeightPercent));
            if (totalWeight <= 0)
                return result;

            var remainingPenalty = Math.Round(totalPenalty, 2, MidpointRounding.AwayFromZero);
            for (var index = 0; index < details.Count; index++)
            {
                var detail = details[index];
                var allocation = index == details.Count - 1
                    ? remainingPenalty
                    : Math.Round(totalPenalty * detail.WeightPercent / totalWeight, 2, MidpointRounding.AwayFromZero);

                allocation = Math.Min(Math.Max(0, allocation), remainingPenalty);
                result[detail.Id] = allocation;
                remainingPenalty -= allocation;
            }

            return result;
        }

        private async Task<(decimal TotalPoint, string Reason)> GetSystemPenaltyAsync(PerformanceReview review, CancellationToken ct)
        {
            var systemRecords = await _penaltyRecordRepo.GetApprovedPerformanceByEmployeePeriodAsync(review.EmployeeId, review.Period, ct);
            return (
                systemRecords.Sum(r => r.PenaltyPoint),
                string.Join("; ", systemRecords.Select(r => $"{r.RuleCode}: {r.Reason}").Where(x => !string.IsNullOrWhiteSpace(x))));
        }

        private async Task EnsureReviewerAsync(PerformanceReview review, int actorAccountId, string role, CancellationToken ct)
        {
            if (IsAdmin(role))
                return;
            if (!IsManager(role))
                throw new UnauthorizedAccessException("Only Manager or Admin can evaluate performance.");

            var managedDeptIds = await GetManagedDepartmentIdsAsync(actorAccountId, ct);
            if (review.DeptId.HasValue && managedDeptIds.Contains(review.DeptId.Value))
                return;
            throw new UnauthorizedAccessException("Manager can only evaluate employees in their department.");
        }

        private async Task<HashSet<int>> GetManagedDepartmentIdsAsync(int actorAccountId, CancellationToken ct)
        {
            var deptIds = await _employeeRepo.GetManagedDepartmentIdsByAccountIdAsync(actorAccountId, ct);
            if (deptIds.Count == 0)
                throw new UnauthorizedAccessException("Manager account is not linked to a managed department.");
            return deptIds.ToHashSet();
        }

        private static PerformanceEvaluationDto MapReview(PerformanceReview review, decimal systemPenalty)
        {
            var orderedDetails = review.Details.OrderBy(d => d.Id).ToList();
            var previewSystemPenaltyAllocations = IsFinalizedReview(review.Status)
                ? orderedDetails.ToDictionary(d => d.Id, d => d.SystemPenaltyPoint)
                : AllocateSystemPenaltyByWeight(orderedDetails, systemPenalty);

            return new PerformanceEvaluationDto
            {
                Id = review.Id,
                EmployeeId = review.EmployeeId,
                EmployeeName = review.Employee?.FullName ?? string.Empty,
                DepartmentName = review.Department?.DeptName ?? review.Employee?.Department?.DeptName,
                Period = review.Period,
                TotalWeight = review.TotalWeight,
                SystemPenaltyPoint = systemPenalty,
                TotalScore = review.TotalScore,
                ScoringVersion = review.ScoringVersion,
                FinalRating = review.FinalRating,
                FinalComment = review.FinalComment,
                Status = review.Status.ToString(),
                Details = orderedDetails.Select(d => new PerformanceDetailDto
                {
                    Id = d.Id,
                    KpiCode = d.KpiCode,
                    KpiName = d.KpiName,
                    WeightPercent = d.WeightPercent,
                    TargetValue = d.TargetValue,
                    ActualValue = d.ActualValue,
                    Unit = d.Unit,
                    EmployeeSelfPercent = d.EmployeeSelfPercent,
                    AchievedPercent = d.AchievedPercent,
                    ManagerScore = d.ManagerScore,
                    SystemPenaltyPoint = previewSystemPenaltyAllocations.GetValueOrDefault(d.Id),
                    SystemPenaltyReason = previewSystemPenaltyAllocations.GetValueOrDefault(d.Id) > 0
                        ? d.SystemPenaltyReason ?? "Điểm trừ hệ thống được phân bổ theo trọng số KPI."
                        : null,
                    ManualPenaltyPoint = d.ManualPenaltyPoint,
                    ManualPenaltyReason = d.ManualPenaltyReason,
                    PenaltyPoint = IsFinalizedReview(review.Status)
                        ? d.PenaltyPoint
                        : previewSystemPenaltyAllocations.GetValueOrDefault(d.Id) + d.ManualPenaltyPoint,
                    PenaltyReason = d.PenaltyReason,
                    FinalPoint = d.FinalPoint,
                    EmployeeComment = d.EmployeeComment,
                    ManagerComment = d.ManagerComment,
                    EvidencePath = d.EvidencePath
                }).ToList()
            };
        }

        private static bool IsFinalizedReview(ReviewStatus status) =>
            status == ReviewStatus.Evaluated ||
            status == ReviewStatus.AutoEvaluated ||
            status == ReviewStatus.Approved;

        private static string ResolveRating(decimal totalScore)
        {
            return totalScore switch
            {
                >= 90 => "A",
                >= 75 => "B",
                >= 60 => "C",
                _ => "D"
            };
        }

        private static bool IsManager(string role) =>
            role.Equals("Manager", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("Truong phong", StringComparison.OrdinalIgnoreCase);

        private static bool IsAdmin(string role) =>
            role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }
}
