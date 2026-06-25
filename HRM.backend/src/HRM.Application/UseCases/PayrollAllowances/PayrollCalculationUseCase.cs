using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Services;
using HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Usecases;
using HRM.backend.src.HRM.Application.Services.System;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PayrollAllowances;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;
using System.Globalization;
using System.Text;

namespace HRM.backend.src.HRM.Application.UseCases.PayrollAllowances
{
    public class PayrollCalculationUseCase : IPayrollCalculationUseCase
    {
        private static readonly HashSet<PayrollAdjustmentType> AllowedPayrollAdjustmentTypes = new()
        {
            PayrollAdjustmentType.RetroactiveSalaryIncrease,
            PayrollAdjustmentType.RetroactiveAllowance,
            PayrollAdjustmentType.InsuranceArrears,
            PayrollAdjustmentType.TaxAdjustment,
            PayrollAdjustmentType.ManualCorrection
        };

        private static readonly string[] AttendancePresenceKeywords =
        {
            "di muon",
            "ve som",
            "vang mat",
            "nghi khong phep",
            "cham cong",
            "bang cong",
            "roi vi tri",
            "khong co mat",
            "hien dien",
            "vi pham",
            "phat",
            "penalty", "absence", "late", "early leave", "leave early"
        };

        private static readonly string[] ManualPayrollCorrectionKeywords =
        {
            "truy thu",
            "truy linh",
            "bao hiem",
            "thue",
            "boi hoan",
            "hoan ung",
            "tam ung",
            "sai sot",
            "ky truoc",
            "nghiep vu luong",
            "dieu chinh luong",
            "dieu chinh thu nhap",
            "payroll correction",
            "prior period",
            "retroactive",
            "tax",
            "insurance",
            "reimbursement",
            "compensation recovery"
        };

        private readonly IPayrollRepository _payrollRepo;
        private readonly IPayrollSourceResolver _sourceResolver;
        private readonly IPayrollLegalPolicyResolver _policyResolver;
        private readonly IPayrollFeatureToggleResolver _featureToggleResolver;
        private readonly IPayrollFormulaValidator _formulaValidator;
        private readonly IPayrollCalculationEngine _calculationEngine;
        private readonly IPayrollSnapshotWriter _snapshotWriter;
        private readonly IAuditLogRepository _auditRepo;
        private readonly ICompanyCalendarRepository _companyCalendarRepo;
        private readonly ILockService _lockService;
        private readonly IUnitOfWork _unitOfWork;

        public PayrollCalculationUseCase(
            IPayrollRepository payrollRepo,
            IPayrollSourceResolver sourceResolver,
            IPayrollLegalPolicyResolver policyResolver,
            IPayrollFeatureToggleResolver featureToggleResolver,
            IPayrollFormulaValidator formulaValidator,
            IPayrollCalculationEngine calculationEngine,
            IPayrollSnapshotWriter snapshotWriter,
            IAuditLogRepository auditRepo,
            ICompanyCalendarRepository companyCalendarRepo,
            ILockService lockService,
            IUnitOfWork unitOfWork)
        {
            _payrollRepo = payrollRepo;
            _sourceResolver = sourceResolver;
            _policyResolver = policyResolver;
            _featureToggleResolver = featureToggleResolver;
            _formulaValidator = formulaValidator;
            _calculationEngine = calculationEngine;
            _snapshotWriter = snapshotWriter;
            _auditRepo = auditRepo;
            _companyCalendarRepo = companyCalendarRepo;
            _lockService = lockService;
            _unitOfWork = unitOfWork;
        }

        public async Task<PayrollPreflightDto> GetPreflightAsync(PayrollPeriodDto dto, string actorRole, CancellationToken ct = default)
        {
            EnsurePayrollOperator(actorRole);
            ValidatePeriod(dto.Month, dto.Year);

            var periodStart = new DateTime(dto.Year, dto.Month, 1);
            var periodEnd = periodStart.AddMonths(1).AddTicks(-1);
            var result = new PayrollPreflightDto
            {
                Month = dto.Month,
                Year = dto.Year,
                Period = $"{dto.Month:00}/{dto.Year}",
                PeriodStart = periodStart,
                PeriodEnd = periodEnd
            };

            result.FeatureToggles = await _featureToggleResolver.GetAsync(ct);
            result.DependencyImpacts = BuildDependencyImpacts(result.FeatureToggles);

            if (await _payrollRepo.HasLockedPayrollAsync(dto.Month, dto.Year, ct))
                result.Errors.Add("Ky luong da khoa hoac da chot, khong the tinh lai.");

            try
            {
                var policySet = await _policyResolver.ResolvePayrollPoliciesAsync(dto, result.FeatureToggles, ct);
                AddPolicy(result.Policies, "Thue TNCN", policySet.TaxConfig.Code, policySet.TaxConfig.Name, policySet.TaxConfig.Version, policySet.TaxConfig.VersionCode, policySet.TaxConfig.EffectiveFrom, policySet.TaxConfig.EffectiveTo, policySet.TaxConfig.Status.ToString(), true, policySet.TaxConfig.Note);

                var pitVersion = policySet.PitBrackets.FirstOrDefault();
                if (pitVersion != null)
                    AddPolicy(result.Policies, "Bieu thue TNCN", pitVersion.Code, $"Bieu thue luy tien {policySet.PitBrackets.Count} bac", pitVersion.Version, pitVersion.VersionCode, pitVersion.EffectiveFrom, pitVersion.EffectiveTo, pitVersion.Status.ToString(), true, "Ap dung cho thu nhap tinh thue theo phuong phap luy tien.");

                AddPolicy(result.Policies, "Bao hiem", policySet.InsuranceConfig.Code, policySet.InsuranceConfig.Name, policySet.InsuranceConfig.Version, policySet.InsuranceConfig.VersionCode, policySet.InsuranceConfig.EffectiveFrom, policySet.InsuranceConfig.EffectiveTo, policySet.InsuranceConfig.Status.ToString(), result.FeatureToggles.EnableInsurance, policySet.InsuranceConfig.Note);

                foreach (var ot in policySet.OvertimeRateConfigs)
                    AddPolicy(result.Policies, "Lam them gio", ot.Code, ot.OvertimeType.ToString(), ot.Version, ot.VersionCode, ot.EffectiveFrom, ot.EffectiveTo, ot.Status.ToString(), result.FeatureToggles.EnableOvertime, ot.Note);

                foreach (var policy in policySet.AllowanceTaxPolicies)
                    AddPolicy(result.Policies, "Phu cap", policy.Code, policy.Name, policy.Version, policy.VersionCode, policy.EffectiveFrom, policy.EffectiveTo, policy.Status.ToString(), true, policy.Description);

                foreach (var policy in policySet.SeniorityPolicies)
                    AddPolicy(result.Policies, "Tham nien", policy.Code, policy.Name, policy.Version, policy.VersionCode, policy.EffectiveFrom, policy.EffectiveTo, policy.Status.ToString(), true, policy.Description);

                foreach (var policy in policySet.MinimumWagePolicies)
                    AddPolicy(result.Policies, "Luong toi thieu vung", policy.Code, policy.Name, policy.Version, policy.VersionCode, policy.EffectiveFrom, policy.EffectiveTo, policy.Status.ToString(), true, policy.Description);

                if (policySet.MinimumWagePolicies.Count == 0)
                    result.Warnings.Add("Chua co cau hinh luong toi thieu vung cho ky nay. Payroll van co the chay, nhung can ra soat tran bao hiem theo vung.");

                var payrollFormulas = await _payrollRepo.GetApprovedPayrollFormulasAsync(periodEnd, ct);
                foreach (var formula in payrollFormulas)
                {
                    AddPolicy(
                        result.Policies,
                        "Cong thuc luong",
                        formula.FormulaCode,
                        formula.FormulaName,
                        formula.Version,
                        formula.VersionCode,
                        formula.EffectiveFrom,
                        formula.EffectiveTo,
                        formula.Status.ToString(),
                        formula.IsActive,
                        formula.VersionCode == "KPI_PAYOUT_V2"
                            ? "KPI_BONUS duoc tinh la muc thuong KPI toi da * diem KPI / 100."
                            : "Cong thuc cu: KPI_BONUS duoc hieu la khoan thuong KPI muc tieu.");
                }

                var activeCalendar = await _companyCalendarRepo.GetActiveByYearAsync(dto.Year, ct);
                if (activeCalendar == null)
                {
                    result.Errors.Add($"Thieu lich nghi cong ty dang ap dung cho nam {dto.Year}.");
                }
                else
                {
                    AddPolicy(
                        result.Policies,
                        "Lich nghi cong ty",
                        activeCalendar.VersionCode,
                        $"Lich nghi nam {activeCalendar.Year}",
                        1,
                        activeCalendar.VersionCode,
                        activeCalendar.EffectiveFrom,
                        activeCalendar.EffectiveTo,
                        activeCalendar.Status.ToString(),
                        true,
                        $"{activeCalendar.Days.Count} ngay cau hinh.");
                }

                if (policySet.WorkCalendars.Count == 0)
                    result.Warnings.Add("Chua co cau hinh ca lam viec/phong ban cho ky nay. He thong se dung du lieu bang cong da tong hop.");

                if (policySet.WorkCalendars.Any(c => !c.CompanyCalendarId.HasValue))
                    result.Warnings.Add("Mot so cau hinh ca lam viec chua chon lich nghi cong ty. OT va bang cong co the phai dung lich cong ty mac dinh.");
            }
            catch (InvalidOperationException ex)
            {
                result.Errors.Add(ex.Message);
            }

            result.CanCalculate = result.Errors.Count == 0;
            return result;
        }

        public async Task<PayrollCalculationResultDto> ExecuteCalculationAsync(PayrollPeriodDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsurePayrollOperator(actorRole);
            ValidatePeriod(dto.Month, dto.Year);

            return await _lockService.GetWithLockAsync(
                LockKeys.PayrollRun(dto.Month, dto.Year),
                innerCt => ExecuteCalculationCoreAsync(dto, actorAccountId, actorRole, innerCt),
                TimeSpan.FromSeconds(30),
                ct);
        }

        private async Task<PayrollCalculationResultDto> ExecuteCalculationCoreAsync(PayrollPeriodDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsurePayrollOperator(actorRole);
            ValidatePeriod(dto.Month, dto.Year);

            var preflight = await GetPreflightAsync(dto, actorRole, ct);
            if (!preflight.CanCalculate)
                throw new InvalidOperationException(string.Join(" ", preflight.Errors));

            if (await _payrollRepo.HasLockedPayrollAsync(dto.Month, dto.Year, ct))
                throw new InvalidOperationException("Kỳ lương đã khóa/chốt, không thể tính lại.");

            var batch = await _sourceResolver.ResolveAsync(dto, ct);
            if (batch.Sources.Count == 0)
                throw new InvalidOperationException(batch.Warnings.FirstOrDefault() ?? "Không có dữ liệu nguồn hợp lệ để tính lương.");

            var payrolls = new List<Core.Entities.PayrollAllowances.Payroll>();
            var warnings = new List<string>(batch.Warnings);

            foreach (var source in batch.Sources)
            {
                try
                {
                    _formulaValidator.Validate(source);
                    var output = _calculationEngine.Calculate(source);
                    payrolls.Add(_snapshotWriter.CreateSnapshot(source, output, actorAccountId));
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or FormatException)
                {
                    warnings.Add($"{source.Employee.EmployeeCode} - {source.Employee.FullName}: {ex.Message}");
                }
            }

            if (payrolls.Count == 0)
                throw new InvalidOperationException("Tất cả nhân viên đều bị bỏ qua khi tính lương. Vui lòng kiểm tra công thức và dữ liệu đầu vào.");

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                await _payrollRepo.ReplaceDraftsAsync(dto.Month, dto.Year, payrolls, ct);
                await _auditRepo.LogSystemEventAsync(
                    "PAYROLL_CALCULATED",
                    actorAccountId,
                    "payrolls",
                    $"Calculated draft payroll by formula engine for {batch.Period}. Count={payrolls.Count}, Skipped={warnings.Count}");
                await _unitOfWork.CommitAsync(ct);
            }, ct);

            var savedPayrolls = await _payrollRepo.GetByPeriodAsync(dto.Month, dto.Year, ct);
            return new PayrollCalculationResultDto
            {
                Month = dto.Month,
                Year = dto.Year,
                CreatedCount = savedPayrolls.Count,
                SkippedCount = warnings.Count,
                Warnings = warnings,
                Payrolls = savedPayrolls.Select(p => PayrollSlipMapper.Map(p)).ToList()
            };
        }

        public async Task<PayrollRunSummaryDto> GetPayrollRunSummaryAsync(PayrollPeriodDto dto, string actorRole, CancellationToken ct = default)
        {
            EnsurePayrollViewer(actorRole);
            ValidatePeriod(dto.Month, dto.Year);

            var payrolls = await _payrollRepo.GetByPeriodAsync(dto.Month, dto.Year, ct);
            return MapPayrollRunSummary(payrolls, dto.Month, dto.Year, includeSlips: true);
        }

        public async Task<List<PayrollRunSummaryDto>> GetPendingPayrollRunsAsync(string actorRole, CancellationToken ct = default)
        {
            EnsurePayrollApprover(actorRole);

            var pending = await _payrollRepo.GetByStatusAsync(PayrollStatus.PendingApproval, ct);
            var pendingPeriods = pending
                .Where(p => p.Month.HasValue && p.Year.HasValue)
                .GroupBy(p => new { Month = p.Month!.Value, Year = p.Year!.Value })
                .OrderByDescending(g => g.Key.Year)
                .ThenByDescending(g => g.Key.Month)
                .Select(g => g.Key)
                .ToList();

            var result = new List<PayrollRunSummaryDto>();
            foreach (var period in pendingPeriods)
            {
                var payrolls = await _payrollRepo.GetByPeriodAsync(period.Month, period.Year, ct);
                if (IsRunReadyForDirectorReview(payrolls))
                {
                    result.Add(MapPayrollRunSummary(payrolls, period.Month, period.Year, includeSlips: false));
                }
            }

            return result;
        }

        public async Task<PayrollRunSummaryDto> SubmitPayrollRunAsync(PayrollPeriodDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsurePayrollOperator(actorRole);
            ValidatePeriod(dto.Month, dto.Year);

            return await _lockService.GetWithLockAsync(
                LockKeys.PayrollRun(dto.Month, dto.Year),
                innerCt => SubmitPayrollRunCoreAsync(dto, actorAccountId, actorRole, innerCt),
                TimeSpan.FromSeconds(20),
                ct);
        }

        private async Task<PayrollRunSummaryDto> SubmitPayrollRunCoreAsync(PayrollPeriodDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsurePayrollOperator(actorRole);
            ValidatePeriod(dto.Month, dto.Year);

            var payrolls = await _payrollRepo.GetTrackedByPeriodAsync(dto.Month, dto.Year, ct);
            if (payrolls.Count == 0)
                throw new InvalidOperationException("Chua co bang luong nhap de gui duyet.");

            if (payrolls.Any(IsLockedStatus))
                throw new InvalidOperationException("Ky luong da khoa/chot, khong the gui duyet lai.");

            if (payrolls.Any(p => p.Status == PayrollStatus.Approved || p.Status == PayrollStatus.PendingApproval))
                throw new InvalidOperationException("Bang luong dang cho duyet hoac da duoc duyet.");

            if (payrolls.Any(p => p.Status != PayrollStatus.Calculated &&
                                  p.Status != PayrollStatus.HRReviewed &&
                                  p.Status != PayrollStatus.RevisionRequired))
                throw new InvalidOperationException("Chi bang luong da tong hop hoac can bo sung moi duoc gui duyet.");

            var now = DateTime.UtcNow;
            foreach (var payroll in payrolls)
            {
                payroll.Status = PayrollStatus.PendingApproval;
                payroll.SubmittedByAccountId = actorAccountId;
                payroll.SubmittedAt = now;
                payroll.ApprovedByAccountId = null;
                payroll.ApprovedAt = null;
                payroll.ReviewNote = null;
            }

            await _auditRepo.LogSystemEventAsync(
                "PAYROLL_RUN_SUBMITTED",
                actorAccountId,
                "payrolls",
                $"Submitted payroll run {dto.Month:00}/{dto.Year} for approval. Count={payrolls.Count}.");
            await _unitOfWork.CommitAsync(ct);

            return MapPayrollRunSummary(payrolls, dto.Month, dto.Year, includeSlips: true);
        }

        public async Task<PayrollRunSummaryDto> DirectorReviewPayrollRunAsync(PayrollPeriodDto dto, PayrollRunReviewDto review, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsurePayrollApprover(actorRole);
            ValidatePeriod(dto.Month, dto.Year);

            return await _lockService.GetWithLockAsync(
                LockKeys.PayrollRun(dto.Month, dto.Year),
                innerCt => DirectorReviewPayrollRunCoreAsync(dto, review, actorAccountId, actorRole, innerCt),
                TimeSpan.FromSeconds(20),
                ct);
        }

        private async Task<PayrollRunSummaryDto> DirectorReviewPayrollRunCoreAsync(PayrollPeriodDto dto, PayrollRunReviewDto review, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsurePayrollApprover(actorRole);
            ValidatePeriod(dto.Month, dto.Year);

            var payrolls = await _payrollRepo.GetTrackedByPeriodAsync(dto.Month, dto.Year, ct);
            if (payrolls.Count == 0)
                throw new InvalidOperationException("Khong tim thay bang luong can duyet.");

            if (!IsRunReadyForDirectorReview(payrolls))
                throw new InvalidOperationException("Chi co the xu ly khi tat ca phieu luong trong ky dang cho phe duyet.");

            var note = review.Note?.Trim();
            if (!review.IsApproved && string.IsNullOrWhiteSpace(note))
                throw new ArgumentException("Can nhap ghi chu khi tu choi hoac yeu cau bo sung.");

            var now = DateTime.UtcNow;
            var nextStatus = review.IsApproved
                ? PayrollStatus.Approved
                : review.RequestRevision
                    ? PayrollStatus.RevisionRequired
                    : PayrollStatus.Rejected;

            foreach (var payroll in payrolls)
            {
                payroll.Status = nextStatus;
                payroll.ApprovedByAccountId = review.IsApproved ? actorAccountId : null;
                payroll.ApprovedAt = review.IsApproved ? now : null;
                payroll.ReviewNote = note;
            }

            var auditAction = review.IsApproved
                ? "PAYROLL_RUN_APPROVED"
                : review.RequestRevision
                    ? "PAYROLL_RUN_REVISION_REQUESTED"
                    : "PAYROLL_RUN_REJECTED";

            await _auditRepo.LogSystemEventAsync(
                auditAction,
                actorAccountId,
                "payrolls",
                $"Reviewed payroll run {dto.Month:00}/{dto.Year}. Status={nextStatus}. Count={payrolls.Count}.");
            await _unitOfWork.CommitAsync(ct);

            return MapPayrollRunSummary(payrolls, dto.Month, dto.Year, includeSlips: true);
        }

        public async Task<PayrollRunSummaryDto> LockPayrollPeriodAsync(PayrollPeriodDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsurePayrollOperator(actorRole);
            ValidatePeriod(dto.Month, dto.Year);

            return await _lockService.GetWithLockAsync(
                LockKeys.PayrollRun(dto.Month, dto.Year),
                innerCt => LockPayrollPeriodCoreAsync(dto, actorAccountId, actorRole, innerCt),
                TimeSpan.FromSeconds(20),
                ct);
        }

        private async Task<PayrollRunSummaryDto> LockPayrollPeriodCoreAsync(PayrollPeriodDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsurePayrollOperator(actorRole);
            ValidatePeriod(dto.Month, dto.Year);

            var payrolls = await _payrollRepo.GetTrackedByPeriodAsync(dto.Month, dto.Year, ct);
            if (payrolls.Count == 0)
                throw new InvalidOperationException("Chua co bang luong de chot.");

            if (payrolls.All(p => p.Status == PayrollStatus.Finalized || p.Status == PayrollStatus.Paid))
                return MapPayrollRunSummary(payrolls, dto.Month, dto.Year, includeSlips: true);

            if (payrolls.Any(p => p.Status != PayrollStatus.Approved))
                throw new InvalidOperationException("Chi co the chot bang luong da duoc giam doc duyet.");

            var now = DateTime.UtcNow;
            foreach (var payroll in payrolls)
            {
                payroll.Status = PayrollStatus.Finalized;
                payroll.LockedByAccountId = actorAccountId;
                payroll.LockedAt = now;
            }

            await _auditRepo.LogSystemEventAsync(
                "PAYROLL_RUN_FINALIZED",
                actorAccountId,
                "payrolls",
                $"Finalized payroll run {dto.Month:00}/{dto.Year}. Count={payrolls.Count}.");
            await _unitOfWork.CommitAsync(ct);

            return MapPayrollRunSummary(payrolls, dto.Month, dto.Year, includeSlips: true);
        }

        public async Task<PayrollAdjustmentDto> CreateAdjustmentAsync(CreatePayrollAdjustmentDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsurePayrollOperator(actorRole);
            ValidatePeriod(dto.RecognizedMonth, dto.RecognizedYear);
            if (dto.EmployeeId <= 0) throw new ArgumentException("Nhân viên không hợp lệ.");
            if (dto.Amount == 0) throw new ArgumentException("Số tiền điều chỉnh phải khác 0.");
            if (string.IsNullOrWhiteSpace(dto.Reason)) throw new ArgumentException("Cần nhập lý do điều chỉnh.");
            ValidatePayrollAdjustmentBusinessRule(dto);

            if (await _payrollRepo.HasLockedPayrollAsync(dto.RecognizedMonth, dto.RecognizedYear, ct))
                throw new InvalidOperationException("Kỳ lương đã khóa/chốt, không thể thêm điều chỉnh.");

            var adjustment = new Core.Entities.PayrollAllowances.PayrollAdjustment
            {
                EmployeeId = dto.EmployeeId,
                AdjustmentType = dto.AdjustmentType,
                RecognizedMonth = dto.RecognizedMonth,
                RecognizedYear = dto.RecognizedYear,
                RecognizedPayrollPeriod = $"{dto.RecognizedMonth:00}/{dto.RecognizedYear}",
                EffectiveFromMonth = dto.EffectiveFromMonth,
                EffectiveToMonth = dto.EffectiveToMonth,
                Amount = dto.Amount,
                IsTaxable = dto.IsTaxable,
                IsInsuranceBased = dto.IsInsuranceBased,
                IsDeduction = dto.IsDeduction,
                Status = PayrollAdjustmentStatus.Approved,
                ApprovedByAccountId = actorAccountId,
                ApprovedAt = DateTime.UtcNow,
                Reason = dto.Reason.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _payrollRepo.AddPayrollAdjustmentAsync(adjustment, ct);
            await _auditRepo.LogSystemEventAsync(
                "PAYROLL_ADJUSTMENT_CREATED",
                actorAccountId,
                "payroll_adjustments",
                $"Created payroll adjustment for employee {dto.EmployeeId}, period {adjustment.RecognizedPayrollPeriod}, amount={dto.Amount}.");
            await _unitOfWork.CommitAsync(ct);

            return MapAdjustment(adjustment);
        }

        public async Task<List<PayrollAdjustmentDto>> GetAdjustmentsAsync(byte month, short year, string actorRole, CancellationToken ct = default)
        {
            EnsurePayrollOperator(actorRole);
            ValidatePeriod(month, year);

            var adjustments = await _payrollRepo.GetPayrollAdjustmentsAsync(month, year, ct);
            return adjustments.Select(MapAdjustment).ToList();
        }

        private static void ValidatePeriod(byte month, short year)
        {
            if (month is < 1 or > 12)
                throw new ArgumentException("Tháng lương không hợp lệ.");
            if (year < 2000)
                throw new ArgumentException("Năm lương không hợp lệ.");
        }

        private static void EnsurePayrollOperator(string role)
        {
            if (!IsAny(role, "Admin", "HR"))
                throw new UnauthorizedAccessException("Bạn không có quyền tổng hợp bảng lương.");
        }

        private static void EnsurePayrollViewer(string role)
        {
            if (!IsAny(role, "Admin", "HR", "Director"))
                throw new UnauthorizedAccessException("Bạn không có quyền xem bảng lương theo kỳ.");
        }

        private static void EnsurePayrollApprover(string role)
        {
            if (!IsAny(role, "Admin", "Director"))
                throw new UnauthorizedAccessException("Bạn không có quyền phê duyệt bảng lương.");
        }

        private static bool IsLockedStatus(Core.Entities.PayrollAllowances.Payroll payroll)
        {
            return payroll.Status == PayrollStatus.Locked ||
                   payroll.Status == PayrollStatus.Finalized ||
                   payroll.Status == PayrollStatus.Paid;
        }

        private static void AddPolicy(
            List<PayrollPreflightPolicyDto> policies,
            string area,
            string code,
            string name,
            int version,
            string? versionCode,
            DateTime effectiveFrom,
            DateTime? effectiveTo,
            string status,
            bool isApplied,
            string? note)
        {
            policies.Add(new PayrollPreflightPolicyDto
            {
                Area = area,
                Code = code,
                Name = name,
                Version = version,
                VersionCode = versionCode,
                EffectiveFrom = effectiveFrom,
                EffectiveTo = effectiveTo,
                Status = status,
                IsApplied = isApplied,
                Note = note
            });
        }

        private static List<PayrollDependencyImpactDto> BuildDependencyImpacts(PayrollFeatureToggleDto toggles)
        {
            return new List<PayrollDependencyImpactDto>
            {
                new()
                {
                    Key = "enableInsurance",
                    Name = "Bao hiem",
                    Enabled = toggles.EnableInsurance,
                    Impacts = toggles.EnableInsurance
                        ? new List<string>
                        {
                            "Yeu cau cau hinh bao hiem con hieu luc.",
                            "Tinh khoan trich nguoi lao dong va chi phi cong ty.",
                            "Thu nhap tinh thue duoc tru phan bao hiem nguoi lao dong."
                        }
                        : new List<string>
                        {
                            "Khong yeu cau luong dong bao hiem.",
                            "Khong tinh khoan trich nguoi lao dong va chi phi cong ty.",
                            "Snapshot ghi bao hiem la khong ap dung."
                        }
                },
                new()
                {
                    Key = "enableOvertime",
                    Name = "Lam them gio",
                    Enabled = toggles.EnableOvertime,
                    Impacts = toggles.EnableOvertime
                        ? new List<string>
                        {
                            "Yeu cau du policy OT ngay thuong, cuoi tuan, ngay le va ban dem.",
                            "Doc phan loai ngay tu lich cong ty va cau hinh ca lam viec.",
                            "Dua OT da duyet/doi chieu vao cong thuc luong."
                        }
                        : new List<string>
                        {
                            "Khong yeu cau policy OT cho ky luong.",
                            "Khong dua OT vao cong thuc luong.",
                            "An phan tach OT chiu thue va khong chiu thue."
                        }
                },
                new()
                {
                    Key = "enableMealAllowance",
                    Name = "Phu cap an",
                    Enabled = toggles.EnableMealAllowance,
                    Impacts = toggles.EnableMealAllowance
                        ? new List<string>
                        {
                            "Dua phu cap an theo ngay cong vao cong thuc.",
                            "Ap dung cau hinh thue/phu cap lien quan neu co."
                        }
                        : new List<string>
                        {
                            "Khong tinh bien MEAL_ALLOWANCE.",
                            "Khong yeu cau han muc thue cho phu cap an."
                        }
                },
                new()
                {
                    Key = "enableExternalTimesheetPay",
                    Name = "Gio cong cong tac vien",
                    Enabled = toggles.EnableExternalTimesheetPay,
                    Impacts = toggles.EnableExternalTimesheetPay
                        ? new List<string>
                        {
                            "Dua gio cong cong tac vien da duyet vao ky luong.",
                            "Co the tao dong payroll cho cong tac vien chi co timesheet ngoai."
                        }
                        : new List<string>
                        {
                            "Khong doc gio cong cong tac vien.",
                            "Khong tinh bien EXTERNAL_TIMESHEET_PAY."
                        }
                }
            };
        }

        private static void ValidatePayrollAdjustmentBusinessRule(CreatePayrollAdjustmentDto dto)
        {
            if (!AllowedPayrollAdjustmentTypes.Contains(dto.AdjustmentType))
                throw new ArgumentException("Loại điều chỉnh lương không hợp lệ. Vui lòng chọn truy lĩnh/truy thu, điều chỉnh bảo hiểm, điều chỉnh thuế hoặc điều chỉnh thủ công nghiệp vụ lương.");

            var reason = NormalizeForSearch(dto.Reason.Trim());
            if (AttendancePresenceKeywords.Any(keyword => reason.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Lỗi hiện diện như đi muộn, về sớm, vắng mặt hoặc rời vị trí phải xử lý bằng điều chỉnh bảng công/biên bản vi phạm; không tạo PayrollAdjustment trực tiếp.");

            if (dto.AdjustmentType == PayrollAdjustmentType.ManualCorrection &&
                !ManualPayrollCorrectionKeywords.Any(keyword => reason.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Điều chỉnh thủ công trong payroll chỉ dùng cho nghiệp vụ lương hợp lệ như truy thu/truy lĩnh, thuế, bảo hiểm, bồi hoàn hoặc sai sót kỳ trước. Lỗi hiện diện phải xử lý ở bảng công.");
        }

        private static string NormalizeForSearch(string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category != UnicodeCategory.NonSpacingMark)
                    builder.Append(ch);
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static bool IsAny(string role, params string[] values)
        {
            return values.Any(v => string.Equals(role, v, StringComparison.OrdinalIgnoreCase));
        }

        private static PayrollRunSummaryDto MapPayrollRunSummary(List<Core.Entities.PayrollAllowances.Payroll> payrolls, byte month, short year, bool includeSlips)
        {
            if (payrolls.Count == 0)
            {
                return new PayrollRunSummaryDto
                {
                    Month = month,
                    Year = year,
                    Period = $"{month:00}/{year}",
                    Status = PayrollStatus.Draft,
                    StatusText = PayrollStatusLabel(PayrollStatus.Draft)
                };
            }

            var status = ResolveRunStatus(payrolls);
            return new PayrollRunSummaryDto
            {
                Month = month,
                Year = year,
                Period = $"{month:00}/{year}",
                Status = status,
                StatusText = PayrollStatusLabel(status),
                SlipCount = payrolls.Count,
                GrossIncome = payrolls.Sum(p => p.GrossIncome ?? p.GrossSalary ?? 0),
                NetSalary = payrolls.Sum(p => p.NetSalary ?? 0),
                TotalCompanyCost = payrolls.Sum(p => p.TotalCompanyCost ?? 0),
                CalculatedAt = payrolls.Max(p => p.CalculatedAt),
                SubmittedAt = payrolls.Max(p => p.SubmittedAt),
                ApprovedAt = payrolls.Max(p => p.ApprovedAt),
                LockedAt = payrolls.Max(p => p.LockedAt),
                SubmittedByAccountId = payrolls.OrderByDescending(p => p.SubmittedAt).FirstOrDefault(p => p.SubmittedByAccountId.HasValue)?.SubmittedByAccountId,
                ApprovedByAccountId = payrolls.OrderByDescending(p => p.ApprovedAt).FirstOrDefault(p => p.ApprovedByAccountId.HasValue)?.ApprovedByAccountId,
                LockedByAccountId = payrolls.OrderByDescending(p => p.LockedAt).FirstOrDefault(p => p.LockedByAccountId.HasValue)?.LockedByAccountId,
                ReviewNote = payrolls.OrderByDescending(p => p.ApprovedAt ?? p.SubmittedAt ?? p.CalculatedAt ?? p.CreatedAt).FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.ReviewNote))?.ReviewNote,
                Slips = includeSlips
                    ? payrolls.OrderBy(p => p.Employee?.FullName).Select(p => PayrollSlipMapper.Map(p)).ToList()
                    : new List<SalarySlipDto>()
            };
        }

        private static PayrollStatus ResolveRunStatus(List<Core.Entities.PayrollAllowances.Payroll> payrolls)
        {
            var distinctStatuses = payrolls.Select(p => p.Status).Distinct().ToList();
            if (distinctStatuses.Count == 1) return distinctStatuses[0];
            if (distinctStatuses.All(status => status == PayrollStatus.Finalized || status == PayrollStatus.Paid)) return PayrollStatus.Finalized;
            if (distinctStatuses.Contains(PayrollStatus.PendingApproval)) return PayrollStatus.RevisionRequired;
            if (distinctStatuses.Contains(PayrollStatus.RevisionRequired)) return PayrollStatus.RevisionRequired;
            if (distinctStatuses.Contains(PayrollStatus.Rejected)) return PayrollStatus.Rejected;
            if (distinctStatuses.Contains(PayrollStatus.Approved)) return PayrollStatus.Approved;
            if (distinctStatuses.Contains(PayrollStatus.Calculated)) return PayrollStatus.Calculated;
            return distinctStatuses[0];
        }

        private static bool IsRunReadyForDirectorReview(List<Core.Entities.PayrollAllowances.Payroll> payrolls) =>
            payrolls.Count > 0 && payrolls.All(p => p.Status == PayrollStatus.PendingApproval);

        private static string PayrollStatusLabel(PayrollStatus status)
        {
            return status switch
            {
                PayrollStatus.Draft => "Bản nháp",
                PayrollStatus.Calculated => "Đã tổng hợp",
                PayrollStatus.HRReviewed => "HR đã kiểm tra",
                PayrollStatus.PendingApproval => "Chờ giám đốc duyệt",
                PayrollStatus.Approved => "Đã duyệt",
                PayrollStatus.Locked => "Đã khóa",
                PayrollStatus.Finalized => "Đã chốt",
                PayrollStatus.Paid => "Đã chi trả",
                PayrollStatus.Cancelled => "Đã hủy",
                PayrollStatus.RevisionRequired => "Cần bổ sung",
                PayrollStatus.Rejected => "Từ chối",
                _ => status.ToString()
            };
        }

        private static PayrollAdjustmentDto MapAdjustment(Core.Entities.PayrollAllowances.PayrollAdjustment adjustment)
        {
            return new PayrollAdjustmentDto
            {
                Id = adjustment.Id,
                EmployeeId = adjustment.EmployeeId,
                EmployeeCode = adjustment.Employee?.EmployeeCode,
                EmployeeName = adjustment.Employee?.FullName,
                AdjustmentType = adjustment.AdjustmentType,
                RecognizedMonth = adjustment.RecognizedMonth,
                RecognizedYear = adjustment.RecognizedYear,
                RecognizedPayrollPeriod = adjustment.RecognizedPayrollPeriod,
                EffectiveFromMonth = adjustment.EffectiveFromMonth,
                EffectiveToMonth = adjustment.EffectiveToMonth,
                Amount = adjustment.Amount,
                IsTaxable = adjustment.IsTaxable,
                IsInsuranceBased = adjustment.IsInsuranceBased,
                IsDeduction = adjustment.IsDeduction,
                Status = adjustment.Status,
                Reason = adjustment.Reason,
                CreatedAt = adjustment.CreatedAt
            };
        }
    }
}
