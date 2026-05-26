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
            var reviewer = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct)
                ?? throw new UnauthorizedAccessException("Account is not linked to an employee profile.");
            if (!IsManager(role) && !IsHrOrAdmin(role))
                throw new UnauthorizedAccessException("Only Manager, HR or Admin can evaluate performance.");

            var reviews = IsManager(role)
                ? await _reviewRepo.GetPendingEvaluationAsync(reviewer.DeptId ?? 0, ct)
                : await _reviewRepo.GetByStatusAsync(ReviewStatus.PendingEvaluation, ct);

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

                    detail.EmployeeSelfPercent = Math.Clamp(update.EmployeeSelfPercent, 0, 100);
                    detail.ActualValue = update.ActualValue;
                    detail.EmployeeComment = update.EmployeeComment?.Trim();

                    detail.AchievedPercent = detail.TargetValue.HasValue && detail.TargetValue.Value > 0 && update.ActualValue.HasValue
                        ? Math.Clamp(update.ActualValue.Value / detail.TargetValue.Value * 100, 0, 100)
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
                ApplyScores(review, dto, systemPenalty);
                review.ReviewerAccountId = actorAccountId;
                review.FinalComment = dto.FinalComment;
                review.FinalRating = string.IsNullOrWhiteSpace(dto.FinalRating)
                    ? ResolveRating(review.TotalScore)
                    : dto.FinalRating.Trim();
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
            var systemPenaltyRemaining = systemPenalty.TotalPoint;

            foreach (var detail in orderedDetails)
            {
                detailMap.TryGetValue(detail.Id, out var score);
                detail.ManagerScore = score == null ? detail.ManagerScore : Math.Clamp(score.ManagerScore, 0, 100);
                detail.ManualPenaltyPoint = score == null ? 0 : Math.Max(0, score.ManualPenaltyPoint);
                detail.ManualPenaltyReason = score?.ManualPenaltyReason;
                detail.ManagerComment = score?.ManagerComment;

                detail.SystemPenaltyPoint = systemPenaltyRemaining;
                detail.SystemPenaltyReason = systemPenaltyRemaining > 0 ? systemPenalty.Reason : null;
                systemPenaltyRemaining = 0;

                detail.PenaltyPoint = detail.SystemPenaltyPoint + detail.ManualPenaltyPoint;
                detail.PenaltyReason = string.Join("; ", new[] { detail.SystemPenaltyReason, detail.ManualPenaltyReason }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));
                detail.FinalPoint = Math.Max(0, detail.ManagerScore - detail.PenaltyPoint);
            }

            review.TotalWeight = orderedDetails.Sum(d => d.WeightPercent);
            review.TotalScore = orderedDetails.Sum(d => d.FinalPoint);
        }

        private async Task<(decimal TotalPoint, string Reason)> GetSystemPenaltyAsync(PerformanceReview review, CancellationToken ct)
        {
            var records = await _penaltyRecordRepo.GetByEmployeePeriodAsync(review.EmployeeId, review.Period, ct);
            var systemRecords = records.Where(r => r.CreatedBySystem).ToList();
            return (
                systemRecords.Sum(r => r.PenaltyPoint),
                string.Join("; ", systemRecords.Select(r => $"{r.RuleCode}: {r.Reason}").Where(x => !string.IsNullOrWhiteSpace(x))));
        }

        private async Task EnsureReviewerAsync(PerformanceReview review, int actorAccountId, string role, CancellationToken ct)
        {
            if (IsHrOrAdmin(role))
                return;
            if (!IsManager(role))
                throw new UnauthorizedAccessException("Only Manager, HR or Admin can evaluate performance.");

            var reviewer = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct)
                ?? throw new UnauthorizedAccessException("Account is not linked to an employee profile.");
            if (review.DeptId.HasValue && reviewer.DeptId == review.DeptId)
                return;
            throw new UnauthorizedAccessException("Manager can only evaluate employees in their department.");
        }

        private static PerformanceEvaluationDto MapReview(PerformanceReview review, decimal systemPenalty)
        {
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
                FinalRating = review.FinalRating,
                FinalComment = review.FinalComment,
                Status = review.Status.ToString(),
                Details = review.Details.OrderBy(d => d.Id).Select(d => new PerformanceDetailDto
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
                    SystemPenaltyPoint = d.SystemPenaltyPoint,
                    SystemPenaltyReason = d.SystemPenaltyReason,
                    ManualPenaltyPoint = d.ManualPenaltyPoint,
                    ManualPenaltyReason = d.ManualPenaltyReason,
                    PenaltyPoint = d.PenaltyPoint,
                    PenaltyReason = d.PenaltyReason,
                    FinalPoint = d.FinalPoint,
                    EmployeeComment = d.EmployeeComment,
                    ManagerComment = d.ManagerComment,
                    EvidencePath = d.EvidencePath
                }).ToList()
            };
        }

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

        private static bool IsHrOrAdmin(string role) =>
            role.Equals("HR", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }
}
