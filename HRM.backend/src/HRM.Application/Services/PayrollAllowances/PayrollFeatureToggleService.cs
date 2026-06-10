using System.Text.Json;
using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Services;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;

namespace HRM.backend.src.HRM.Application.Services.PayrollAllowances
{
    public class PayrollFeatureToggleService : IPayrollFeatureToggleResolver, IPayrollFeatureToggleUseCase
    {
        private const string ConfigGroup = "PAYROLL_FEATURE_TOGGLE";
        private const string ConfigKey = "DEFAULT";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IConfigurationRepository _configurationRepo;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IUnitOfWork _unitOfWork;

        public PayrollFeatureToggleService(
            IConfigurationRepository configurationRepo,
            IAuditLogRepository auditLogRepo,
            IUnitOfWork unitOfWork)
        {
            _configurationRepo = configurationRepo;
            _auditLogRepo = auditLogRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<PayrollFeatureToggleDto> GetAsync(CancellationToken ct = default)
        {
            var config = await _configurationRepo.GetConfigByKeyAsync(ConfigGroup, ConfigKey, ct);
            if (config == null || !config.IsActive || string.IsNullOrWhiteSpace(config.ParamValue))
                return PayrollFeatureToggleDto.Default();

            try
            {
                return JsonSerializer.Deserialize<PayrollFeatureToggleDto>(config.ParamValue, JsonOptions)
                    ?? PayrollFeatureToggleDto.Default();
            }
            catch (JsonException)
            {
                return PayrollFeatureToggleDto.Default();
            }
        }

        public async Task<PayrollFeatureToggleDto> UpdateAsync(PayrollFeatureToggleDto dto, int actorAccountId, CancellationToken ct = default)
        {
            var normalized = Normalize(dto);
            await _configurationRepo.SaveConfigAsync(
                ConfigGroup,
                ConfigKey,
                JsonSerializer.Serialize(normalized, JsonOptions),
                "Cấu hình bật/tắt các nhánh tính lương phụ thuộc",
                true,
                ct);

            await _auditLogRepo.LogSystemEventAsync(
                "PAYROLL_FEATURE_TOGGLE_UPDATE",
                actorAccountId,
                "configurations",
                ConfigKey);

            await _unitOfWork.CommitAsync(ct);
            return normalized;
        }

        private static PayrollFeatureToggleDto Normalize(PayrollFeatureToggleDto dto)
        {
            return new PayrollFeatureToggleDto
            {
                EnableInsurance = dto.EnableInsurance,
                EnableOvertime = dto.EnableOvertime,
                EnableMealAllowance = dto.EnableMealAllowance,
                EnableExternalTimesheetPay = dto.EnableExternalTimesheetPay
            };
        }
    }
}
