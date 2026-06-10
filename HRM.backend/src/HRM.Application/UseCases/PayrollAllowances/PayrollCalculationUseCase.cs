using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;
using HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Services;
using HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Usecases;
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
