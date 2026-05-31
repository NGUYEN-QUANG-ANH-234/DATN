using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;
using HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Services;
using HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Usecases;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PayrollAllowances;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
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
        private readonly IPayrollFormulaValidator _formulaValidator;
        private readonly IPayrollCalculationEngine _calculationEngine;
        private readonly IPayrollSnapshotWriter _snapshotWriter;
        private readonly IAuditLogRepository _auditRepo;
        private readonly IUnitOfWork _unitOfWork;

        public PayrollCalculationUseCase(
            IPayrollRepository payrollRepo,
            IPayrollSourceResolver sourceResolver,
            IPayrollFormulaValidator formulaValidator,
            IPayrollCalculationEngine calculationEngine,
            IPayrollSnapshotWriter snapshotWriter,
            IAuditLogRepository auditRepo,
            IUnitOfWork unitOfWork)
        {
            _payrollRepo = payrollRepo;
            _sourceResolver = sourceResolver;
            _formulaValidator = formulaValidator;
            _calculationEngine = calculationEngine;
            _snapshotWriter = snapshotWriter;
            _auditRepo = auditRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<PayrollCalculationResultDto> ExecuteCalculationAsync(PayrollPeriodDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsurePayrollOperator(actorRole);
            ValidatePeriod(dto.Month, dto.Year);

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
