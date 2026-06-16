using System.Globalization;
using System.Text;
using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;
using HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Usecases;
using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PayrollAllowances;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;

namespace HRM.backend.src.HRM.Application.UseCases.PayrollAllowances
{
    public class PayrollAccessUseCase : IPayrollAccessUseCase
    {
        private readonly IPayrollRepository _payrollRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly IUnitOfWork _unitOfWork;

        public PayrollAccessUseCase(
            IPayrollRepository payrollRepo,
            IEmployeeRepository employeeRepo,
            IAuditLogRepository auditRepo,
            IUnitOfWork unitOfWork)
        {
            _payrollRepo = payrollRepo;
            _employeeRepo = employeeRepo;
            _auditRepo = auditRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<SalarySlipDto>> GetSalarySlipsAsync(int accountId, string role, string? period, byte? month, short? year, CancellationToken ct = default)
        {
            var resolved = ResolvePeriod(period, month, year);
            var context = await ResolveAccessContextAsync(accountId, role, ct);

            var slips = context.Scope switch
            {
                PayrollAccessScope.All => await _payrollRepo.GetByPeriodAsync(resolved.Month, resolved.Year, ct),
                PayrollAccessScope.Department => await _payrollRepo.GetByDepartmentPeriodAsync(context.DepartmentId!.Value, resolved.Month, resolved.Year, ct),
                PayrollAccessScope.Individual => await _payrollRepo.GetByEmployeePeriodAsync(context.EmployeeId!.Value, resolved.Month, resolved.Year, ct),
                _ => new List<Payroll>()
            };

            return slips.Select(p => PayrollSlipMapper.Map(p, includeDetails: false)).ToList();
        }

        public async Task<SalarySlipDto> GetSalarySlipDetailAsync(int accountId, string role, int slipId, CancellationToken ct = default)
        {
            var context = await ResolveAccessContextAsync(accountId, role, ct);
            var slip = await _payrollRepo.GetDetailAsync(slipId, ct)
                ?? throw new InvalidOperationException("Không tìm thấy phiếu lương.");

            EnsureSlipAccess(slip, context);
            return PayrollSlipMapper.Map(slip);
        }

        public async Task<List<SalarySlipDto>> GetMySalarySlipsAsync(int accountId, string? period, byte? month, short? year, CancellationToken ct = default)
        {
            var resolved = ResolvePeriod(period, month, year);
            var context = await ResolveIndividualAccessContextAsync(accountId, ct);
            var slips = await _payrollRepo.GetByEmployeePeriodAsync(context.EmployeeId!.Value, resolved.Month, resolved.Year, ct);

            return slips.Select(p => PayrollSlipMapper.Map(p, includeDetails: false)).ToList();
        }

        public async Task<SalarySlipDto> GetMySalarySlipDetailAsync(int accountId, int slipId, CancellationToken ct = default)
        {
            var context = await ResolveIndividualAccessContextAsync(accountId, ct);
            var slip = await _payrollRepo.GetDetailAsync(slipId, ct)
                ?? throw new InvalidOperationException("Khong tim thay phieu luong.");

            EnsureSlipAccess(slip, context);
            return PayrollSlipMapper.Map(slip);
        }

        public async Task<SalarySlipExportResultDto> GenerateSalarySlipFilesAsync(int accountId, string role, string? email, SalarySlipExportRequestDto dto, CancellationToken ct = default)
        {
            if (dto.SlipIds.Count == 0)
                throw new ArgumentException("Vui lòng chọn ít nhất một phiếu lương để kết xuất.");

            var context = await ResolveAccessContextAsync(accountId, role, ct);
            var slips = await _payrollRepo.GetDetailsByIdsAsync(dto.SlipIds, ct);
            if (slips.Count != dto.SlipIds.Distinct().Count())
                throw new InvalidOperationException("Một hoặc nhiều phiếu lương không tồn tại.");

            foreach (var slip in slips)
                EnsureSlipAccess(slip, context);

            var content = BuildCsv(slips, email ?? $"account-{accountId}");
            await _auditRepo.LogSystemEventAsync(
                "SALARY_SLIP_EXPORTED",
                accountId,
                "payrolls",
                $"Exported salary slips: {string.Join(",", dto.SlipIds.Distinct())}");
            await _unitOfWork.CommitAsync(ct);

            return new SalarySlipExportResultDto
            {
                FileName = $"salary_slips_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv",
                ContentType = "text/csv; charset=utf-8",
                Content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(content)).ToArray()
            };
        }

        private async Task<PayrollAccessContext> ResolveAccessContextAsync(int accountId, string role, CancellationToken ct)
        {
            if (IsAny(role, "Admin", "HR", "Director"))
                return new PayrollAccessContext(PayrollAccessScope.All, null, null);

            var employee = await _employeeRepo.GetByAccountIdAsync(accountId, ct)
                ?? throw new UnauthorizedAccessException("Tài khoản chưa liên kết hồ sơ nhân viên.");

            if (IsAny(role, "Manager"))
            {
                if (!employee.DeptId.HasValue)
                    throw new UnauthorizedAccessException("Tài khoản quản lý chưa có phòng ban để xem bảng lương.");

                return new PayrollAccessContext(PayrollAccessScope.Department, employee.Id, employee.DeptId);
            }

            return new PayrollAccessContext(PayrollAccessScope.Individual, employee.Id, employee.DeptId);
        }

        private async Task<PayrollAccessContext> ResolveIndividualAccessContextAsync(int accountId, CancellationToken ct)
        {
            var employee = await _employeeRepo.GetByAccountIdAsync(accountId, ct)
                ?? throw new UnauthorizedAccessException("Tai khoan chua lien ket ho so nhan vien.");

            return new PayrollAccessContext(PayrollAccessScope.Individual, employee.Id, employee.DeptId);
        }

        private static void EnsureSlipAccess(Payroll slip, PayrollAccessContext context)
        {
            var allowed = context.Scope switch
            {
                PayrollAccessScope.All => true,
                PayrollAccessScope.Department => slip.Employee?.DeptId == context.DepartmentId,
                PayrollAccessScope.Individual => slip.EmployeeId == context.EmployeeId,
                _ => false
            };

            if (!allowed)
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập phiếu lương này.");
        }

        private static (byte Month, short Year) ResolvePeriod(string? period, byte? month, short? year)
        {
            if (!string.IsNullOrWhiteSpace(period))
            {
                var normalized = period.Trim().Replace("-", "/");
                if (DateTime.TryParseExact(normalized, "MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    return ((byte)parsed.Month, (short)parsed.Year);

                throw new ArgumentException("Kỳ lương không hợp lệ. Định dạng hợp lệ: MM-yyyy hoặc MM/yyyy.");
            }

            if (month.HasValue && year.HasValue)
            {
                if (month.Value is < 1 or > 12)
                    throw new ArgumentException("Tháng lương không hợp lệ.");
                return (month.Value, year.Value);
            }

            var now = DateTime.UtcNow;
            return ((byte)now.Month, (short)now.Year);
        }

        private static string BuildCsv(IEnumerable<Payroll> slips, string exportedBy)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Watermark,Exported by {Csv(exportedBy)} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            builder.AppendLine("PayrollId,Period,EmployeeCode,EmployeeName,Department,Position,ComponentCode,ComponentName,Amount,TaxableAmount,InsuranceBaseAmount,GrossIncome,InsuranceSalary,EmployeeInsurance,PIT,NetSalary,Status");

            foreach (var slip in slips.OrderBy(s => s.Employee?.FullName).ThenBy(s => s.Id))
            {
                if (slip.Details.Count == 0)
                {
                    AppendSummaryLine(builder, slip, null);
                    continue;
                }

                foreach (var detail in slip.Details.OrderBy(d => d.Id))
                    AppendSummaryLine(builder, slip, detail);
            }

            return builder.ToString();
        }

        private static void AppendSummaryLine(StringBuilder builder, Payroll slip, PayrollDetail? detail)
        {
            var columns = new[]
            {
                slip.Id.ToString(CultureInfo.InvariantCulture),
                Csv(slip.Period ?? $"{slip.Month:00}/{slip.Year}"),
                Csv(slip.Employee?.EmployeeCode),
                Csv(slip.Employee?.FullName),
                Csv(slip.Employee?.Department?.DeptName),
                Csv(slip.Employee?.Position?.Title),
                Csv(detail?.ComponentCode),
                Csv(detail?.ComponentName),
                Money(detail?.Amount ?? 0),
                Money(detail?.TaxableAmount ?? 0),
                Money(detail?.InsuranceBaseAmount ?? 0),
                Money(slip.GrossIncome ?? slip.GrossSalary ?? 0),
                Money(slip.InsuranceSalary ?? 0),
                Money(slip.EmployeeInsuranceAmount ?? slip.InsuranceDeduction ?? 0),
                Money(slip.PitAmount ?? 0),
                Money(slip.NetSalary ?? 0),
                slip.Status.ToString()
            };

            builder.AppendLine(string.Join(",", columns));
        }

        private static string Csv(string? value)
        {
            value ??= string.Empty;
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        private static string Money(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        private static bool IsAny(string role, params string[] values)
        {
            return values.Any(v => string.Equals(role, v, StringComparison.OrdinalIgnoreCase));
        }

        private sealed record PayrollAccessContext(PayrollAccessScope Scope, int? EmployeeId, int? DepartmentId);
    }
}
