using HRM.backend.src.HRM.Application.DTOs.TasksTraining;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.TasksTraining.Services;
using HRM.backend.src.HRM.Application.Interfaces.TasksTraining.Usecases;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;

namespace HRM.backend.src.HRM.Application.UseCases.TasksTraining
{
    public class KpiManagementUseCase : IKpiManagementUseCase
    {
        private const string WeightedScoringVersion = "WeightedV2";

        private readonly IExcelKpiParserService _excelParser;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IPerformanceReviewRepository _reviewRepo;
        private readonly IPerformanceDetailRepository _detailRepo;
        private readonly IKpiImportBatchRepository _batchRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;

        public KpiManagementUseCase(
            IExcelKpiParserService excelParser,
            IEmployeeRepository employeeRepo,
            IPerformanceReviewRepository reviewRepo,
            IPerformanceDetailRepository detailRepo,
            IKpiImportBatchRepository batchRepo,
            IUnitOfWork unitOfWork,
            ILockService lockService)
        {
            _excelParser = excelParser;
            _employeeRepo = employeeRepo;
            _reviewRepo = reviewRepo;
            _detailRepo = detailRepo;
            _batchRepo = batchRepo;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
        }

        public async Task<KpiImportResultDto> ImportKpisFromExcelAsync(
            KpiImportRequestDto dto,
            int actorAccountId,
            string actorRole,
            CancellationToken ct = default)
        {
            var period = NormalizePeriod(dto.Period);
            var actor = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct);
            var deptId = ResolveImportDeptId(dto.DeptId, actor, actorRole);

            return await _lockService.GetWithLockAsync(
                $"kpi_import_dept_{deptId}_{period}",
                async (innerCt) => await ImportInternalAsync(dto, actorAccountId, deptId, period, innerCt),
                cancellationToken: ct);
        }

        private async Task<KpiImportResultDto> ImportInternalAsync(
            KpiImportRequestDto dto,
            int actorAccountId,
            int deptId,
            string period,
            CancellationToken ct)
        {
            var rows = await _excelParser.ParseToDtoListAsync(dto.File, ct);
            var employees = await _employeeRepo.GetActiveByDeptWithDepartmentAsync(deptId, ct: ct);
            var employeeByCode = employees
                .GroupBy(e => e.EmployeeCode.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var errors = ValidateRows(rows, employeeByCode);
            if (errors.Any())
                throw new KpiImportValidationException(errors);

            var batch = new KpiImportBatch
            {
                Period = period,
                DeptId = deptId,
                ImportedByAccountId = actorAccountId,
                FileName = dto.File.FileName,
                TotalRows = rows.Count,
                SuccessRows = rows.Count,
                ErrorRows = 0,
                Status = ImportBatchStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            var createdOrUpdatedReviews = 0;
            var createdDetails = 0;
            var totalAssignedWeight = 0;

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                await _batchRepo.AddAsync(batch, ct);

                foreach (var group in rows.GroupBy(r => r.EmployeeCode.Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    var employee = employeeByCode[group.Key];
                    var review = await _reviewRepo.GetByEmployeePeriodAsync(employee.Id, period, ct);

                    if (review == null)
                    {
                        review = new PerformanceReview
                        {
                            EmployeeId = employee.Id,
                            DeptId = deptId,
                            ImportBatch = batch,
                            CreatedByAccountId = actorAccountId,
                            Period = period,
                            ScoringVersion = WeightedScoringVersion,
                            Status = ReviewStatus.PendingEmployeeUpdate,
                            ReviewDeadline = DateTime.UtcNow.AddDays(7),
                            CreatedAt = DateTime.UtcNow
                        };

                        await _reviewRepo.AddAsync(review, ct);
                    }
                    else
                    {
                        if (review.IsPayrollSynced || review.PayrollSyncedAt.HasValue)
                            throw new InvalidOperationException($"KPI kỳ {period} của nhân viên {employee.EmployeeCode} đã được đồng bộ sang payroll, không thể ghi đè bằng file import.");

                        if (review.Details.Any())
                            _detailRepo.RemoveRange(review.Details);

                        review.DeptId = deptId;
                        review.ImportBatch = batch;
                        review.CreatedByAccountId = actorAccountId;
                        review.Status = ReviewStatus.PendingEmployeeUpdate;
                        review.ReviewDeadline = DateTime.UtcNow.AddDays(7);
                        review.FinalRating = null;
                        review.FinalComment = null;
                        review.FinalizedAt = null;
                        review.ScoringVersion = WeightedScoringVersion;
                        review.TotalScore = 0;
                        review.IsPayrollSynced = false;
                        review.PayrollSyncedAt = null;
                        _reviewRepo.Update(review);
                    }

                    var details = group.Select(row =>
                        new PerformanceDetail
                        {
                            Review = review,
                            KpiCode = BuildKpiCode(row),
                            KpiName = row.KpiName.Trim(),
                            Description = row.Description,
                            WeightPercent = row.WeightPercent,
                            TargetValue = row.TargetValue,
                            Unit = row.Unit,
                            EmployeeSelfPercent = 0,
                            AchievedPercent = 0,
                            ManagerScore = 0,
                            FinalPoint = 0,
                            SystemPenaltyPoint = 0,
                            ManualPenaltyPoint = 0,
                            PenaltyPoint = 0
                        }).ToList();

                    review.TotalWeight = details.Sum(d => d.WeightPercent);
                    review.TotalScore = 0;
                    await _detailRepo.AddRangeAsync(details, ct);
                    createdOrUpdatedReviews++;
                    createdDetails += details.Count;
                    totalAssignedWeight += review.TotalWeight;
                }

                await _unitOfWork.CommitAsync(ct);
            }, ct);

            return new KpiImportResultDto
            {
                ImportBatchId = batch.Id,
                Period = period,
                DeptId = deptId,
                TotalRows = rows.Count,
                SuccessRows = rows.Count,
                ErrorRows = 0,
                CreatedOrUpdatedReviews = createdOrUpdatedReviews,
                CreatedDetails = createdDetails,
                TotalAssignedWeight = totalAssignedWeight
            };
        }

        private static List<KpiImportErrorDto> ValidateRows(
            List<KpiImportRowDto> rows,
            Dictionary<string, Employee> employeeByCode)
        {
            var errors = new List<KpiImportErrorDto>();
            if (!rows.Any())
            {
                errors.Add(new KpiImportErrorDto { RowNumber = 0, Message = "File không có dòng KPI hợp lệ." });
                return errors;
            }

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.EmployeeCode))
                    errors.Add(new KpiImportErrorDto { RowNumber = row.RowNumber, Message = "Thiếu mã nhân viên." });
                else if (!employeeByCode.ContainsKey(row.EmployeeCode.Trim()))
                    errors.Add(new KpiImportErrorDto { RowNumber = row.RowNumber, Message = $"Mã nhân viên {row.EmployeeCode} không thuộc phòng ban được phép import." });

                if (string.IsNullOrWhiteSpace(row.KpiName))
                    errors.Add(new KpiImportErrorDto { RowNumber = row.RowNumber, Message = "Tên chỉ tiêu KPI không được để trống." });

                if (row.WeightPercent <= 0 || row.WeightPercent > 100)
                    errors.Add(new KpiImportErrorDto { RowNumber = row.RowNumber, Message = "Trọng số KPI phải nằm trong khoảng 1-100." });
            }

            foreach (var group in rows
                .Where(r => !string.IsNullOrWhiteSpace(r.EmployeeCode))
                .GroupBy(r => r.EmployeeCode.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                var totalWeight = group.Sum(r => r.WeightPercent);
                if (totalWeight != 100)
                    errors.Add(new KpiImportErrorDto { RowNumber = group.First().RowNumber, Message = $"Tổng trọng số KPI của nhân viên {group.Key} phải bằng 100%, hiện tại là {totalWeight}%." });

                var duplicatedCodes = group
                    .Where(r => !string.IsNullOrWhiteSpace(r.KpiCode))
                    .GroupBy(r => r.KpiCode!.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                foreach (var code in duplicatedCodes)
                    errors.Add(new KpiImportErrorDto { RowNumber = group.First(r => string.Equals(r.KpiCode?.Trim(), code, StringComparison.OrdinalIgnoreCase)).RowNumber, Message = $"Mã KPI {code} bị trùng trong cùng một nhân viên." });
            }

            return errors;
        }

        private static int ResolveImportDeptId(int? requestedDeptId, Employee? actor, string actorRole)
        {
            if (IsManager(actorRole))
            {
                if (actor?.DeptId == null)
                    throw new UnauthorizedAccessException("Tài khoản Trưởng phòng chưa liên kết với phòng ban.");
                return actor.DeptId.Value;
            }

            if (IsHrOrAdmin(actorRole))
            {
                if (requestedDeptId.HasValue && requestedDeptId.Value > 0)
                    return requestedDeptId.Value;
                if (actor?.DeptId != null)
                    return actor.DeptId.Value;
            }

            throw new UnauthorizedAccessException("Chỉ Trưởng phòng, HR hoặc Admin được import KPI.");
        }

        private static bool IsManager(string role)
        {
            return role.Equals("Manager", StringComparison.OrdinalIgnoreCase) ||
                   role.Equals("Trưởng phòng", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsHrOrAdmin(string role)
        {
            return role.Equals("HR", StringComparison.OrdinalIgnoreCase) ||
                   role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePeriod(string? period)
        {
            if (string.IsNullOrWhiteSpace(period))
                return $"{DateTime.UtcNow.Month:D2}/{DateTime.UtcNow.Year}";

            var trimmed = period.Trim();
            var parts = trimmed.Split(new[] { '/', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                if (int.TryParse(parts[0], out var first) && int.TryParse(parts[1], out var second))
                {
                    var month = first;
                    var year = second;

                    if (first > 1900)
                    {
                        year = first;
                        month = second;
                    }

                    if (month is >= 1 and <= 12 && year is >= 2000 and <= 2100)
                        return $"{month:D2}/{year}";
                }
            }

            if (DateTime.TryParse(trimmed, out var parsed))
                return $"{parsed.Month:D2}/{parsed.Year}";

            throw new ArgumentException("Kỳ KPI không hợp lệ. Định dạng gợi ý: MM/yyyy.");
        }

        private static string BuildKpiCode(KpiImportRowDto row)
        {
            if (!string.IsNullOrWhiteSpace(row.KpiCode))
                return row.KpiCode.Trim();

            return $"KPI-{row.RowNumber}";
        }
    }

    public class KpiImportValidationException : Exception
    {
        public KpiImportValidationException(List<KpiImportErrorDto> errors)
            : base("File KPI có dữ liệu không hợp lệ.")
        {
            Errors = errors;
        }

        public List<KpiImportErrorDto> Errors { get; }
    }
}
